using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif


namespace ProfilerBinlogSplit
{

    public interface ILogFileSlicer
    {
        int GetCurrentFrame();
        bool CreateTmpFile(int frameNum, string tmpFile);
    }

    public class BinLogToUnityProfiler : EditorWindow
    {
        private const string TmpFileName = "tmp.log";
        private string filePath;
        private int readFrameNum = 100;

        private ILogFileSlicer slicer;

        [MenuItem("Tools/UTJ/ProfilerBinlogSlice")]
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
                this.filePath = EditorUtility.OpenFilePanelWithFilters("", "Select BinaryLogFile", new string[] { "profiler log", "data,raw" });
            }
            if (oldFile != this.filePath)
            {
                slicer = null;
            }
            EditorGUILayout.EndHorizontal();


            readFrameNum = EditorGUILayout.IntField("ReadFrameNum", readFrameNum);

            if (GUILayout.Button("Send to Profiler"))
            {
                try
                {
                    if(slicer == null)
                    {
                        if (RawDataFileSlicer.IsRawData(this.filePath))
                        {
                            slicer = new RawDataFileSlicer(this.filePath);
                        }
                        else
                        {
                            slicer = new LogDataFileSlicer(this.filePath);
                        }
                    }
                    bool flag = slicer.CreateTmpFile(readFrameNum,TmpFileName);
                    if (flag)
                    {
                        Profiler.AddFramesFromFile(TmpFileName);
                    }
                }
                catch (System.Exception ed) { }
            }
            if (slicer != null)
            {
                GUILayout.Label("Frame " + slicer.GetCurrentFrame());
                if (GUILayout.Button("Reset Frame"))
                {
                    slicer = null;
                }
            }

        }
        
    }
}