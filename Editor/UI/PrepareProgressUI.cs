using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization;

#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif

namespace ProfilerBinlogSplit
{
    public class PrepareProgressUI:VisualElement
    {
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
        private ProgressBar progressBar;
#endif
        private Label label;
        public float value
        {
            set
            {
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
                progressBar.value = value * 100.0f;
#endif

                this.label.text = (value * 100.0f).ToString() + "%";
            }
        }

        public PrepareProgressUI()
        {
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            this.progressBar = new ProgressBar();
            this.Add(this.progressBar);
#endif

            this.label = new Label();
            this.Add(this.label);

            this.style.width = 200;
        }

        public void InsertBeforeElement(VisualElement element)
        {
            var parent = element.parent;
            int idx = 0;
            foreach(var child in parent.Children())
            {
                if(element == child)
                {
                    break;
                }
                idx++;
            }
            parent.Insert(idx, this);
        }

        public void RemoveFromParent()
        {
            var parent = this.parent;
            if( parent != null)
            {
                parent.Remove(this);
            }
        }
    }
}