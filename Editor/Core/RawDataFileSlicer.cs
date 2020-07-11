using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace UTJ.ProfilerLogSplit
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
        private long currentFileLength;

        private int frameNum = 0;
        private uint startFrameIdx = 0;
        private float progress;
        private bool isComplete;


        // message Info
        internal struct FrameMessageInfo
        {
            public ulong threadId;
            public uint frameIdx;
            public int dataPosIndex;
        }
        internal struct FileBlockInfoWithThreadId
        {
            public FileBlockInfo blockInfo;
            public ulong threadId;
        }


        private List<FileBlockInfoWithThreadId> threadDatas;

        private List<FrameMessageInfo> frameMessages;


        public static bool IsRawData(string path)
        {
            uint header = 0;
            using (FileStream fs = File.OpenRead(path))
            {
                byte[] bin = ReadFromFile(fs, 36);
                header = GetUInt(bin, 0);
                fs.Close();
            }
            return (FileSignature == header);
        }

        public void SetFile(string path)
        {
            this.currentFilePath = path;
            this.threadDatas = new List<FileBlockInfoWithThreadId>();
            this.frameMessages = new List<FrameMessageInfo>();

            this.progress = 0.0f;
            this.isComplete = false;

            Thread thread = new Thread(this.Prepare);
            thread.Start();
        }


        public bool CreateTmpFile(int frameIdx, int frameNum, string tmpFile)
        {
            FileTransfer transferObj = new FileTransfer();
            List<FileBlockInfo> datas = new List<FileBlockInfo>();
            using (FileStream writeFs = File.OpenWrite(tmpFile))
            {
                using (FileStream readFs = File.OpenRead(this.currentFilePath))
                {
                    readFs.Seek(0, SeekOrigin.Begin);
                    transferObj.Transfer(readFs, writeFs, 36);

                    var startThreadIdx = GetThreadDataIndexFromFrameIdx(frameIdx);
                    // write Global threaddata
                    this.GlobalThreadData( datas,this.threadDatas[startThreadIdx].blockInfo.Position);
                    this.TransferDatas(transferObj, datas, readFs, writeFs);
                    /*
                    UnityEngine.Debug.Log("Global Datgas " + datas.Count + "::" + datas[datas.Count-1].Position);
                    */

                    // write to body
                    var endThreadIdx = GetThreadDataIndexFromFrameIdx(frameIdx+frameNum, startThreadIdx);
                    long startFilePos = this.threadDatas[startThreadIdx].blockInfo.Position;
                    long endFilePos = this.threadDatas[endThreadIdx].blockInfo.Position;
                    readFs.Position = startFilePos;
                    transferObj.Transfer(readFs, writeFs, endFilePos - startFilePos);
                    /*
                    UnityEngine.Debug.Log("Frame " + frameIdx + ";" + frameNum +
                        "(" + startThreadIdx + "-" + endThreadIdx + 
                        "(" + startFilePos + "-" +endFilePos);
                        */
                    // write thread data...
                    datas.Clear();
                    this.GetNextFrameOtherThreadDatas(datas,frameIdx+frameNum+1,endThreadIdx);
                    this.TransferDatas(transferObj, datas, readFs, writeFs);
                }
            }

            return true;
        }

        private void TransferDatas(FileTransfer transferObj,List<FileBlockInfo> datas,FileStream readFs, FileStream writeFs)
        {
            foreach (var data in datas)
            {
                readFs.Position = data.Position;
                transferObj.Transfer(readFs, writeFs, data.Size);
            }

        }


        private void GetNextFrameOtherThreadDatas(List<FileBlockInfo> datas,int frame ,int endThreadIdx)
        {
            var nextIdx = GetThreadDataIndexFromFrameIdx(frame, endThreadIdx);

            for(int i = endThreadIdx; i < nextIdx; ++i)
            {
                if( this.threadDatas[i].threadId != this.mainThreadId)
                {
                    datas.Add(this.threadDatas[i].blockInfo);
                }
            }

        }


        private int GlobalThreadData(List<FileBlockInfo> datas,long pos,int startIdx = 0)
        {
            for(int i = startIdx; i < this.threadDatas.Count;++i)
            {
                var data = threadDatas[i];
                if( data.threadId != BlockHeaderGlobalThreadId)
                {
                    continue;
                }
                if( data.blockInfo.Position < pos)
                {
                    datas.Add(data.blockInfo);
                }
                else
                {
                    return i;
                }
            }
            return startIdx;
        }



        public bool IsPrepareDone
        {
            get
            {
                return this.isComplete;
            }
        }
        public float PrepareProgress
        {
            get
            {
                return this.progress;
            }
        }
        public int FrameNum
        {
            get
            { 
                return this.frameNum; 
            }
        }

         
        private void Prepare() { 
            //
            using (FileStream fs = File.OpenRead(currentFilePath))
            {
                // サイズ
                this.currentFileLength = fs.Length;
                // file header
                byte[] fileHeader = ReadFromFile(fs, 36);
                this.version = GetUInt(fileHeader, 8);
                this.mainThreadId = GetULongValue(fileHeader, 28);
                this.isAlignedMemoryAccess = (fileHeader[5] != 0);


                // read Blocks
                this.ReadBlocks(fs);
                fs.Close();
            }
            // フレーム数算出
            this.frameNum = CalculateFrameNum(out startFrameIdx);

            // 終了
            this.isComplete = true;
            this.progress = 1.0f;
        }


        private int GetThreadDataIndexFromFrameIdx (int frameIdx,int startIdx = 0)
        {
            uint actualFrame = this.startFrameIdx + (uint)frameIdx;

            for( int i = startIdx; i < frameMessages.Count;++i)
            {
                var message = frameMessages[i];
                if(message.frameIdx == actualFrame)
                {
                    return message.dataPosIndex;
                }
            }
            return 0;
        }

        private int CalculateFrameNum(out uint minFrameIdx)
        {
            minFrameIdx = uint.MaxValue;
            uint maxFrameidx = uint.MinValue;
            foreach(var message in frameMessages)
            {
                if (message.frameIdx < minFrameIdx)
                {
                    minFrameIdx = message.frameIdx;
                }
                if (message.frameIdx > maxFrameidx)
                {
                    maxFrameidx = message.frameIdx;
                }
            }
            return (int)(maxFrameidx - minFrameIdx) + 1;
        }

        private void ReadBlocks(FileStream fs)
        {
            while (true)
            {
                if (this.currentFileLength <= fs.Position)
                {
                    break;
                }
                this.progress = (float)((double)fs.Position / (double)this.currentFileLength);

                long position = fs.Position;
                byte[] blockHeader = ReadFromFile(fs, 20);
                byte[] messageInfo = ReadFromFile(fs, 12);

                ulong threadId = GetULongValue(blockHeader, 8);
                uint length = GetUInt(blockHeader, 16);
                uint frameIdx;
                bool isMessage = ReadFrameMessage(messageInfo, out frameIdx);

                if (isMessage)
                {
                    var frameMsgInfo = new FrameMessageInfo()
                    {
                        threadId = threadId,
                        frameIdx = frameIdx,
                        dataPosIndex = this.threadDatas.Count
                    };
                    frameMessages.Add(frameMsgInfo);
                }

                /* header + size + footer */
                var blockInfo = new FileBlockInfo(position, 20 + length + 8);
                FileBlockInfoWithThreadId info = new FileBlockInfoWithThreadId()
                {
                    blockInfo = blockInfo,
                    threadId = threadId
                };
                this.threadDatas.Add(info);
                fs.Position = blockInfo.Position + blockInfo.Size;
            }
        }
        private bool ReadFrameMessage(byte[] data,out uint frameIdx)
        {
            ushort type = GetUShort(data, 0);
            if (type != MessageFrame)
            {
                frameIdx = 0;
                return false;
            }
            if (this.isAlignedMemoryAccess)
            {
                frameIdx = GetUInt(data, 4);
            }
            else
            {
                frameIdx = GetUInt(data, 2);
            }
            return true;
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
        private static ulong GetULongValue(byte[] bin, int offset)
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
        private static byte[] ReadFromFile( FileStream fs,int length)
        {
            byte[] data = new byte[length];
            int read = fs.Read(data, 0, length);
            if (read < length) { throw new System.Exception("ReadError " + length + " - " + read); }

            return data;
        }

    }
}