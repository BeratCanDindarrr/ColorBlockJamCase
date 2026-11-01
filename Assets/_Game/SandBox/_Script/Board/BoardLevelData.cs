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
        // Removed board cells: obstacles nothing stands on or slides through.
        public IReadOnlyCollection<CellCoord> Holes { get; }

        public BoardLevelDefinition(int cols, int rows, double timeLimitSeconds, IReadOnlyList<BlockData> blocks,
            IReadOnlyList<GateData> gates, IReadOnlyCollection<CellCoord> holes = null)
        {
            Cols = cols;
            Rows = rows;
            TimeLimitSeconds = timeLimitSeconds;
            Blocks = blocks ?? Array.Empty<BlockData>();
            Gates = gates ?? Array.Empty<GateData>();
            Holes = holes != null ? new HashSet<CellCoord>(holes) : new HashSet<CellCoord>();
        }
    }
}
