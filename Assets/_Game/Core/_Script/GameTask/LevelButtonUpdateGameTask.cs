using System.Threading;
using ColorBlockJam.Core.Controller;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using ColorBlockJam.Interfaces;
using Cysharp.Threading.Tasks;
using VContainer;

namespace ColorBlockJam.GameTask
{
    // After a win: the level path above the main screen's Play button slides on to the new level, then the button plays
    // its level-up and shows the level now up next.
    public class LevelButtonUpdateGameTask : IGameTask
    {
        private ScreenController _screenController;
        private PlayerController _playerController;
        private LevelSettings _levelSettings;
        private IAudioService _audioService;

        [Inject]
        public void Construct(ScreenController screenController, PlayerController playerController,
            LevelSettings levelSettings, IAudioService audioService)
        {
            _audioService = audioService;
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

            var level = _playerController.GetVisualLevel();
            var path = _screenController.MainScreenView.LevelPath;
            if (path != null) await path.PlayAdvance(level, _levelSettings, cancellationToken);
            _audioService.PlaySfx(SfxName.LevelUp);
            await button.PlayLevelUp(level, _levelSettings, cancellationToken);
        }

        public void Dispose()
        {
        }
    }
}
