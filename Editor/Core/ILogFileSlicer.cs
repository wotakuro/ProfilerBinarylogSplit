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

}