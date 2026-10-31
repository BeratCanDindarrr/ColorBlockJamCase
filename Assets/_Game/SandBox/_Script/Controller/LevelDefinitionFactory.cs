using ColorBlockJam.Core.Settings;
using ColorBlockJam.SandBox.Board;

namespace ColorBlockJam.SandBox
{
    public static class LevelDefinitionFactory
    {
        public static BoardLevelDefinition Create(LevelData level) =>
            new BoardLevelDefinition(level.Cols, level.Rows, level.TimeLimitSec, level.Blocks, level.Gates);
    }
}
