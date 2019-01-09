using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace ProfilerBinlogSplit
{
    public class RawDataFileSlicer : ILogFileSlicer
    {
        const uint FileSignature = 'U' << 24 | '3' << 16 | 'D' << 8 | 'P';
        const uint BlockHeaderSignature = 0xB10C7EAD;
        const ulong BlockHeaderGlobalThreadId = 18446744073709551614;
        const ushort MessageFrame = 34;

        private bool isAlignedMemoryAccess;
        private ulong mainThreadId;
        private uint version;
        private string currentFilePath;
        private long currentFilePos;
        private long currentFileLength;
        private int currentFrame;
        private uint currentFrameIdx;

        private List<byte> globalDataBuffer;
        private List<byte> nextGlobalDataBuffer;
        private List<byte> currentDataBuffer;
        private List<byte> nextFrameDataBuffer;

        public static bool IsRawData(string path)
        {
            var bin = ReadFromFile(path, 0, 4);
            uint header = GetUInt(bin, 0);
            return (FileSignature == header);
        }

        public RawDataFileSlicer(string path)
        {
            currentFilePath = path;
            globalDataBuffer = new List<byte>(1024*1024);
            nextGlobalDataBuffer = new List<byte>(1024 * 1024);
            currentDataBuffer = new List<byte>(1024 * 1024);
            nextFrameDataBuffer = new List<byte>(1024 * 1024);
            //
            using (FileStream fs = File.OpenRead(path))
            {
                currentFileLength = fs.Length;
                fs.Close();
            }
            // read header
            byte[] fileHeader = ReadFromFile(path, 0, 36);
            version = GetUInt(fileHeader, 8);
            mainThreadId = GetULongValue(fileHeader, 28);
            isAlignedMemoryAccess = (fileHeader[5] != 0);

            // append GlobalData
            globalDataBuffer.AddRange(fileHeader);
            this.currentFilePos = fileHeader.Length;
        }


        public int GetCurrentFrame()
        {
            return currentFrame;
        }


        public bool CreateTmpFile(int frameNum, string tmpFile)
        {
            try
            {
                uint finalFrameIdx = uint.MaxValue;
                while (currentFilePos < currentFileLength)
                {
                    uint frameIdx;
                    bool hasBlockFrame = false;
                    if( GetNextBlockFrame(out frameIdx ))
                    {
                        if ( currentFrameIdx < frameIdx)
                        {
                            this.currentFrame++;
                            --frameNum;
                            if( frameNum < 0) { finalFrameIdx = frameIdx; }
                            if(frameNum < -1) { break; }
                            currentFrameIdx = frameIdx;
                        }
                        hasBlockFrame = true;
                    }

                    this.ReadBlock( (frameIdx > finalFrameIdx) & hasBlockFrame);
                }
                using (FileStream writeFs = File.Open(tmpFile, FileMode.Create))
                {
                    writeFs.Write(globalDataBuffer.ToArray(), 0, globalDataBuffer.Count);
                    writeFs.Write(currentDataBuffer.ToArray(), 0, currentDataBuffer.Count);
                }


                if(nextGlobalDataBuffer != null)
                {
                    globalDataBuffer.AddRange(nextGlobalDataBuffer);
                    nextGlobalDataBuffer.Clear();
                }
                if (currentDataBuffer != null) {
                    currentDataBuffer.Clear();
                    if( nextFrameDataBuffer != null)
                    {
                        currentDataBuffer.AddRange(nextFrameDataBuffer);
                    }
                }
                if(nextFrameDataBuffer != null)
                {
                    nextFrameDataBuffer.Clear();
                }
            }catch(System.Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }
            return true;
        }


        private bool GetNextBlockFrame(out uint frameIdx )
        {
            byte[] data = ReadFromFile(currentFilePath, this.currentFilePos, 20 + 12);
            ushort type = GetUShort(data, 20);
            if (type != MessageFrame)
            {
                frameIdx = 0;
                return false;
            }
            if (isAlignedMemoryAccess)
            {
                frameIdx = GetUInt( data , 24);
            }
            else
            {
                frameIdx = GetUInt(data, 22);
            }
            return true;
        }
        

        private void ReadBlock(bool isOverFrameBlock)
        {

            byte[] blockHeader = ReadFromFile(currentFilePath, this.currentFilePos , 20);
            ulong threadId = GetULongValue(blockHeader, 8);
            uint length = GetUInt(blockHeader, 16);
            this.currentFilePos += 20;
            

            byte[] blockBodyAndFooter = ReadFromFile(currentFilePath, this.currentFilePos, (int)length + 8);

            this.currentFilePos += length;
            this.currentFilePos += 8;

            if ( threadId == BlockHeaderGlobalThreadId)
            {
                nextGlobalDataBuffer.AddRange(blockHeader);
                nextGlobalDataBuffer.AddRange(blockBodyAndFooter);
            }
            if (isOverFrameBlock)
            {
                nextFrameDataBuffer.AddRange(blockHeader);
                nextFrameDataBuffer.AddRange(blockBodyAndFooter);
            }
            else
            {
                currentDataBuffer.AddRange(blockHeader);
                currentDataBuffer.AddRange(blockBodyAndFooter);
            }
        }


        private static ushort GetUShort(byte[] data, int offset)
        {
            ushort val = (ushort)( (data[offset + 0] << 0 ) + (data[offset + 1] << 8) );
            return val;
        }

        private static uint GetUInt(byte[] data,int offset)
        {
            uint val = ((uint)data[offset+0] << 0) + 
                ((uint)data[offset + 1] << 8) + 
                ((uint)data[offset + 2] << 16) + 
                ((uint)data[offset + 3] << 24);
            return val;
        }

        public static ulong GetULongValue(byte[] bin, int offset)
        {
            return (ulong)(bin[offset + 0] << 0) +
                (ulong)(bin[offset + 1] << 8) +
                (ulong)(bin[offset + 2] << 16) +
                (ulong)(bin[offset + 3] << 24) +
                (ulong)(bin[offset + 4] << 32) +
                (ulong)(bin[offset + 5] << 40) +
                (ulong)(bin[offset + 6] << 48) +
                (ulong)(bin[offset + 7] << 56);
        }
        private static byte[] ReadFromFile( string file , long offset,int length)
        {
            byte[] data = new byte[length];

            using (FileStream fs = File.OpenRead(file))
            {
                fs.Position = offset;
                int read = fs.Read(data, 0, length);
                if( read < length) { throw new System.Exception("ReadError " + length + " - " + read); }
                fs.Close();
            }

            return data;

        }

    }
}