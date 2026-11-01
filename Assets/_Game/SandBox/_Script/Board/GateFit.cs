using ColorBlockJam.Core.Level;
using System.Collections.Generic;

namespace ColorBlockJam.SandBox.Board
{
    // Whether a block standing at a given origin can go into a wall gate. Row 0 is the top of the board.
    // Gates have no capacity: any same-colour block that fits may use one any number of times.
    public static class GateFit
    {
        // The direction out of the board through the given wall, as (col, row).
        public static CellCoord Normal(BoardEdge edge) => GateGeometry.Normal(edge);

        // True when a cell is on the line of cells right inside the wall (depth cells in from the outer edge).
        public static bool IsOnWall(CellCoord cell, BoardEdge edge, int depth, int cols, int rows)
        {
            var line = GateGeometry.Line(edge, depth, cols, rows);
            return edge == BoardEdge.Top || edge == BoardEdge.Bottom ? cell.Row == line : cell.Col == line;
        }

        // The cell's position along the wall: its column on the top and bottom walls, its row on the sides.
        public static int AlongWall(CellCoord cell, BoardEdge edge) => GateGeometry.Along(cell, edge);

        public static bool TouchesWall(IReadOnlyList<CellCoord> cells, CellCoord origin, BoardEdge edge, int depth,
            int cols, int rows)
        {
            for (var i = 0; i < cells.Count; i++)
                if (IsOnWall(Absolute(cells[i], origin), edge, depth, cols, rows))
                    return true;
            return false;
        }

        // The block touches the gate's wall, lies wholly within the gate's span along it, and has the gate's color.
        public static bool Fits(BlockState block, CellCoord origin, GateState gate, int cols, int rows)
        {
            if (block == null || gate == null)
                return false;
            if (block.Color != gate.Color || block.Cells.Count == 0)
                return false;
            if (!TouchesWall(block.Cells, origin, gate.Edge, gate.Depth, cols, rows))
                return false;

            for (var i = 0; i < block.Cells.Count; i++)
            {
                var along = AlongWall(Absolute(block.Cells[i], origin), gate.Edge);
                if (along < gate.Index || along >= gate.Index + gate.Length)
                    return false;
            }

            return true;
        }

        // How far a move from "from" to "target" pushes into the given wall; positive is into the wall,
        // negative is away from it.
        public static double Push(BoardPoint from, BoardPoint target, BoardEdge edge)
        {
            var delta = target - from;
            var normal = Normal(edge);
            return delta.Col * normal.Col + delta.Row * normal.Row;
        }

        private static CellCoord Absolute(CellCoord cell, CellCoord origin) =>
            new CellCoord(origin.Col + cell.Col, origin.Row + cell.Row);
    }
}
