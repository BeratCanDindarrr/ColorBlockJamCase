using System;
using System.Collections.Generic;

namespace ColorBlockJam.SandBox.Board
{
    [Serializable]
    public class BlockData
    {
        public string Id;
        public int Col;
        public int Row;
        public CellCoord[] Cells;
        public BlockColor Color;
    }

    // A gate on one wall of the board: every cell from Index to Index + Length - 1 along that
    // edge accepts a block of its Color.
    [Serializable]
    public class GateData
    {
        public BoardEdge Edge;
        public int Index;
        public int Length;
        public BlockColor Color;
    }

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
