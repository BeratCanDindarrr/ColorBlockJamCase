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

        public BoardSimulation Simulation { get; private set; }

        public BoardController(BoardSettings boardSettings, LevelSettings levelSettings)
        {
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
            DebugExtension.Log($"{nameof(BoardController)}: built '{level.name}' with {Simulation.Blocks.Count} blocks");
        }

        public void Tick()
        {
        }

        public void Dispose()
        {
            Simulation = null;
        }
    }
}
