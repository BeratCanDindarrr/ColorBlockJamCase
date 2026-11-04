using System;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Interfaces;
using ColorBlockJam.SandBox.Board;
using ColorBlockJam.Signals;
using ColorBlockJam.Utils;
using MessagePipe;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // Builds the board for a level (LevelController picks which) and runs its clock on Core's TimerService. Nothing
    // ticks per frame: the board only changes on finger input (BoardDragController) and timer ticks, and each change
    // is followed by Flush.
    public class BoardController : ITimerOwner
    {
        private readonly BoardSettings _boardSettings;
        private readonly BoardViewController _boardViewController;
        private readonly IPublisher<LevelTimeChangedSignal> _timeChangedPublisher;
        private readonly IPublisher<LevelEndedSignal> _levelEndedPublisher;
        private readonly ITimerSubscriber _timerSubscriber;
        private readonly ISubscriber<TimePurchasedSignal> _timePurchasedSubscriber;
        private readonly IPopupService _popupService;

        private IDisposable _timePurchasedSubscription;

        private int _publishedSeconds;
        private bool _publishedClockRunning;
        private bool _publishedClockFrozen;

        public BoardSimulation Simulation { get; private set; }

        public BoardController(BoardSettings boardSettings, BoardViewController boardViewController, IPublisher<LevelTimeChangedSignal> timeChangedPublisher,
            IPublisher<LevelEndedSignal> levelEndedPublisher, ITimerSubscriber timerSubscriber,
            ISubscriber<TimePurchasedSignal> timePurchasedSubscriber, IPopupService popupService)
        {
            _popupService = popupService;
            _timePurchasedSubscriber = timePurchasedSubscriber;
            _timerSubscriber = timerSubscriber;
            _timeChangedPublisher = timeChangedPublisher;
            _levelEndedPublisher = levelEndedPublisher;
            _boardViewController = boardViewController;
            _boardSettings = boardSettings;
        }

        public void CreateLevel(LevelData level)
        {
            if (level == null)
            {
                Debug.LogError($"{nameof(BoardController)}: no level to build");
                return;
            }

            Simulation = new BoardSimulation(LevelDefinitionFactory.Create(level), _boardSettings.ToRules());
            _boardViewController.Build(Simulation);
            PublishTime();
            _timerSubscriber.Subscribe(this);
            _timePurchasedSubscription = _timePurchasedSubscriber.Subscribe(OnTimePurchased);
            DebugExtension.Log($"{nameof(BoardController)}: built '{level.name}' with {Simulation.Blocks.Count} blocks");
        }

        public void UpdateTime(float intervalTime)
        {
            // The clock stops while a popup (settings, retry, ...) is open.
            if (Simulation == null || _popupService.HasOpenPopup) return;

            Simulation.AdvanceClock(intervalTime);
            Flush();
        }

        // Hands the events of the last board change to the views and reports the level end.
        public void Flush()
        {
            if (Simulation == null) return;

            // The first drag starts a waiting clock; publish so the HUD knows right away.
            if (SecondsLeft() != _publishedSeconds || Simulation.IsClockRunning != _publishedClockRunning ||
                Simulation.IsClockFrozen != _publishedClockFrozen) PublishTime();

            var events = Simulation.TakeEvents();
            _boardViewController.Apply(events);
            foreach (var boardEvent in events)
            {
                if (boardEvent.Type == BoardEventType.Won || boardEvent.Type == BoardEventType.Lost)
                    _levelEndedPublisher.Publish(new LevelEndedSignal(boardEvent.Type == BoardEventType.Won));
            }
        }

        // The level clock does not run while the app is in the background, so the away time is ignored.
        public void ResumeTime(float passedRealTime)
        {
        }

        private int SecondsLeft() => (int)Math.Ceiling(Simulation.TimeLeft);

        private void PublishTime()
        {
            _publishedSeconds = SecondsLeft();
            _publishedClockRunning = Simulation.IsClockRunning;
            _publishedClockFrozen = Simulation.IsClockFrozen;
            _timeChangedPublisher.Publish(new LevelTimeChangedSignal(_publishedSeconds, _publishedClockRunning,
                _publishedClockFrozen));
        }

        // "Play On" bought more time; a level lost on time resumes.
        private void OnTimePurchased(TimePurchasedSignal signal)
        {
            if (Simulation == null) return;

            Simulation.AddTime(signal.Seconds);
            PublishTime();
            Flush();
        }

        public void UnLoad()
        {
            _timePurchasedSubscription?.Dispose();
            _timePurchasedSubscription = null;
            _timerSubscriber.UnSubscribe(this);
            // The view root may already be destroyed when the gameplay scene unloads first.
            if (_boardViewController) _boardViewController.Clear();
            Simulation = null;
        }
    }
}
