using ColorBlockJam.Core.Settings;
using ColorBlockJam.Interfaces;

namespace ColorBlockJam.Core.Service
{
    // shortcut: index lives in memory and always starts at 0; persist it once a save system exists.
    public class LevelService : ILevelService
    {
        private readonly LevelSettings _levelSettings;

        public int CurrentLevelIndex { get; private set; }

        public LevelService(LevelSettings levelSettings)
        {
            _levelSettings = levelSettings;
        }

        public LevelData GetCurrentLevel() => _levelSettings.GetLevel(CurrentLevelIndex);
    }
}
