using System;
using System.Collections.Generic;
using ColorBlockJam.Core.Level;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using ColorBlockJam.SandBox.Board;
using ColorBlockJam.Signals;
using MessagePipe;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // Runs the in-game boosters the HUD asks for. Every request is answered with
    // a BoosterEndedSignal, so the HUD buttons unlock whatever happened; Used tells the HUD to spend one. A booster
    // that needs a target waits in a BoardSelection until BoardDragController hands it a tap.
    public class BoardBoosterController : IDisposable
    {
        private readonly BoardController _boardController;
        private readonly BoosterSettings _boosterSettings;
        private readonly BoardSelection _selection;
        private readonly BoardViewController _boardViewController;
        private readonly BoardCameraView _cameraView;
        private readonly BoosterAnimationSettings _animationSettings;
        private readonly IPublisher<BoosterEndedSignal> _endedPublisher;
        private readonly ISubscriber<BoosterRequestedSignal> _requestedSubscriber;
        private readonly ISubscriber<LevelEndedSignal> _levelEndedSubscriber;
        private readonly ISubscriber<BoosterTutorialSignal> _tutorialSubscriber;
        private readonly IPublisher<BoosterTutorialTargetSignal> _tutorialTargetPublisher;
        private readonly List<BlockState> _targets = new List<BlockState>();

        private IDisposable _subscriptions;
        private CurrencyType? _selecting;
        private Vector2 _buttonScreenPoint;
        private RocketProjectileView _rocket;
        private HammerView _hammer;
        // A rocket or hammer is on screen.
        private bool _flying;
        // The booster a tutorial is teaching: it offers one block and taps elsewhere do not cancel it.
        private CurrencyType? _tutorial;

        public bool IsSelecting => _selecting.HasValue && !_flying;
        // A rocket or hammer is on its way: the board takes no input until it is done.
        public bool IsBusy => _flying;

        public BoardBoosterController(BoardController boardController, BoosterSettings boosterSettings,
            BoardViewController boardViewController, BoardCameraView cameraView, BoosterAnimationSettings animationSettings,
            IPublisher<BoosterEndedSignal> endedPublisher, ISubscriber<BoosterRequestedSignal> requestedSubscriber,
            ISubscriber<LevelEndedSignal> levelEndedSubscriber, ISubscriber<BoosterTutorialSignal> tutorialSubscriber,
            IPublisher<BoosterTutorialTargetSignal> tutorialTargetPublisher)
        {
            _tutorialSubscriber = tutorialSubscriber;
            _tutorialTargetPublisher = tutorialTargetPublisher;
            _boardController = boardController;
            _boosterSettings = boosterSettings;
            _boardViewController = boardViewController;
            _cameraView = cameraView;
            _animationSettings = animationSettings;
            _selection = new BoardSelection(boardViewController, cameraView, animationSettings);
            _endedPublisher = endedPublisher;
            _requestedSubscriber = requestedSubscriber;
            _levelEndedSubscriber = levelEndedSubscriber;
        }

        public void Init()
        {
            _subscriptions = DisposableBag.Create(
                _requestedSubscriber.Subscribe(OnRequested),
                // Time ran out (or the level was won) while a target was being picked.
                _levelEndedSubscriber.Subscribe(_ => Cancel()),
                _tutorialSubscriber.Subscribe(signal => _tutorial = signal.Active ? signal.Type : (CurrencyType?)null));
        }

        public void Dispose()
        {
            _subscriptions?.Dispose();
            _subscriptions = null;
            if (IsSelecting) _selection.Close();
            _selecting = null;
        }

        // A board tap while selecting: point is null when the finger missed the board. Anything but a marked block
        // cancels the booster without spending it.
        public void HandleTap(BoardPoint? point)
        {
            if (!IsSelecting) return;

            var simulation = _boardController.Simulation;
            var block = point.HasValue && simulation != null ? simulation.BlockAt(point.Value) : null;
            if (!_selection.Contains(block))
            {
                if (_tutorial == _selecting) return;
                Cancel();
                return;
            }

            var type = _selecting.Value;
            _selection.Close();
            if (type == CurrencyType.Booster2)
            {
                FireRocket(block, new CellCoord((int)Math.Floor(point.Value.Col), (int)Math.Floor(point.Value.Row)));
                return;
            }

            if (type == CurrencyType.Booster3)
            {
                SwingHammer(block, new CellCoord((int)Math.Floor(point.Value.Col), (int)Math.Floor(point.Value.Row)));
                return;
            }

            if (type == CurrencyType.Booster4) _boardViewController.MarkVacuumed(block);
            var used = simulation.RemoveBlock(block);
            _boardController.Flush();
            End(type, used);
        }

        private void OnRequested(BoosterRequestedSignal signal)
        {
            switch (signal.Type)
            {
                case CurrencyType.Booster1:
                    End(signal.Type, UseTimer());
                    break;
                case CurrencyType.Booster2:
                case CurrencyType.Booster3:
                case CurrencyType.Booster4:
                    // Rocket breaks one piece (targets marked); Hammer and Vacuum take a whole block (a hammer blow /
                    // suck in).
                    _buttonScreenPoint = signal.ScreenPoint;
                    if (!TrySelect(signal.Type)) End(signal.Type, false);
                    break;
                default:
                    End(signal.Type, false);
                    break;
            }
        }

        // Timer: the clock stands still for TimerFreezeSeconds; the HUD shows the frozen clock from the time signal.
        private bool UseTimer()
        {
            var simulation = _boardController.Simulation;
            if (simulation == null || !simulation.FreezeClock(_boosterSettings.TimerFreezeSeconds)) return false;

            _boardController.Flush();
            return true;
        }

        // Any block that is not iced (nor leaving) can be taken; nothing starts while a block is held or none fits.
        private bool TrySelect(CurrencyType type)
        {
            var simulation = _boardController.Simulation;
            if (IsSelecting || simulation == null || simulation.Status != BoardStatus.Playing ||
                simulation.DraggedBlock != null)
                return false;

            _targets.Clear();
            foreach (var block in simulation.Blocks)
            {
                if (block.Ice == 0 && !block.IsExiting) _targets.Add(block);
            }

            if (_targets.Count == 0) return false;

            // shortcut: the tutorial offers the first block that fits; name one per level in the level data if a
            // tutorial needs a particular block.
            if (_tutorial == type) _targets.RemoveRange(1, _targets.Count - 1);
            _selection.Open(_targets, type == CurrencyType.Booster2);
            _selecting = type;
            if (_tutorial == type) ShowTutorialTarget(_targets[0]);
            return true;
        }

        // Where the tutorial hand taps: the block's first cell (on the board), on top of it.
        private void ShowTutorialTarget(BlockState block)
        {
            var world = _boardViewController.CellToWorld(block.Origin + block.Cells[0], _animationSettings.RocketImpactHeight);
            _tutorialTargetPublisher.Publish(
                new BoosterTutorialTargetSignal(_cameraView.GamePlayCamera.WorldToScreenPoint(world)));
        }

        // The rocket flies from under the HUD button, circles the cell and dives onto it; the rules break the piece when
        // it lands (refused then
        // if the level ended meanwhile, and nothing is spent).
        private void FireRocket(BlockState block, CellCoord cell)
        {
            var type = _selecting.Value;
            var rocket = Rocket();
            if (rocket == null)
            {
                End(type, false);
                return;
            }

            _flying = true;
            var orbit = _boardViewController.CellToWorld(cell, _animationSettings.RocketHeight);
            var flight = new RocketProjectileView.Flight
            {
                From = ButtonWorldPoint(orbit.y),
                Orbit = orbit,
                Impact = _boardViewController.CellToWorld(cell, _animationSettings.RocketImpactHeight),
                Radius = _animationSettings.RocketOrbitRadius * _boardViewController.Layout.CellSize,
                Duration = _animationSettings.RocketFlightDuration,
                Ease = _animationSettings.RocketEase,
                EndScale = _animationSettings.RocketEndScale,
                GrowDuration = _animationSettings.RocketGrowDuration,
                TouchRadius = _animationSettings.RocketTouchRadius * _boardViewController.Layout.CellSize
            };
            rocket.Fly(flight, () =>
            {
                _flying = false;
                var simulation = _boardController.Simulation;
                if (block.Cells.Count == 1) _boardViewController.MarkHit(block);
                var used = simulation != null && simulation.BreakCell(block, cell);
                _boardController.Flush();
                End(type, used);
            });
        }

        // The hammer pops up beside the tapped cell and swings down onto it; the block leaves (breaking apart at once)
        // on the hit, and the HUD buttons unlock once the hammer is gone.
        private void SwingHammer(BlockState block, CellCoord cell)
        {
            var type = _selecting.Value;
            var hammer = Hammer();
            if (hammer == null)
            {
                End(type, false);
                return;
            }

            _flying = true;
            var used = false;
            var swing = new HammerView.Swing
            {
                Target = _boardViewController.CellToWorld(cell, _animationSettings.HammerHeight),
                Size = _animationSettings.HammerSize * _boardViewController.Layout.CellSize,
                AppearDuration = _animationSettings.HammerAppearDuration,
                Travel = _animationSettings.HammerTravel * _boardViewController.Layout.CellSize,
                RaiseAngle = _animationSettings.HammerRaiseAngle,
                StrikeDuration = _animationSettings.HammerStrikeDuration,
                FadeDuration = _animationSettings.HammerFadeDuration
            };
            hammer.Strike(swing, () =>
            {
                var simulation = _boardController.Simulation;
                _boardViewController.MarkHammered(block);
                used = simulation != null && simulation.RemoveBlock(block);
                _boardController.Flush();
            }, () =>
            {
                _flying = false;
                End(type, used);
            });
        }

        // One hammer per gameplay scene, made on first use under the board root (unloads with the scene).
        private HammerView Hammer()
        {
            if (_hammer != null) return _hammer;
            if (_animationSettings.HammerPrefab == null)
            {
                Debug.LogError($"{nameof(BoardBoosterController)}: {nameof(BoosterAnimationSettings)}.{nameof(BoosterAnimationSettings.HammerPrefab)} is not set");
                return null;
            }

            _hammer = UnityEngine.Object.Instantiate(_animationSettings.HammerPrefab, _boardViewController.transform);
            _hammer.Initialize();
            _hammer.gameObject.SetActive(false);
            return _hammer;
        }

        // The point under the HUD button at the rocket's flying height.
        private Vector3 ButtonWorldPoint(float height)
        {
            var ray = _cameraView.GamePlayCamera.ScreenPointToRay(_buttonScreenPoint);
            var plane = new Plane(Vector3.up, new Vector3(0f, height, 0f));
            return plane.Raycast(ray, out var distance) ? ray.GetPoint(distance) : ray.GetPoint(1f);
        }

        // One rocket per gameplay scene, made on first use under the board root (unloads with the scene).
        private RocketProjectileView Rocket()
        {
            if (_rocket != null) return _rocket;
            if (_animationSettings.RocketPrefab == null)
            {
                Debug.LogError($"{nameof(BoardBoosterController)}: {nameof(BoosterAnimationSettings)}.{nameof(BoosterAnimationSettings.RocketPrefab)} is not set");
                return null;
            }

            _rocket = UnityEngine.Object.Instantiate(_animationSettings.RocketPrefab, _boardViewController.transform);
            _rocket.Initialize();
            _rocket.gameObject.SetActive(false);
            return _rocket;
        }

        private void Cancel()
        {
            // A flying rocket finishes on its own (the rules refuse it if the level is over).
            if (!IsSelecting) return;

            var type = _selecting.Value;
            _selection.Close();
            End(type, false);
        }

        private void End(CurrencyType type, bool used)
        {
            _selecting = null;
            _endedPublisher.Publish(new BoosterEndedSignal(type, used));
        }
    }
}
