using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;

#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif

namespace UTJ.ProfilerLogSplit
{
    public class BinLogToUnityProfiler : EditorWindow
    {
        private const string TmpFileName = "Temp/tmpProfiler.data";

        private PrepareProgressUI progressUI;
        private VisualElement afterPrepareElement;
        private MinMaxSlider frameSlider;
        private IntegerField minFrame;
        private IntegerField maxFrame;

        private Label FileNameLabel;
        private Label FileInfoLabel;

        private bool isExecuting = false;


        private ILogFileSlicer slicer;
        //        private IntegerField

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

            this.FileNameLabel = this.rootVisualElement.Q<Label>("FileName");
            this.rootVisualElement.Q<Button>("SelFileBtn").clickable.clicked += OnClickFileSelectBtn;
            afterPrepareElement = this.rootVisualElement.Q<VisualElement>("AfterAnalyze");
            afterPrepareElement.visible = false;
            afterPrepareElement.Q<Button>("SendProfilerBtn").clickable.clicked += OnClickSendToProfiler;

            afterPrepareElement.Q<Button>("NextBlockBtn").clickable.clicked += OnClickNextBtn;

            this.frameSlider = afterPrepareElement.Q<MinMaxSlider>("ProfilerFrame");
            this.minFrame = afterPrepareElement.Q<IntegerField>("ProfilerMinFrame");
            this.maxFrame = afterPrepareElement.Q<IntegerField>("ProfilerMaxFrame");
            this.FileInfoLabel = afterPrepareElement.Q<Label>("Result");

            this.frameSlider.RegisterCallback<ChangeEvent<Vector2>>(OnChangeSlider);
            this.minFrame.RegisterCallback<ChangeEvent<int>>(OnChangeMinValue);
            this.maxFrame.RegisterCallback<ChangeEvent<int>>(OnChangeMaxValue);

            this.progressUI = new PrepareProgressUI();
        }

        private void OnChangeSlider(ChangeEvent<Vector2> changeEvent)
        {
            var newValue = new Vector2Int(Mathf.RoundToInt(changeEvent.newValue.x),
                Mathf.RoundToInt(changeEvent.newValue.y) );
            var prevValue = new Vector2Int(Mathf.RoundToInt(changeEvent.previousValue.x),
                Mathf.RoundToInt(changeEvent.previousValue.y));
            if (newValue.y - newValue.x >= MaxProfilerFrame)
            {
                if (newValue.x == prevValue.x) {
                    int tmpVal = newValue.y - MaxProfilerFrame+1;
                    this.frameSlider.minValue = tmpVal;
                }
                else if (newValue.y == prevValue.y)
                {
                    int tmpVal = newValue.x + MaxProfilerFrame-1;
                    this.frameSlider.maxValue = tmpVal;
                }
            }
            else { 
                if (minFrame.value != newValue.x) { minFrame.value = newValue.x; }
                if (maxFrame.value != newValue.y) { maxFrame.value = newValue.y; }
            }
        }


        private void OnChangeMinValue(ChangeEvent<int> changeEvent)
        {
            var newValue = changeEvent.newValue;
            if (Mathf.RoundToInt(this.frameSlider.minValue) != newValue)
            {
                this.frameSlider.minValue = newValue;
            }
        }
        private void OnChangeMaxValue(ChangeEvent<int> changeEvent)
        {
            var newValue = changeEvent.newValue;
            if (Mathf.RoundToInt(this.frameSlider.maxValue) != newValue)
            {
                this.frameSlider.maxValue = newValue;
            }
        }


        private void OnClickFileSelectBtn()
        {
            string file = EditorUtility.OpenFilePanelWithFilters("Select Profiler log file.", "", new string[] { "profiler log", "data,raw" });
            if(string.IsNullOrEmpty(file)) { return; }
            bool isRaw = RawDataFileSlicer.IsRawData(file);
            if (isRaw)
            {
                this.slicer = new RawDataFileSlicer();
            }
            else
            {
                this.slicer = new LogDataFileSlicer();
            }
            this.isExecuting = true;
            this.slicer.SetFile(file);
            afterPrepareElement.visible = false;
            progressUI.value = 0;
            progressUI.InsertBeforeElement(afterPrepareElement);
            this.FileNameLabel.text = file;
        }

        private void Update()
        {
            if( this.slicer != null && this.isExecuting)
            {
                float progress = this.slicer.PrepareProgress;
                this.progressUI.value = progress;
                if (this.slicer.IsPrepareDone)
                {
                    this.OnPreapareDone();
                    this.isExecuting = false;
                }
            }
        }

        private void OnPreapareDone()
        {
            if( slicer == null) { return; }
            this.progressUI.RemoveFromParent();
            this.afterPrepareElement.visible = true;
            this.frameSlider.lowLimit = 0;
            this.frameSlider.highLimit = slicer.FrameNum;

            this.frameSlider.minValue = 0;
            int frameNum = this.MaxProfilerFrame - 1;
            if( frameNum > slicer.FrameNum)
            {
                frameNum = slicer.FrameNum;
            }
            this.frameSlider.maxValue = frameNum;

            this.FileInfoLabel.text = "Frames:" + slicer.FrameNum;

        }

        private void OnClickSendToProfiler()
        {
            if (slicer == null) { return; }
            int startFrame = Mathf.RoundToInt(this.frameSlider.minValue);
            int endFrame = Mathf.RoundToInt(this.frameSlider.maxValue);
            int frameNum = endFrame - startFrame +1;

            bool flag = slicer.CreateTmpFile(startFrame,frameNum, TmpFileName);
            if (flag)
            {
                Profiler.AddFramesFromFile(TmpFileName);
            }
        }

        private void OnClickNextBtn()
        {
            var frameVal = new Vector2Int(Mathf.RoundToInt(this.frameSlider.value.x),
                 Mathf.RoundToInt(this.frameSlider.value.y));
            int frameNum = frameVal.y - frameVal.x;
            int minFrame = frameVal.y + 1;

            frameSlider.value = new Vector2(minFrame, minFrame + frameNum);
        }

        private static VisualElement CloneTree(VisualTreeAsset asset)
        {
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            return asset.CloneTree();
#else
            return asset.CloneTree(null);
#endif
        }

        private int MaxProfilerFrame
        {
            get
            {
                return 300;
            }
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