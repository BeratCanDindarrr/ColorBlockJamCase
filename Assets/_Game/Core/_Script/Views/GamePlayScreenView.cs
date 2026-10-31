using System;
using ColorBlockJam.Core.Controller;
using ColorBlockJam.Signals;
using Cysharp.Threading.Tasks;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    // HUD shown on top of the gameplay scene.
    public class GamePlayScreenView : BaseView
    {
        private const string WonText = "You Win!";
        private const string LostText = "Time's Up!";

        [SerializeField] private Button homeButton;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI resultText;

        private SceneLoaderController _sceneLoaderController;
        private ISubscriber<LevelTimeChangedSignal> _timeChangedSubscriber;
        private ISubscriber<LevelEndedSignal> _levelEndedSubscriber;
        private IDisposable _subscriptions;

        [Inject]
        public void Construct(SceneLoaderController sceneLoaderController,
            ISubscriber<LevelTimeChangedSignal> timeChangedSubscriber,
            ISubscriber<LevelEndedSignal> levelEndedSubscriber)
        {
            _sceneLoaderController = sceneLoaderController;
            _timeChangedSubscriber = timeChangedSubscriber;
            _levelEndedSubscriber = levelEndedSubscriber;
        }

        public void Init()
        {
            homeButton.onClick.AddListener(OnHomeClicked);

            var bag = DisposableBag.CreateBuilder();
            _timeChangedSubscriber.Subscribe(OnTimeChanged).AddTo(bag);
            _levelEndedSubscriber.Subscribe(OnLevelEnded).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public override void Dispose()
        {
            if (homeButton != null) homeButton.onClick.RemoveListener(OnHomeClicked);
            _subscriptions?.Dispose();
            _subscriptions = null;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // The timer is not reset here: the board publishes the full time on Start, which can run before Show.
        public void Show()
        {
            resultText.gameObject.SetActive(false);
            gameObject.SetActive(true);
        }

        private void OnTimeChanged(LevelTimeChangedSignal signal)
        {
            var seconds = Mathf.Max(0, signal.SecondsLeft);
            timerText.text = $"{seconds / 60}:{seconds % 60:00}";
        }

        private void OnLevelEnded(LevelEndedSignal signal)
        {
            resultText.text = signal.Won ? WonText : LostText;
            resultText.gameObject.SetActive(true);
        }

        private void OnHomeClicked()
        {
            _sceneLoaderController.CloseGamePlayScene().Forget();
        }
    }
}
