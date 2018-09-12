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

        private ulong mainThreadId;
        private uint version;
        private string currentFilePath;
        private long currentFilePos;
        private int currentFrame;

        private MemoryStream globalDataStream;
        private MemoryStream currentDataStream;

        public static bool IsRawData(string path)
        {
            var bin = ReadFromFile(path, 0, 4);
            uint header = GetUInt(bin, 0);
            return (FileSignature == header);
        }

        public RawDataFileSlicer(string path)
        {
            currentFilePath = path;
            globalDataStream = new MemoryStream(1024*1024);
            currentDataStream = new MemoryStream(1024 * 1024);
            // read header
            byte[] fileHeader = ReadFromFile(path, 0, 36);
            version = GetUInt(fileHeader, 8);
            mainThreadId = GetULongValue(fileHeader, 28);

            // append GlobalData
            globalDataStream.Write(fileHeader, 0, fileHeader.Length);
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
                for (int i = 0; i < 10; ++i) { this.ReadBlock(); }
                using (FileStream writeFs = File.Open(tmpFile + ".data", FileMode.Create))
                {
                    writeFs.Write(globalDataStream.ToArray(), 0, (int)globalDataStream.Length);
                    writeFs.Write(currentDataStream.ToArray(), 0, (int)currentDataStream.Length);
                }
                if (currentDataStream != null) {
                    currentDataStream.Close();
                    currentDataStream = new MemoryStream();
                }

                UnityEngine.Debug.Log("globalDataStream " + globalDataStream.Length);
                UnityEngine.Debug.Log("currentDataStream " + currentDataStream.Length);
                currentFrame += frameNum;
            }catch(System.Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }
            return true;
        }

        private void ReadBlock()
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
                globalDataStream.Write(blockHeader, 0, blockHeader.Length);
                globalDataStream.Write(blockBodyAndFooter, 0, blockBodyAndFooter.Length);
            }
            currentDataStream.Write(blockHeader, 0, blockHeader.Length);
            currentDataStream.Write(blockBodyAndFooter, 0, blockBodyAndFooter.Length);
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