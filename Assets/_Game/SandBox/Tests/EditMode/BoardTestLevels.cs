using ColorBlockJam.Core.Level;
using System.Collections.Generic;

namespace ColorBlockJam.SandBox.Board.Tests
{
    // Building blocks for test levels on a square board.
    internal static class BoardTestLevels
    {
        public const int GridSize = 6;

        public static CellCoord[] Cells(params int[] pairs)
        {
            var cells = new CellCoord[pairs.Length / 2];
            for (var i = 0; i < cells.Length; i++)
                cells[i] = new CellCoord(pairs[i * 2], pairs[i * 2 + 1]);
            return cells;
        }

        public static BlockData Block(string id, int col, int row, BlockColor color, CellCoord[] cells)
        {
            return new BlockData { Id = id, Col = col, Row = row, Cells = cells, Color = color };
        }

        public static GateData Gate(BoardEdge edge, int index, int length, BlockColor color, int depth = 0)
        {
            return new GateData { Edge = edge, Index = index, Length = length, Color = color, Depth = depth };
        }

        public static BoardLevelDefinition Create(int cols, int rows, double timeLimit, IReadOnlyList<BlockData> blocks, IReadOnlyList<GateData> gates)
        {
            return new BoardLevelDefinition(cols, rows, timeLimit, blocks, gates);
        }

        // Two single-cell blocks, each with a matching gate on the wall it starts nearest to.
        public static BoardLevelDefinition TwoBlocksTwoGates()
        {
            var blocks = new[]
            {
                Block("A", 0, 0, BlockColor.Red, Cells(0, 0)),
                Block("B", GridSize - 1, GridSize - 1, BlockColor.Blue, Cells(0, 0))
            };
            var gates = new[]
            {
                Gate(BoardEdge.Left, 0, 1, BlockColor.Red),
                Gate(BoardEdge.Right, GridSize - 1, 1, BlockColor.Blue)
            };
            return Create(GridSize, GridSize, 60, blocks, gates);
        }

        // One red block in the top-left corner, next to a red gate on the left wall.
        public static BoardLevelDefinition OneBlockOneGate(double timeLimit)
        {
            var blocks = new[] { Block("A", 0, 0, BlockColor.Red, Cells(0, 0)) };
            var gates = new[] { Gate(BoardEdge.Left, 0, 1, BlockColor.Red) };
            return Create(GridSize, GridSize, timeLimit, blocks, gates);
        }
    }
}
