using System.Threading;
using ColorBlockJam.Core.Controller;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Views;
using Cysharp.Threading.Tasks;
using VContainer;

namespace ColorBlockJam.GameTask
{
    public class UnlockInputGameTask : IGameTask
    {
        private InputController _inputController;
        private ScreenController _screenController;
        [Inject]
        public void Construct(InputController inputController
        , ScreenController screenController)
        {
            _inputController = inputController;
            _screenController = screenController;
        }
        public UniTask RunTask(CancellationToken cancellationToken)
        {
            _inputController.EnableInput();
            _screenController.MainScreenView.SwitchRaycaster(true);
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            
        }
    }
}