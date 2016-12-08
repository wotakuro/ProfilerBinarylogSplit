using UnityEngine;
using System.IO;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif


public class SaveBinaryLogSample : MonoBehaviour {
    
	// Use this for initialization
    void Start()
    {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        Profiler.logFile = Path.Combine(Application.persistentDataPath, "profiler.log");
#else
        Profiler.logFile = Path.Combine(Directory.GetCurrentDirectory(), "profiler.log");
#endif
        Profiler.enableBinaryLog = true;
        Profiler.enabled = true;
	
	}
	
	// Update is called once per frame
	void Update () {
        if (Time.frameCount == 2000)
        {
            Profiler.enableBinaryLog = false;
            Profiler.enabled = false;
            Profiler.logFile = "";
        }
	}
    void OnDestroy() {
        Profiler.enableBinaryLog = false;
        Profiler.enabled = false;
        Profiler.logFile = "";
    }
}
