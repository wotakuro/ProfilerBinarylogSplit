using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

namespace ProfilerBinlogSplit
{
    public class BinLogToUnityProfiler : EditorWindow
    {
        private const string TmpFileName = "tmp.log";
        private string filePath;
        private int currentFrame = 0;
        private long currentFilePos = 0;
        private int readFrameNum = 100;

        [MenuItem("Tools/ProfilerBinlogSplit")]
        public static void GetWindow()
        {
            EditorWindow.GetWindow<BinLogToUnityProfiler>();
        }


        void OnGUI()
        {
            EditorGUILayout.LabelField("Profiler Bin Log File");
            EditorGUILayout.BeginHorizontal();
            string oldFile = this.filePath;
            this.filePath = EditorGUILayout.TextField(this.filePath);
            if (GUILayout.Button("File", GUILayout.Width(40.0f)))
            {
                this.filePath = EditorUtility.OpenFilePanel("", "Select BinaryLogFile", "data");
            }
            if (oldFile != this.filePath)
            {
                this.currentFrame = 0;
                this.currentFilePos = 0;
            }
            EditorGUILayout.EndHorizontal();


            readFrameNum = EditorGUILayout.IntField("ReadFrameNum", readFrameNum);

            if (GUILayout.Button("Send to Profiler"))
            {
                try
                {
                    bool flag = CreateTmpFile(readFrameNum);
                    if (flag)
                    {
                        Profiler.AddFramesFromFile(TmpFileName);
                    }
                }
                catch (System.Exception ed) { }
            }
            GUILayout.Label("Frame " + currentFrame);
            if (GUILayout.Button("Reset Frame"))
            {
                this.currentFrame = 0;
                this.currentFilePos = 0;
            }

        }

        private bool CreateTmpFile(int frameNum)
        {
            bool flag = false;
            using (FileStream fs = File.OpenRead(this.filePath))
            {
                using (FileStream writeFs = File.Open(TmpFileName + ".data", FileMode.Create))
                {
                    if (currentFilePos != 0)
                    {
                        fs.Seek(currentFilePos, SeekOrigin.Begin);
                    }
                    for (int i = 0; i < frameNum; ++i)
                    {
                        if (fs.Length <= fs.Position) { break; }
                        byte[] header = new byte[16];
                        fs.Read(header, 0, header.Length);
                        int size = GetIntValue(header, 8);
                        writeFs.Write(header, 0, header.Length);
                        byte[] buffer = new byte[size];
                        fs.Read(buffer, 0, size);
                        writeFs.Write(buffer, 0, buffer.Length);
                        currentFilePos += size + 16;
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