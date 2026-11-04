using System.Threading;
using ColorBlockJam.Core.Controller;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using ColorBlockJam.Interfaces;
using Cysharp.Threading.Tasks;
using VContainer;

namespace ColorBlockJam.GameTask
{
    // After a win: the main screen's level button plays its level-up and shows the level now up next.
    public class LevelButtonUpdateGameTask : IGameTask
    {
        private ScreenController _screenController;
        private PlayerController _playerController;
        private LevelSettings _levelSettings;

        [Inject]
        public void Construct(ScreenController screenController, PlayerController playerController,
            LevelSettings levelSettings)
        {
            _screenController = screenController;
            _playerController = playerController;
            _levelSettings = levelSettings;
        }

        public async UniTask RunTask(CancellationToken cancellationToken)
        {
            await UniTask.WaitUntil(() => _screenController.ScreenStatus == ScreenType.MainScreen,
                cancellationToken: cancellationToken);
            var button = _screenController.MainScreenView.LevelButton;
            if (button == null) return;

            await button.PlayLevelUp(_playerController.GetVisualLevel(), _levelSettings, cancellationToken);
        }

        public void Dispose()
        {
        }
    }
}
