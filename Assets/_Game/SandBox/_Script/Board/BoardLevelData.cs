using System;
using System.Collections.Generic;
using ColorBlockJam.Core.Level;

namespace ColorBlockJam.SandBox.Board
{
    public sealed class BoardLevelDefinition
    {
        public int Cols { get; }
        public int Rows { get; }
        public double TimeLimitSeconds { get; }
        public IReadOnlyList<BlockData> Blocks { get; }
        public IReadOnlyList<GateData> Gates { get; }

        public BoardLevelDefinition(int cols, int rows, double timeLimitSeconds, IReadOnlyList<BlockData> blocks, IReadOnlyList<GateData> gates)
        {
            Cols = cols;
            Rows = rows;
            TimeLimitSeconds = timeLimitSeconds;
            Blocks = blocks ?? Array.Empty<BlockData>();
            Gates = gates ?? Array.Empty<GateData>();
        }
    }
}
