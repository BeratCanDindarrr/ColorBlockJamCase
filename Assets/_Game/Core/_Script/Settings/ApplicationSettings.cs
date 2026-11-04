using ColorBlockJam.Const;
using UnityEngine;

namespace ColorBlockJam.Core.Settings
{
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(ApplicationSettings), fileName = nameof(ApplicationSettings))]
    public class ApplicationSettings : ScriptableObject
    {
        public string ApplicationName;

        // Applied once at startup by ApplicationController.
        public int TargetFrameRate = 60;
        public bool NeverSleep = true;
        public bool MultiTouch = false;
        public bool RunInBackground = false;
        public float FixedDeltaTime = 0.02f;
        public int TweenCapacity = 800;
        public int SequenceCapacity = 100;

        public void Apply()
        {
            // vSync off, or the frame rate below is ignored on platforms that honour it.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
            Screen.sleepTimeout = NeverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            Input.multiTouchEnabled = MultiTouch;
            Application.runInBackground = RunInBackground;
            Time.fixedDeltaTime = FixedDeltaTime;
            DG.Tweening.DOTween.SetTweensCapacity(TweenCapacity, SequenceCapacity);
        }

        private void OnValidate()
        {
            TargetFrameRate = Mathf.Max(1, TargetFrameRate);
            FixedDeltaTime = Mathf.Max(0.001f, FixedDeltaTime);
            TweenCapacity = Mathf.Max(1, TweenCapacity);
            SequenceCapacity = Mathf.Max(1, SequenceCapacity);
        }
    }
}
