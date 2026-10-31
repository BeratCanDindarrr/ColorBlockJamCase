using System;
using ColorBlockJam.Core.Settings;

namespace ColorBlockJam.SandBox
{
    // Loads the level to play onto the board and unloads it when the gameplay scope goes away.
    public class LevelController : IDisposable
    {
        private readonly LevelSettings _levelSettings;
        private readonly BoardController _boardController;

        public LevelData LevelData { get; private set; }

        public LevelController(LevelSettings levelSettings, BoardController boardController)
        {
            _levelSettings = levelSettings;
            _boardController = boardController;
        }

        public void LoadLevel(int levelIndex)
        {
            LevelData = _levelSettings.GetLevel(levelIndex);
            _boardController.CreateLevel(LevelData);
        }

        public void UnloadLevel()
        {
            _boardController.UnLoad();
            LevelData = null;
        }

        public void Dispose() => UnloadLevel();
    }
}
