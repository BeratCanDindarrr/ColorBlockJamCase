using System;
using ColorBlockJam.Interfaces;
using ColorBlockJam.SandBox.Board;
using ColorBlockJam.Signals;
using ColorBlockJam.Utils;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace ColorBlockJam.SandBox
{
    public class BoardController : IStartable, ITickable, IDisposable
    {
        private readonly BoardSettings _boardSettings;
        private readonly ILevelService _levelService;
        private readonly BoardViewController _boardViewController;
        private readonly IPublisher<LevelTimeChangedSignal> _timeChangedPublisher;
        private readonly IPublisher<LevelEndedSignal> _levelEndedPublisher;

        private int _publishedSeconds;

        public BoardSimulation Simulation { get; private set; }

        public BoardController(BoardSettings boardSettings, ILevelService levelService,
            BoardViewController boardViewController, IPublisher<LevelTimeChangedSignal> timeChangedPublisher,
            IPublisher<LevelEndedSignal> levelEndedPublisher)
        {
            _timeChangedPublisher = timeChangedPublisher;
            _levelEndedPublisher = levelEndedPublisher;
            _boardViewController = boardViewController;
            _boardSettings = boardSettings;
            _levelService = levelService;
        }

        public void Start()
        {
            var level = _levelService.GetCurrentLevel();
            if (level == null)
            {
                Debug.LogError($"{nameof(BoardController)}: level {_levelService.CurrentLevelIndex} is missing");
                return;
            }

            Simulation = new BoardSimulation(LevelDefinitionFactory.Create(level), _boardSettings.ToRules());
            _boardViewController.Build(Simulation);
            PublishTime();
            DebugExtension.Log($"{nameof(BoardController)}: built '{level.name}' with {Simulation.Blocks.Count} blocks");
        }

        public void Tick()
        {
            if (Simulation == null) return;

            var wasPlaying = Simulation.Status == BoardStatus.Playing;
            _boardViewController.Apply(Simulation.Tick(Time.deltaTime));
            _boardViewController.FollowHeld(Time.deltaTime);

            if (SecondsLeft() != _publishedSeconds) PublishTime();
            if (wasPlaying && Simulation.Status != BoardStatus.Playing)
                _levelEndedPublisher.Publish(new LevelEndedSignal(Simulation.Status == BoardStatus.Won));
        }

        private int SecondsLeft() => (int)Math.Ceiling(Simulation.TimeLeft);

        private void PublishTime()
        {
            _publishedSeconds = SecondsLeft();
            _timeChangedPublisher.Publish(new LevelTimeChangedSignal(_publishedSeconds));
        }

        public void Dispose()
        {
            // The view root may already be destroyed when the gameplay scene unloads first.
            if (_boardViewController) _boardViewController.Clear();
            Simulation = null;
        }
    }
}
