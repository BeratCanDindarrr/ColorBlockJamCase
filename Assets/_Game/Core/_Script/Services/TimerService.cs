using System;
using System.Collections.Generic;
using System.Threading;
using ColorBlockJam.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace ColorBlockJam.Core.Service
{
    // Ticks every subscribed ITimerOwner about once per IntervalTime with the real time that passed (unaffected by
    // Time.timeScale). While the app is in the background ticks stop; on return each owner gets ResumeTime with the
    // seconds it was away.
    public class TimerService : ITimerSubscriber, IInitializable, IDisposable
    {
        private const float IntervalTime = 1f;

        private readonly HashSet<ITimerOwner> _timerOwners = new HashSet<ITimerOwner>();
        // Snapshot iterated each tick, so owners may subscribe/unsubscribe from inside UpdateTime/ResumeTime.
        private readonly List<ITimerOwner> _snapshot = new List<ITimerOwner>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private bool _isStarted;
        private bool _isPaused;
        private float _lastTickTime;
        private DateTime _pausedAtUtc;

        public void Initialize()
        {
            if (_isStarted) return;
            _isStarted = true;

            _lastTickTime = Time.realtimeSinceStartup;
            Application.focusChanged += OnFocusChanged;
            RunTimer(_cts.Token).Forget();
        }

        // Wall-clock UTC time in Unix seconds; kept across app restarts (life refill, install time).
        public long TotalSeconds() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public long SecondsLeft(long targetSeconds) => Math.Max(0, targetSeconds - TotalSeconds());

        public void Subscribe(ITimerOwner owner)
        {
            if (owner == null) return;
            _timerOwners.Add(owner);
        }

        public void UnSubscribe(ITimerOwner owner)
        {
            if (owner == null) return;
            _timerOwners.Remove(owner);
        }

        public void Dispose()
        {
            Application.focusChanged -= OnFocusChanged;
            if (!_cts.IsCancellationRequested) _cts.Cancel();
            _cts.Dispose();
            _timerOwners.Clear();
        }

        private async UniTaskVoid RunTimer(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(IntervalTime), DelayType.Realtime, cancellationToken: token);
                }
                catch (OperationCanceledException)
                {
                    // Leave the loop: retrying with a cancelled token would spin without ever yielding.
                    break;
                }

                var now = Time.realtimeSinceStartup;
                var elapsed = now - _lastTickTime;
                _lastTickTime = now;
                if (_isPaused) continue;

                NotifyOwners(owner => owner.UpdateTime(elapsed));
            }
        }

        private void OnFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
            {
                if (_isPaused) return;
                _isPaused = true;
                _pausedAtUtc = DateTime.UtcNow;
                return;
            }

            if (!_isPaused) return;
            _isPaused = false;
            // The background gap is reported through ResumeTime only, so the next tick must not count it again.
            _lastTickTime = Time.realtimeSinceStartup;
            var passed = (float)(DateTime.UtcNow - _pausedAtUtc).TotalSeconds;
            NotifyOwners(owner => owner.ResumeTime(Mathf.Max(0f, passed)));
        }

        // One failing owner must not stop the timer for the others.
        private void NotifyOwners(Action<ITimerOwner> notify)
        {
            _snapshot.Clear();
            _snapshot.AddRange(_timerOwners);
            foreach (var owner in _snapshot)
            {
                try
                {
                    notify(owner);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
