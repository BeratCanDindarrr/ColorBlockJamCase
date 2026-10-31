using ColorBlockJam.Core.Level;
using ColorBlockJam.SandBox.Board;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // Where the cells, walls and gates of a board sit, in the board's local space.
    // Columns grow along +x; rows grow toward the camera, along -z (row 0 is the top row).
    public sealed class BoardLayout
    {
        private const float Half = 0.5f;
        // The wall kit is half a cell thick, so the wall centre line sits a quarter cell outside the grid.
        private const float WallOffset = 0.25f;
        private const float YawForward = 0f;
        private const float YawRight = 90f;
        private const float YawBack = 180f;
        private const float YawLeft = 270f;

        public int Cols { get; }
        public int Rows { get; }
        public float CellSize { get; }

        // Half size of the board including its walls, along x and z.
        public Vector2 OuterHalfExtents => new Vector2(Cols * Half + 2f * WallOffset, Rows * Half + 2f * WallOffset) * CellSize;

        public BoardLayout(BoardSettings boardSettings, int cols, int rows)
        {
            CellSize = boardSettings.CellSize;
            Cols = cols;
            Rows = rows;
        }

        public Vector3 CellToLocal(CellCoord cell) => ToLocal(cell.Col, cell.Row);

        // Local position of the center of the cell at the given (possibly fractional) board coordinates.
        public Vector3 PointToLocal(BoardPoint point) => ToLocal(point.Col, point.Row);

        // Board coordinates of a local point on the board plane, corner-origin: the cell at (c, r) covers [c, c+1) x [r, r+1),
        // so a cell centre maps to (c + 0.5, r + 0.5). Not the inverse of PointToLocal, which takes cell-centre coordinates.
        public BoardPoint LocalToBoard(Vector3 local) =>
            new BoardPoint(local.x / CellSize + Cols * Half, -local.z / CellSize + Rows * Half);

        // A gate stands in the wall line, centred on the cells it covers.
        public Vector3 GateLocal(BoardEdge edge, int index, int length) => EdgeLocal(edge, index + (length - 1) * Half);

        // The gate's opening on the outside face of the wall.
        public Vector3 GateOuterLocal(BoardEdge edge, int index, int length) =>
            GateLocal(edge, index, length) + OutwardLocal(edge) * (WallOffset * CellSize);

        // Unit direction out of the board through an edge (GateFit.Normal in local space; rows grow along -z).
        public Vector3 OutwardLocal(BoardEdge edge)
        {
            var normal = GateFit.Normal(edge);
            return new Vector3(normal.Col, 0f, -normal.Row);
        }

        // A gate (and a wall piece) faces the board.
        public Quaternion GateRotation(BoardEdge edge)
        {
            switch (edge)
            {
                case BoardEdge.Top: return Quaternion.Euler(0f, YawBack, 0f);
                case BoardEdge.Bottom: return Quaternion.Euler(0f, YawForward, 0f);
                case BoardEdge.Left: return Quaternion.Euler(0f, YawRight, 0f);
                default: return Quaternion.Euler(0f, YawLeft, 0f);
            }
        }

        // A point on the wall centre line of an edge; `along` is a column (top/bottom) or row (left/right) coordinate.
        // Values past the grid (-0.75, Cols - 0.25) reach the outer corners.
        public Vector3 EdgeLocal(BoardEdge edge, double along)
        {
            switch (edge)
            {
                case BoardEdge.Top: return ToLocal(along, -Half - WallOffset);
                case BoardEdge.Bottom: return ToLocal(along, Rows - Half + WallOffset);
                case BoardEdge.Left: return ToLocal(-Half - WallOffset, along);
                default: return ToLocal(Cols - Half + WallOffset, along);
            }
        }

        public int EdgeLength(BoardEdge edge) => edge == BoardEdge.Top || edge == BoardEdge.Bottom ? Cols : Rows;

        private Vector3 ToLocal(double col, double row) =>
            new Vector3(((float)col + Half - Cols * Half) * CellSize, 0f, -((float)row + Half - Rows * Half) * CellSize);
    }
}
