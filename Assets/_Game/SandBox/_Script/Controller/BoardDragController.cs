using System;
using ColorBlockJam.Enums;
using ColorBlockJam.Interfaces;
using ColorBlockJam.SandBox.Board;
using ColorBlockJam.Signals;
using Lean.Touch;
using MessagePipe;
using Solo.MOST_IN_ONE;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // Turns finger input into a block drag: picks the block under the finger on the board plane,
    // slides it with the board rules while the finger moves and snaps it to a cell on release.
    public class BoardDragController : IDisposable
    {
        private readonly BoardController _boardController;
        private readonly BoardViewController _boardViewController;
        private readonly BoardCameraView _cameraView;
        private readonly BoardSettings _boardSettings;
        private readonly ISubscriber<OnFingerDownSignal> _fingerDownSubscriber;
        private readonly ISubscriber<OnFingerUpdateSignal> _fingerUpdateSubscriber;
        private readonly ISubscriber<OnFingerUpSignal> _fingerUpSubscriber;
        private readonly ISubscriber<PopupOpenedSignal> _popupOpenedSubscriber;
        private readonly IAudioService _audioService;
        private readonly IHapticService _hapticService;
        private readonly BoardBoosterController _boosterController;
        private readonly BoardTutorialController _tutorialController;

        private IDisposable _subscriptions;
        private BlockState _heldBlock;
        private LeanFinger _finger;
        // Finger point minus the block origin, in board space, so the block keeps its place under the finger.
        private BoardPoint _grabOffset;

        public BoardDragController(BoardController boardController, BoardViewController boardViewController,
            BoardCameraView cameraView, BoardSettings boardSettings,
            ISubscriber<OnFingerDownSignal> fingerDownSubscriber,
            ISubscriber<OnFingerUpdateSignal> fingerUpdateSubscriber,
            ISubscriber<OnFingerUpSignal> fingerUpSubscriber,
            ISubscriber<PopupOpenedSignal> popupOpenedSubscriber,
            IAudioService audioService, IHapticService hapticService, BoardBoosterController boosterController,
            BoardTutorialController tutorialController)
        {
            _tutorialController = tutorialController;
            _boosterController = boosterController;
            _audioService = audioService;
            _hapticService = hapticService;
            _boardController = boardController;
            _boardViewController = boardViewController;
            _cameraView = cameraView;
            _boardSettings = boardSettings;
            _fingerDownSubscriber = fingerDownSubscriber;
            _fingerUpdateSubscriber = fingerUpdateSubscriber;
            _fingerUpSubscriber = fingerUpSubscriber;
            _popupOpenedSubscriber = popupOpenedSubscriber;
        }

        public void Init()
        {
            _subscriptions = DisposableBag.Create(
                _fingerDownSubscriber.Subscribe(signal => OnFingerDown(signal.Finger)),
                _fingerUpdateSubscriber.Subscribe(signal => OnFingerUpdate(signal.Finger)),
                _fingerUpSubscriber.Subscribe(signal => OnFingerUp(signal.Finger)),
                _popupOpenedSubscriber.Subscribe(_ => DropHeld()));
        }

        public void Dispose()
        {
            _subscriptions?.Dispose();
            _subscriptions = null;
            Reset();
        }

        private void OnFingerDown(LeanFinger finger)
        {
            var simulation = _boardController.Simulation;
            if (simulation == null) return;
            if (_heldBlock != null)
            {
                if (finger == _finger) return;
                // The previous finger's up never came (input disabled mid-drag): end that drag first.
                if (simulation.DraggedBlock == _heldBlock) simulation.EndDrag();
                _boardController.Flush();
                Reset();
            }

            // Pressed a HUD button (e.g. Home) over the board: let the UI handle it, don't pick up a block.
            if (finger.StartedOverGui) return;
            if (_boosterController.IsBusy) return;

            var onBoard = TryGetBoardPoint(finger.ScreenPosition, out var point);
            // A booster waiting for a target takes the tap instead of the drag.
            if (_boosterController.IsSelecting)
            {
                _boosterController.HandleTap(onBoard ? point : (BoardPoint?)null);
                return;
            }

            if (!onBoard) return;

            var block = simulation.BlockAt(point);
            if (!_tutorialController.CanPick(block) || !simulation.BeginDrag(block)) return;
            _boardController.Flush();
            _tutorialController.OnDragStarted(block);
            // Picking a block clicks like a button.
            _audioService.PlaySfx(SfxName.Buttons);
            _hapticService.Play(MOST_HapticFeedback.HapticTypes.Selection);

            _heldBlock = block;
            _finger = finger;
            _grabOffset = point - BoardPoint.From(block.Origin);
        }

        private void OnFingerUpdate(LeanFinger finger)
        {
            if (finger != _finger || !IsDragging()) return;
            if (!TryGetBoardPoint(finger.ScreenPosition, out var point)) return;

            var simulation = _boardController.Simulation;
            var position = simulation.MoveDrag(point - _grabOffset);
            _boardController.Flush();
            // Pushed into a gate it fits: the rules let go of it; the view follows the BlockExitStarted event.
            if (simulation.DraggedBlock != _heldBlock)
            {
                _tutorialController.OnDragEnded(_heldBlock);
                Reset();
                return;
            }

            _boardViewController.MoveBlock(_heldBlock, position);
            _boardViewController.FollowHeld(Time.deltaTime);
        }

        private void OnFingerUp(LeanFinger finger)
        {
            if (finger != _finger) return;

            DropHeld();
        }

        // Sets the held block down where it is (finger up, or a popup opened over the board). The finger's own up later
        // finds nothing held.
        private void DropHeld()
        {
            if (!IsDragging()) return;

            _boardController.Simulation.EndDrag();
            _boardController.Flush();
            _tutorialController.OnDragEnded(_heldBlock);
            Reset();
        }

        // The simulation drops the drag by itself when the block exits or the level ends.
        private bool IsDragging()
        {
            if (_heldBlock == null) return false;

            var simulation = _boardController.Simulation;
            if (simulation != null && simulation.DraggedBlock == _heldBlock) return true;

            Reset();
            return false;
        }

        private void Reset()
        {
            _heldBlock = null;
            _finger = null;
        }

        private bool TryGetBoardPoint(Vector2 screenPosition, out BoardPoint point)
        {
            point = default;
            var layout = _boardViewController.Layout;
            if (layout == null) return false;

            var root = _boardViewController.transform;
            var plane = new Plane(root.up, root.position + root.up * _boardSettings.PickPlaneHeight);
            var ray = _cameraView.GamePlayCamera.ScreenPointToRay(screenPosition);
            if (!plane.Raycast(ray, out var enter)) return false;

            point = layout.LocalToBoard(root.InverseTransformPoint(ray.GetPoint(enter)));
            return true;
        }
    }
}
