using System;
using System.Threading;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Utils;
using Cysharp.Threading.Tasks;

namespace ColorBlockJam.GameTask
{
    public class WaitForSecondsGameTask : IGameTask
    {
        private float _delay;
        public void Init(float delay = 0f)
        {
            _delay = delay;
        }
        public async UniTask RunTask(CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_delay), cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException e)
            {
                DebugExtension.Log($"{nameof(WaitForSecondsGameTask)} cancelled: {e.Message}");
            }

        }

        public void Dispose()
        {
        }
    }
}