using System;
using ColorBlockJam.Signals;
using Cysharp.Threading.Tasks;
using Lean.Touch;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace ColorBlockJam.Core.Controller
{
    public class InputController : IInitializable, IDisposable
    {
        public Vector3 FingerPosition => LeanTouch.Fingers[0].ScreenPosition;

        private bool _inputEnabled = true, _isFingerDown;

        #region Injection

        private readonly IPublisher<OnFingerDownSignal> _fingerDownPublisher;
        private readonly IPublisher<OnFingerUpdateSignal> _fingerUpdatePublisher;
        private readonly IPublisher<OnFingerUpSignal> _fingerUpPublisher;

        public InputController(IPublisher<OnFingerDownSignal> fingerDownPublisher
            , IPublisher<OnFingerUpdateSignal> fingerUpdatePublisher
            , IPublisher<OnFingerUpSignal> fingerUpPublisher)
        {
            _fingerDownPublisher = fingerDownPublisher;
            _fingerUpdatePublisher = fingerUpdatePublisher;
            _fingerUpPublisher = fingerUpPublisher;
        }

        #endregion

        public void Initialize()
        {
            LeanTouch.OnFingerDown += OnInputFingerDown;
            LeanTouch.OnFingerUp += OnFingerUp;
            LeanTouch.OnFingerUpdate += OnFingerUpdate;
            EnableInput().Forget();
        }

        public async UniTaskVoid EnableInput()
        {
            await UniTask.DelayFrame(2);

            if (!_inputEnabled)
            {
                _inputEnabled = true;
                _isFingerDown = false;
            }
        }

        public async UniTaskVoid DisableInput()
        {
            await UniTask.DelayFrame(3);

            if (_inputEnabled)
            {
                _inputEnabled = false;
                _isFingerDown = false;
            }
        }

        private void OnInputFingerDown(LeanFinger finger)
        {
            if (!_inputEnabled || _isFingerDown) return;

            if (LeanTouch.Fingers.Count > 1)
            {
                return;
            }

            _isFingerDown = true;
            _fingerDownPublisher.Publish(new OnFingerDownSignal(finger));
        }

        private void OnFingerUpdate(LeanFinger finger)
        {
            if (_inputEnabled)
            {
                _fingerUpdatePublisher.Publish(new OnFingerUpdateSignal(finger));
            }
        }

        private void OnFingerUp(LeanFinger finger)
        {
            if (!_inputEnabled || !_isFingerDown) return;

            if (LeanTouch.Fingers.Count > 1)
            {
                if (LeanTouch.Fingers[0] == finger)
                {
                    _fingerUpPublisher.Publish(new OnFingerUpSignal(finger));
                    _isFingerDown = false;
                }
                return;
            }

            _fingerUpPublisher.Publish(new OnFingerUpSignal(finger));

            _isFingerDown = false;
        }

        public void Dispose()
        {
            LeanTouch.OnFingerDown -= OnInputFingerDown;
            LeanTouch.OnFingerUp -= OnFingerUp;
            LeanTouch.OnFingerUpdate -= OnFingerUpdate;
        }
    }
}
