using System;
using ColorBlockJam.Core.Controller;
using ColorBlockJam.Enums;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using VContainer;

namespace ColorBlockJam.Views
{
    public class LoadingScreenView : BaseView
    {
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private float loadingTime = 2f;
        [SerializeField] private float dotInterval = 0.3f;
        private const string Loading = "Loading";
        private const float MinDotInterval = 0.05f;
        private static readonly string[] LoadingFrames = { Loading + ".", Loading + "..", Loading + "..." };

        #region Injection

        private ScreenController _screenController;

        [Inject]
        public void Construct(ScreenController screenController)
        {
            _screenController = screenController;
        }

        #endregion
        public void Init()
        {
            loadingText.SetText(Loading);
        }

        public async UniTaskVoid FakeLoading(ScreenType isFinishedToSetScreenType = ScreenType.MainScreen)
        {
            var token = this.GetCancellationTokenOnDestroy();
            var interval = Mathf.Max(dotInterval, MinDotInterval);
            var elapsed = 0f;
            var frame = 0;

            while (elapsed < loadingTime)
            {
                loadingText.SetText(LoadingFrames[frame]);
                frame = (frame + 1) % LoadingFrames.Length;

                var wait = Mathf.Min(interval, loadingTime - elapsed);
                await UniTask.Delay(TimeSpan.FromSeconds(wait), cancellationToken: token);
                elapsed += wait;
            }
            _screenController.SetScreen(isFinishedToSetScreenType);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Show()
        { 
            gameObject.SetActive(true);
        }
    }
}