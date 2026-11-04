using System;
using ColorBlockJam.Core.Model;
using ColorBlockJam.Core.Service;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Signals;
using ColorBlockJam.Views;
using MessagePipe;
using VContainer.Unity;

namespace ColorBlockJam.Core.Controller
{
    // Teaches a booster once, on its unlock level (yarm_stack's BoosterTutorialController). Its promote popup comes
    // first; then the screen dims around the booster's button and a hand taps it, the only thing that takes a tap.
    // A booster that needs a target then has the board offer one block (SandBox, told by BoosterTutorialSignal) and the
    // hand taps that. Using the booster finishes the tutorial; anything else stops it until the level is played again.
    // The Timer waits for the level clock to run, as its button does.
    public class BoosterTutorialController : IInitializable, IDisposable
    {
        private readonly ScreenController _screenController;
        private readonly PlayerController _playerController;
        private readonly CurrencyController _currencyController;
        private readonly BoosterSettings _boosterSettings;
        private readonly CurrencySettings _currencySettings;
        private readonly StorageService _storageService;
        private readonly IPopupService _popupService;
        private readonly IPublisher<BoosterTutorialSignal> _tutorialPublisher;
        private readonly ISubscriber<BoosterTutorialTargetSignal> _targetSubscriber;
        private readonly ISubscriber<BoosterRequestedSignal> _requestedSubscriber;
        private readonly ISubscriber<BoosterEndedSignal> _endedSubscriber;
        private readonly ISubscriber<LevelTimeChangedSignal> _timeChangedSubscriber;
        private readonly ISubscriber<LevelEndedSignal> _levelEndedSubscriber;

        private IDisposable _subscriptions;
        // The booster to teach on this level, until its tutorial starts.
        private CurrencyType? _pending;
        private CurrencyType? _active;

        private GamePlayScreenView Hud => _screenController.GamePlayScreenView;

        public BoosterTutorialController(ScreenController screenController, PlayerController playerController,
            CurrencyController currencyController, BoosterSettings boosterSettings, CurrencySettings currencySettings,
            StorageService storageService, IPopupService popupService,
            IPublisher<BoosterTutorialSignal> tutorialPublisher,
            ISubscriber<BoosterTutorialTargetSignal> targetSubscriber,
            ISubscriber<BoosterRequestedSignal> requestedSubscriber, ISubscriber<BoosterEndedSignal> endedSubscriber,
            ISubscriber<LevelTimeChangedSignal> timeChangedSubscriber, ISubscriber<LevelEndedSignal> levelEndedSubscriber)
        {
            _screenController = screenController;
            _playerController = playerController;
            _currencyController = currencyController;
            _boosterSettings = boosterSettings;
            _currencySettings = currencySettings;
            _storageService = storageService;
            _popupService = popupService;
            _tutorialPublisher = tutorialPublisher;
            _targetSubscriber = targetSubscriber;
            _requestedSubscriber = requestedSubscriber;
            _endedSubscriber = endedSubscriber;
            _timeChangedSubscriber = timeChangedSubscriber;
            _levelEndedSubscriber = levelEndedSubscriber;
        }

        public void Initialize()
        {
            _screenController.ScreenChanged += OnScreenChanged;
            _subscriptions = DisposableBag.Create(
                _targetSubscriber.Subscribe(OnTargetShown),
                _requestedSubscriber.Subscribe(OnRequested),
                _endedSubscriber.Subscribe(OnEnded),
                _timeChangedSubscriber.Subscribe(OnTimeChanged),
                _levelEndedSubscriber.Subscribe(_ => Stop()));
        }

        public void Dispose()
        {
            _screenController.ScreenChanged -= OnScreenChanged;
            _subscriptions?.Dispose();
            _subscriptions = null;
        }

        // The gameplay screen shows once the level is on the board.
        private void OnScreenChanged(ScreenType screen)
        {
            Stop();
            _pending = screen == ScreenType.GamePlayScreen ? BoosterToTeach() : null;
            if (_pending.HasValue && _pending.Value != CurrencyType.Booster1) Begin();
        }

        private void OnTimeChanged(LevelTimeChangedSignal signal)
        {
            if (_pending == CurrencyType.Booster1 && signal.IsClockRunning && !signal.IsClockFrozen) Begin();
        }

        private CurrencyType? BoosterToTeach()
        {
            var level = _playerController.GetVisualLevel();
            var completed = Model().completed;
            foreach (var booster in _boosterSettings.Boosters)
            {
                if (booster.UnlockLevel == level && !completed.Contains(booster.Type)) return booster.Type;
            }

            return null;
        }

        private void Begin()
        {
            var type = _pending.Value;
            _pending = null;
            // The player is given a booster to learn it with, so pressing the button cannot ask for coins.
            if (_boosterSettings.GrantBoosterIfNone && _currencyController.GetCurrency(type) <= 0)
                _currencyController.AddCurrency(new CurrencyData(type, 1), true);

            var data = _boosterSettings.Get(type);
            _popupService.AddPopup(new BoosterPromotePopupParameters
            {
                Icon = _currencySettings.IconOf(type),
                Title = data.TutorialTitle,
                Info = data.TutorialInfo,
                OnCloseAction = () => ShowButton(type)
            });
        }

        private void ShowButton(CurrencyType type)
        {
            // The level may have ended or been left while the popup was open.
            if (_screenController.ScreenStatus != ScreenType.GamePlayScreen) return;

            var button = Hud.BoosterButton(type);
            if (button == null) return;

            _active = type;
            _tutorialPublisher.Publish(new BoosterTutorialSignal(type, true));
            Hud.Tutorial.ShowButtonHint(button, _boosterSettings);
        }

        // The button was pressed: the hand leaves it; for a target the HUD lets taps through to the board.
        private void OnRequested(BoosterRequestedSignal signal)
        {
            if (_active != signal.Type) return;

            Hud.Tutorial.Hide();
            Hud.SetRaycasts(false);
        }

        private void OnTargetShown(BoosterTutorialTargetSignal signal)
        {
            if (_active.HasValue) Hud.Tutorial.ShowTapHint(signal.ScreenPoint, _boosterSettings);
        }

        private void OnEnded(BoosterEndedSignal signal)
        {
            if (_active != signal.Type) return;

            if (signal.Used)
            {
                Model().completed.Add(signal.Type);
                _storageService.SaveStorage();
            }

            Stop();
        }

        private void Stop()
        {
            _pending = null;
            if (!_active.HasValue) return;

            _tutorialPublisher.Publish(new BoosterTutorialSignal(_active.Value, false));
            _active = null;
            Hud.Tutorial.Hide();
            Hud.SetRaycasts(true);
        }

        private BoosterTutorialModel Model()
        {
            var storage = _storageService.StorageModel;
            if (storage.boosterTutorialModel == null) storage.boosterTutorialModel = new BoosterTutorialModel();
            return storage.boosterTutorialModel;
        }
    }
}
