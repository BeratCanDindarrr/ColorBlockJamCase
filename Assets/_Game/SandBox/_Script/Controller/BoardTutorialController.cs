using System;
using System.Collections.Generic;
using ColorBlockJam.Core.Level;
using ColorBlockJam.SandBox.Board;
using ColorBlockJam.Signals;
using MessagePipe;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // The board side of the first-level tutorial (yarm_stack's BoardTutorialController): the block with the shortest
    // way out is the only one that can be picked, and its way (cell by cell, round the other blocks, into its gate) goes
    // to the HUD as screen points for the hand. The hand hides while the block is held and shows the way again from
    // where it was dropped; the tutorial is done once the block leaves through its gate.
    public class BoardTutorialController : IDisposable
    {
        private readonly BoardController _boardController;
        private readonly BoardViewController _boardViewController;
        private readonly BoardCameraView _cameraView;
        private readonly BoosterAnimationSettings _animationSettings;
        private readonly ISubscriber<DragTutorialSignal> _tutorialSubscriber;
        private readonly IPublisher<DragTutorialHintSignal> _hintPublisher;

        private IDisposable _subscription;
        private BlockState _block;

        public BoardTutorialController(BoardController boardController, BoardViewController boardViewController,
            BoardCameraView cameraView, BoosterAnimationSettings animationSettings,
            ISubscriber<DragTutorialSignal> tutorialSubscriber, IPublisher<DragTutorialHintSignal> hintPublisher)
        {
            _boardController = boardController;
            _boardViewController = boardViewController;
            _cameraView = cameraView;
            _animationSettings = animationSettings;
            _tutorialSubscriber = tutorialSubscriber;
            _hintPublisher = hintPublisher;
        }

        public void Init()
        {
            _subscription = _tutorialSubscriber.Subscribe(signal =>
            {
                if (signal.Active) Begin();
                else _block = null;
            });
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
            _block = null;
        }

        // Without a tutorial every block can be picked.
        public bool CanPick(BlockState block) => _block == null || block == _block;

        public void OnDragStarted(BlockState block)
        {
            if (_block != null && block == _block) _hintPublisher.Publish(new DragTutorialHintSignal(null, false));
        }

        // Released or pushed out: done once the block is on its way out, else the way from where it lies now.
        public void OnDragEnded(BlockState block)
        {
            if (_block == null || block != _block) return;

            if (_block.IsExiting)
            {
                _block = null;
                _hintPublisher.Publish(new DragTutorialHintSignal(null, true));
                return;
            }

            ShowWay();
        }

        private void Begin()
        {
            var simulation = _boardController.Simulation;
            if (simulation == null) return;

            IReadOnlyList<CellCoord> best = null;
            foreach (var block in simulation.Blocks)
            {
                if (block.Ice > 0) continue;
                var route = simulation.RouteToGate(block);
                if (route == null || best != null && route.Count >= best.Count) continue;
                best = route;
                _block = block;
            }

            if (best == null)
            {
                Debug.LogWarning($"[Tutorial] {nameof(BoardTutorialController)}: no block on this level can reach a gate; the drag tutorial is left out.");
                return;
            }

            ShowWay();
        }

        private void ShowWay()
        {
            var route = _boardController.Simulation.RouteToGate(_block);
            if (route == null)
            {
                _hintPublisher.Publish(new DragTutorialHintSignal(null, false));
                return;
            }

            _hintPublisher.Publish(new DragTutorialHintSignal(ToScreen(Corners(route)), false));
        }

        // Only where the way turns (and its ends), so the hand moves in straight runs.
        private static List<CellCoord> Corners(IReadOnlyList<CellCoord> route)
        {
            var corners = new List<CellCoord> { route[0] };
            for (var i = 1; i < route.Count - 1; i++)
            {
                var inStep = new CellCoord(route[i].Col - route[i - 1].Col, route[i].Row - route[i - 1].Row);
                var outStep = new CellCoord(route[i + 1].Col - route[i].Col, route[i + 1].Row - route[i].Row);
                if (inStep != outStep) corners.Add(route[i]);
            }

            corners.Add(route[route.Count - 1]);
            return corners;
        }

        // The hand holds the block by its first cell, on top of it.
        private Vector2[] ToScreen(List<CellCoord> origins)
        {
            var points = new Vector2[origins.Count];
            for (var i = 0; i < points.Length; i++)
            {
                var world = _boardViewController.CellToWorld(origins[i] + _block.Cells[0], _animationSettings.RocketImpactHeight);
                points[i] = _cameraView.GamePlayCamera.WorldToScreenPoint(world);
            }

            return points;
        }
    }
}
