using System.IO;
using UnityEditor.PackageManager.Requests;

namespace ProfilerBinlogSplit
{
    public class FileTransfer
    {
        byte[] buffer = new byte[1024];

        public void Transfer(FileStream readFs,FileStream writeFs,long size)
        {
            int loopNum = (int)((size + buffer.Length - 1) / buffer.Length);

            for( int i = 0; i < loopNum; ++i)
            {
                long requestSize = size;
                if(requestSize > buffer.Length) { requestSize = buffer.Length; }

                int readSize = readFs.Read(buffer, 0, (int)requestSize);
                if( readSize < requestSize) { throw new System.Exception("ReadError " + requestSize +" "+readSize); }
                writeFs.Write(buffer, 0, readSize);

                size -= requestSize;
            }
        }
    }
}