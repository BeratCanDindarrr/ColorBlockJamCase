using System;
using ColorBlockJam.SandBox.Board;
using ColorBlockJam.Utils;
using UnityEngine;
using VContainer.Unity;

namespace ColorBlockJam.SandBox
{
    public class BoardController : IStartable, ITickable, IDisposable
    {
        private const int FirstLevelIndex = 0;

        private readonly BoardSettings _boardSettings;
        private readonly LevelSettings _levelSettings;
        private readonly BoardViewController _boardViewController;

        public BoardSimulation Simulation { get; private set; }

        public BoardController(BoardSettings boardSettings, LevelSettings levelSettings,
            BoardViewController boardViewController)
        {
            _boardViewController = boardViewController;
            _boardSettings = boardSettings;
            _levelSettings = levelSettings;
        }

        public void Start()
        {
            var level = _levelSettings.GetLevel(FirstLevelIndex);
            if (level == null)
            {
                Debug.LogError($"{nameof(BoardController)}: level {FirstLevelIndex} is missing");
                return;
            }

            Simulation = new BoardSimulation(level.CreateBoardDefinition(), _boardSettings.ToRules());
            _boardViewController.Build(Simulation);
            DebugExtension.Log($"{nameof(BoardController)}: built '{level.name}' with {Simulation.Blocks.Count} blocks");
        }

        public void Tick()
        {
        }

        public void Dispose()
        {
            // The view root may already be destroyed when the gameplay scene unloads first.
            if (_boardViewController) _boardViewController.Clear();
            Simulation = null;
        }
    }
}
