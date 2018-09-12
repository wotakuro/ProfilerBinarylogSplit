
using System.IO;
using UnityEngine;

namespace ProfilerBinlogSplit
{
    public class LogDataFileSlicer : ILogFileSlicer
    {

        private long currentFilePos = 0;
        private int currentFrame = 0;
        private string filePath;

        public LogDataFileSlicer(string path)
        {
            this.filePath = path;
        }
        public int GetCurrentFrame()
        {
            return currentFrame;
        }

        public bool CreateTmpFile(int frameNum,string TmpFileName)
        {
            bool flag = false;
            using (FileStream fs = File.OpenRead(this.filePath))
            {
#if !UNITY_2017_3_OR_NEWER
                TmpFileName += ".data";
#endif
                using (FileStream writeFs = File.Open(TmpFileName, FileMode.Create))
                {
                    if (currentFilePos != 0)
                    {
                        fs.Seek(currentFilePos, SeekOrigin.Begin);
                    }
                    for (int i = 0; i < frameNum; ++i)
                    {
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
                        writeFs.Write(header, 0, header.Length);
                        byte[] buffer = new byte[size];
                        fs.Read(buffer, 0, size);
                        writeFs.Write(buffer, 0, buffer.Length);
                        currentFilePos += size + header.Length;
                        currentFrame++;
                        flag = true;
                    }
                }
            }
            return flag;
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