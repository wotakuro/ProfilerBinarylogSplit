using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using UnityEngine.Profiling;

#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif

namespace ProfilerBinlogSplit
{
    public class BinLogToUnityProfiler : EditorWindow
    {
        [MenuItem("Tools/UTJ/ProfilerBinlogSlice")]
        public static void GetWindow()
        {
            EditorWindow.GetWindow<BinLogToUnityProfiler>();
        }

        private void OnEnable()
        {
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            string windowLayoutPath = "Packages/com.utj.profilerlogsplit/Editor/UI/UXML/ProfilerLogSplit.uxml";
#else
            string windowLayoutPath = "Packages/com.utj.profilerlogsplit/Editor/UI/UXML2018/ProfilerLogSplit.uxml";
#endif
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(windowLayoutPath);
            var visualElement = CloneTree(tree);
            this.rootVisualElement.Add(visualElement);
        }

        private static VisualElement CloneTree(VisualTreeAsset asset)
        {
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            return asset.CloneTree();
#else
            return asset.CloneTree(null);
#endif
        }


#if !UNITY_2019_1_OR_NEWER && !UNITY_2019_OR_NEWER
        private VisualElement rootVisualElement
        {
            get
            {
                return this.GetRootVisualContainer();
            }
        }
#endif
    }
}