using System;
using ColorBlockJam.Core.Model;
using ColorBlockJam.Core.Service;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using ColorBlockJam.Signals;
using ColorBlockJam.Views;
using MessagePipe;
using VContainer.Unity;

namespace ColorBlockJam.Core.Controller
{
    // The first level's tutorial, once: the board (SandBox, told by DragTutorialSignal) lets only one block be picked
    // and sends the way out to its gate, which the hand drags along until that block has left the board.
    public class DragTutorialController : IInitializable, IDisposable
    {
        private readonly ScreenController _screenController;
        private readonly PlayerController _playerController;
        private readonly BoosterSettings _settings;
        private readonly StorageService _storageService;
        private readonly IPublisher<DragTutorialSignal> _tutorialPublisher;
        private readonly ISubscriber<DragTutorialHintSignal> _hintSubscriber;
        private readonly ISubscriber<LevelEndedSignal> _levelEndedSubscriber;

        private IDisposable _subscriptions;
        private bool _active;

        private GamePlayScreenView Hud => _screenController.GamePlayScreenView;

        public DragTutorialController(ScreenController screenController, PlayerController playerController,
            BoosterSettings settings, StorageService storageService, IPublisher<DragTutorialSignal> tutorialPublisher,
            ISubscriber<DragTutorialHintSignal> hintSubscriber, ISubscriber<LevelEndedSignal> levelEndedSubscriber)
        {
            _screenController = screenController;
            _playerController = playerController;
            _settings = settings;
            _storageService = storageService;
            _tutorialPublisher = tutorialPublisher;
            _hintSubscriber = hintSubscriber;
            _levelEndedSubscriber = levelEndedSubscriber;
        }

        public void Initialize()
        {
            _screenController.ScreenChanged += OnScreenChanged;
            _subscriptions = DisposableBag.Create(
                _hintSubscriber.Subscribe(OnHint),
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
            if (screen != ScreenType.GamePlayScreen || _playerController.GetVisualLevel() != _settings.DragTutorialLevel ||
                Model().dragCompleted)
                return;

            _active = true;
            _tutorialPublisher.Publish(new DragTutorialSignal(true));
        }

        private void OnHint(DragTutorialHintSignal signal)
        {
            if (!_active) return;

            if (signal.Done)
            {
                Model().dragCompleted = true;
                _storageService.SaveStorage();
                Stop();
                return;
            }

            if (signal.Points == null) Hud.Tutorial.Hide();
            else Hud.Tutorial.ShowDragHint(signal.Points, _settings);
        }

        private void Stop()
        {
            if (!_active) return;

            _active = false;
            _tutorialPublisher.Publish(new DragTutorialSignal(false));
            Hud.Tutorial.Hide();
        }

        private BoosterTutorialModel Model()
        {
            var storage = _storageService.StorageModel;
            if (storage.boosterTutorialModel == null) storage.boosterTutorialModel = new BoosterTutorialModel();
            return storage.boosterTutorialModel;
        }
    }
}
