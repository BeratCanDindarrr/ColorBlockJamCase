using System.Collections.Generic;

namespace ColorBlockJam.Core.Level
{
    // Where a gate is. A gate stands on the Edge side of a line of board cells ("attached" cells) that is Depth cells in
    // from that outer edge; the cell just past each attached cell (Normal of Edge) is off the board or a removed cell,
    // and is where blocks leave through the gate. Shared by the rules, the views and the level editor.
    public static class GateGeometry
    {
        // The direction out through a wall, as (col, row); rows grow downward.
        public static CellCoord Normal(BoardEdge edge)
        {
            switch (edge)
            {
                case BoardEdge.Top: return new CellCoord(0, -1);
                case BoardEdge.Bottom: return new CellCoord(0, 1);
                case BoardEdge.Left: return new CellCoord(-1, 0);
                default: return new CellCoord(1, 0);
            }
        }

        public static BoardEdge EdgeOfNormal(CellCoord normal)
        {
            if (normal.Row < 0) return BoardEdge.Top;
            if (normal.Row > 0) return BoardEdge.Bottom;
            return normal.Col < 0 ? BoardEdge.Left : BoardEdge.Right;
        }

        // How many cells a wall line runs: columns for Top/Bottom, rows for Left/Right.
        public static int LineLength(BoardEdge edge, int cols, int rows) =>
            edge == BoardEdge.Top || edge == BoardEdge.Bottom ? cols : rows;

        // How deep a gate on this edge can go: rows for Top/Bottom, columns for Left/Right.
        public static int MaxDepth(BoardEdge edge, int cols, int rows) =>
            edge == BoardEdge.Top || edge == BoardEdge.Bottom ? rows : cols;

        // The row (Top/Bottom) or column (Left/Right) of the cells a gate at this depth is attached to.
        public static int Line(BoardEdge edge, int depth, int cols, int rows)
        {
            switch (edge)
            {
                case BoardEdge.Top:
                case BoardEdge.Left: return depth;
                case BoardEdge.Bottom: return rows - 1 - depth;
                default: return cols - 1 - depth;
            }
        }

        public static CellCoord Attached(BoardEdge edge, int depth, int index, int cols, int rows)
        {
            var line = Line(edge, depth, cols, rows);
            return edge == BoardEdge.Top || edge == BoardEdge.Bottom ? new CellCoord(index, line) : new CellCoord(line, index);
        }

        // The cell past the wall: off the board for an outer gate, a removed cell for a deeper one.
        public static CellCoord Outside(BoardEdge edge, int depth, int index, int cols, int rows) =>
            Attached(edge, depth, index, cols, rows) + Normal(edge);

        // Position of a cell along a wall line: its column on Top/Bottom, its row on Left/Right.
        public static int Along(CellCoord cell, BoardEdge edge) =>
            edge == BoardEdge.Top || edge == BoardEdge.Bottom ? cell.Col : cell.Row;

        // The gate place a board cell has on its edge side: index along the wall and depth in from that edge.
        public static void PlaceOf(CellCoord attached, BoardEdge edge, int cols, int rows, out int index, out int depth)
        {
            index = Along(attached, edge);
            switch (edge)
            {
                case BoardEdge.Top: depth = attached.Row; break;
                case BoardEdge.Bottom: depth = rows - 1 - attached.Row; break;
                case BoardEdge.Left: depth = attached.Col; break;
                default: depth = cols - 1 - attached.Col; break;
            }
        }

        public static bool IsBoardCell(CellCoord cell, int cols, int rows, ICollection<CellCoord> holes) =>
            cell.Col >= 0 && cell.Row >= 0 && cell.Col < cols && cell.Row < rows && (holes == null || !holes.Contains(cell));

        // A wall stands on this side of a board cell (and a gate may go there): the cell past it is not a board cell.
        public static bool IsWallSide(CellCoord cell, BoardEdge edge, int cols, int rows, ICollection<CellCoord> holes) =>
            IsBoardCell(cell, cols, rows, holes) && !IsBoardCell(cell + Normal(edge), cols, rows, holes);
    }
}
