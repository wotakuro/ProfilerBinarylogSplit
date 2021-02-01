
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace UTJ.ProfilerLogSplit
{
    internal struct FileBlockInfo
    {
        public long Position;
        public long Size;

        public FileBlockInfo(long pos,long size)
        {
            Position = pos;
            Size = size;
        }
    }


    public class LogDataFileSlicer : ILogFileSlicer
    {
        private float progress = 0.0f;
        private bool isCompleteFlag = false;
        private string filePath;
        private List<FileBlockInfo> blocks;

        public void SetFile(string path)
        {
            this.filePath = path;
            this.blocks = new List<FileBlockInfo>();
            this.isCompleteFlag = false;
            this.progress = 0.0f;

            Thread thread = new Thread(this.Prepare);
            thread.Start();
        }

        public bool IsPrepareDone
        {
            get
            {
                return isCompleteFlag;
            }
        }
        public float PrepareProgress
        {
            get
            {
                return progress;
            }
        }
        public int FrameNum
        {
            get
            {
                if( this.blocks == null) { return 0; }
                return this.blocks.Count;
            }
        }

        public void Prepare()
        {
            long fileSize = 0;
            long currentFilePos = 0;
            using (FileStream fs = File.OpenRead(this.filePath))
            {
                fileSize = fs.Length;

                for (int i = 0; ; ++i)
                {
                    FileBlockInfo info = new FileBlockInfo(currentFilePos, 0);
                    if (fs.Length <= fs.Position) { break; }


#if UNITY_2017_3_OR_NEWER
                    byte[] header = new byte[12];
                    fs.Read(header, 0, header.Length);
                    int size = GetIntValue(header, 4);
#else
                        byte[] header = new byte[16];
                        fs.Read(header, 0, header.Length);
                        int size = GetIntValue(header, 8);
#endif
                    info.Size = size + header.Length;
                    currentFilePos += info.Size;
                    fs.Position = currentFilePos;
                    this.progress = (float)((double)currentFilePos / (double)fileSize);
                    this.blocks.Add(info);
                }
            }
            this.isCompleteFlag = true;
            this.progress = 1.0f;
        }

        public bool CreateTmpFile(int startFrame,int frameNum,string tempFile)
        {
            FileTransfer transferObj = new FileTransfer();
            using (FileStream writeFs = File.OpenWrite(tempFile))
            {
                using (FileStream readFs = File.OpenRead(this.filePath))
                {
                    readFs.Position = this.blocks[startFrame].Position;

                    for (int i = 0; i < frameNum; ++i) {
                        transferObj.Transfer(readFs,
                            writeFs,
                            blocks[startFrame + i].Size);
                    }
                }
            }

            return true;
        }


        private static int GetIntValue(byte[] bin, int offset)
        {
            return (bin[offset + 0] << 0) +
                (bin[offset + 1] << 8) +
                (bin[offset + 2] << 16) +
                (bin[offset + 3] << 24);
        }
    }
}