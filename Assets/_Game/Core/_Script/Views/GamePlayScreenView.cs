using System;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Signals;
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

        [SerializeField] private Button settingsButton;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI resultText;

        private IPopupService _popupService;
        private ISubscriber<LevelTimeChangedSignal> _timeChangedSubscriber;
        private ISubscriber<LevelEndedSignal> _levelEndedSubscriber;
        private ISubscriber<TimePurchasedSignal> _timePurchasedSubscriber;
        private IDisposable _subscriptions;

        [Inject]
        public void Construct(IPopupService popupService,
            ISubscriber<LevelTimeChangedSignal> timeChangedSubscriber,
            ISubscriber<LevelEndedSignal> levelEndedSubscriber,
            ISubscriber<TimePurchasedSignal> timePurchasedSubscriber)
        {
            _timePurchasedSubscriber = timePurchasedSubscriber;
            _popupService = popupService;
            _timeChangedSubscriber = timeChangedSubscriber;
            _levelEndedSubscriber = levelEndedSubscriber;
        }

        public void Init()
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);

            var bag = DisposableBag.CreateBuilder();
            _timeChangedSubscriber.Subscribe(OnTimeChanged).AddTo(bag);
            _levelEndedSubscriber.Subscribe(OnLevelEnded).AddTo(bag);
            // Play On resumes the lost level.
            _timePurchasedSubscriber.Subscribe(_ => resultText.gameObject.SetActive(false)).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public override void Dispose()
        {
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
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

        // Leaving the level goes through the settings popup (Quit), like knit_Out.
        private void OnSettingsClicked()
        {
            _popupService.AddPopup(new InGameSettingsPopupParameters());
        }
    }
}
