using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif


namespace UTJ.ProfilerLogSplit
{

    public interface ILogFileSlicer
    {
        bool IsPrepareDone { get; }
        float PrepareProgress { get; }
        int FrameNum { get; }
        void SetFile(string file);
        bool CreateTmpFile(int startFrame,int frameNum, string tmpFile);
    }

}