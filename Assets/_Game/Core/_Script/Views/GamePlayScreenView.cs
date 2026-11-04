using System;
using ColorBlockJam.Core.Controller;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Signals;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    // HUD shown on top of the gameplay scene. Button clicks are wired in the prefab (OnSettingsClicked / OnRetryClicked).
    public class GamePlayScreenView : BaseView
    {
        private const string LostText = "Time's Up!";
        private const string LevelFormat = "Level\n<size=130%>{0}</size>";

        [SerializeField] private Button retryButton;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI resultText;

        private IPopupService _popupService;
        private PlayerController _playerController;
        private ISubscriber<LevelTimeChangedSignal> _timeChangedSubscriber;
        private ISubscriber<LevelEndedSignal> _levelEndedSubscriber;
        private ISubscriber<TimePurchasedSignal> _timePurchasedSubscriber;
        private IDisposable _subscriptions;

        [Inject]
        public void Construct(IPopupService popupService, PlayerController playerController,
            ISubscriber<LevelTimeChangedSignal> timeChangedSubscriber,
            ISubscriber<LevelEndedSignal> levelEndedSubscriber,
            ISubscriber<TimePurchasedSignal> timePurchasedSubscriber)
        {
            _timePurchasedSubscriber = timePurchasedSubscriber;
            _popupService = popupService;
            _playerController = playerController;
            _timeChangedSubscriber = timeChangedSubscriber;
            _levelEndedSubscriber = levelEndedSubscriber;
        }

        // Retry stays locked until the level's clock runs (every new board publishes its clock state).
        public void Init()
        {
            retryButton.interactable = false;
            var bag = DisposableBag.CreateBuilder();
            _timeChangedSubscriber.Subscribe(OnTimeChanged).AddTo(bag);
            _levelEndedSubscriber.Subscribe(OnLevelEnded).AddTo(bag);
            // Play On resumes the lost level.
            _timePurchasedSubscriber.Subscribe(_ => resultText.gameObject.SetActive(false)).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public override void Dispose()
        {
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
            levelText.text = string.Format(LevelFormat, _playerController.GetVisualLevel());
            gameObject.SetActive(true);
        }

        private void OnTimeChanged(LevelTimeChangedSignal signal)
        {
            var seconds = Mathf.Max(0, signal.SecondsLeft);
            timerText.text = $"{seconds / 60}:{seconds % 60:00}";
            retryButton.interactable = signal.IsClockRunning;
        }

        // A win goes straight on to the LevelComplete popup (ApplicationController); only a loss is shown here.
        private void OnLevelEnded(LevelEndedSignal signal)
        {
            if (signal.Won) return;

            resultText.text = LostText;
            resultText.gameObject.SetActive(true);
        }

        // Leaving the level goes through the settings popup (Quit), like knit_Out.
        public void OnSettingsClicked()
        {
            _popupService.AddPopup(new InGameSettingsPopupParameters());
        }

        // Try Again restarts the level; closing it keeps playing.
        public void OnRetryClicked()
        {
            _popupService.AddPopup(new LevelFailedPopupParameters());
        }
    }
}
