using System;
using System.Threading;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Utils;
using Cysharp.Threading.Tasks;

namespace ColorBlockJam.GameTask
{
    public class CallBackGameTask : IGameTask
    {
        private Action _action;
        private float _delay;
        public void Init(Action callBackAction,float delay = 0f)
        {
            _action = callBackAction;
            _delay = delay;
        }
        public async UniTask RunTask(CancellationToken cancellationToken)
        {
            if (_delay > 0f)
            {
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_delay), cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException e)
                {
                    DebugExtension.Log($"{nameof(CallBackGameTask)} cancelled: {e.Message}");
                }
                finally
                {
                    _action?.Invoke();
                }
                return;
            }
            _action?.Invoke();
        }

        public void Dispose()
        {
            
        }
    }
}