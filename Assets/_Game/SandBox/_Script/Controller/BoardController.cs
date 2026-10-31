using System;
using ColorBlockJam.Interfaces;
using ColorBlockJam.SandBox.Board;
using ColorBlockJam.Utils;
using UnityEngine;
using VContainer.Unity;

namespace ColorBlockJam.SandBox
{
    public class BoardController : IStartable, ITickable, IDisposable
    {
        private readonly BoardSettings _boardSettings;
        private readonly ILevelService _levelService;
        private readonly BoardViewController _boardViewController;

        public BoardSimulation Simulation { get; private set; }

        public BoardController(BoardSettings boardSettings, ILevelService levelService,
            BoardViewController boardViewController)
        {
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
