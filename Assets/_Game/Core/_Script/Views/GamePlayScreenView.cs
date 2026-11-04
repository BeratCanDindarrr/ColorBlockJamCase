using System;
using ColorBlockJam.Core.Controller;
using ColorBlockJam.Core.Model;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
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
        [SerializeField] private InGameBoosterView[] boosterViews;
        [SerializeField] private TutorialView tutorialView;
        [SerializeField] private GraphicRaycaster raycaster;

        private IPopupService _popupService;
        private PlayerController _playerController;
        private ISubscriber<LevelTimeChangedSignal> _timeChangedSubscriber;
        private ISubscriber<LevelEndedSignal> _levelEndedSubscriber;
        private ISubscriber<TimePurchasedSignal> _timePurchasedSubscriber;
        private ISubscriber<CurrencyUpdateSignal> _currencyUpdateSubscriber;
        private ISubscriber<BoosterRequestedSignal> _boosterRequestedSubscriber;
        private ISubscriber<BoosterEndedSignal> _boosterEndedSubscriber;
        private CurrencyController _currencyController;
        private IDisposable _subscriptions;

        [Inject]
        public void Construct(IPopupService popupService, PlayerController playerController,
            ISubscriber<LevelTimeChangedSignal> timeChangedSubscriber,
            ISubscriber<LevelEndedSignal> levelEndedSubscriber,
            ISubscriber<TimePurchasedSignal> timePurchasedSubscriber,
            CurrencyController currencyController, BoosterSettings boosterSettings,
            IPublisher<BoosterRequestedSignal> boosterRequestedPublisher,
            ISubscriber<BoosterRequestedSignal> boosterRequestedSubscriber,
            ISubscriber<BoosterEndedSignal> boosterEndedSubscriber,
            ISubscriber<CurrencyUpdateSignal> currencyUpdateSubscriber)
        {
            _currencyController = currencyController;
            _currencyUpdateSubscriber = currencyUpdateSubscriber;
            _boosterRequestedSubscriber = boosterRequestedSubscriber;
            _boosterEndedSubscriber = boosterEndedSubscriber;
            foreach (var view in boosterViews)
                view.Construct(currencyController, playerController, boosterSettings, boosterRequestedPublisher);
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
            _currencyUpdateSubscriber.Subscribe(_ => RefreshBoosters()).AddTo(bag);
            // One booster at a time: every button waits until the board has finished with the requested one.
            _boosterRequestedSubscriber.Subscribe(_ => SetBoostersBlocked(true)).AddTo(bag);
            _boosterEndedSubscriber.Subscribe(OnBoosterEnded).AddTo(bag);
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
            SetBoostersBlocked(false);
            gameObject.SetActive(true);
        }

        private void SetBoostersBlocked(bool blocked)
        {
            foreach (var view in boosterViews) view.SetBlocked(blocked);
        }

        private void RefreshBoosters()
        {
            foreach (var view in boosterViews) view.Refresh();
        }

        private void OnTimeChanged(LevelTimeChangedSignal signal)
        {
            var seconds = Mathf.Max(0, signal.SecondsLeft);
            timerText.text = $"{seconds / 60}:{seconds % 60:00}";
            retryButton.interactable = signal.IsClockRunning;
            var timer = BoosterView(CurrencyType.Booster1);
            if (timer == null) return;
            timer.SetUnavailable(!signal.IsClockRunning || signal.IsClockFrozen);
            timer.SetEffectActive(signal.IsClockFrozen);
        }

        // The board spends nothing itself (it holds no Core controller); a used booster is taken off here.
        private void OnBoosterEnded(BoosterEndedSignal signal)
        {
            if (signal.Used) _currencyController.TryConsume(new CurrencyData(signal.Type, 1));
            SetBoostersBlocked(false);
        }

        public TutorialView Tutorial => tutorialView;

        public RectTransform BoosterButton(CurrencyType type)
        {
            var view = BoosterView(type);
            return view != null ? (RectTransform)view.transform : null;
        }

        // Off: no HUD button takes a tap and taps reach the board (a booster tutorial waiting for its target).
        public void SetRaycasts(bool on)
        {
            raycaster.enabled = on;
        }

        private InGameBoosterView BoosterView(CurrencyType type)
        {
            foreach (var view in boosterViews)
            {
                if (view.BoosterType == type) return view;
            }

            return null;
        }

        // A win goes straight on to the LevelComplete popup (ApplicationController); only a loss is shown here.
        private void OnLevelEnded(LevelEndedSignal signal)
        {
            if (signal.Won) return;

            resultText.text = LostText;
            resultText.gameObject.SetActive(true);
        }

        // Leaving the level goes through the settings popup (Quit).
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
