using ColorBlockJam.Core.Settings;

namespace ColorBlockJam.Interfaces
{
    public interface ILevelService
    {
        int CurrentLevelIndex { get; }
        LevelData GetCurrentLevel();
    }
}
