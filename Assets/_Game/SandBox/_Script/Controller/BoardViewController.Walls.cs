using System.Collections.Generic;
using ColorBlockJam.Core.Level;
using ColorBlockJam.SandBox.Board;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // Walls from the art's pieces, joined: every straight run of wall sides along one wall line is one wall piece
    // stretched to the run's length (rounded ends at the run's ends only), and every corner where two walls meet gets
    // the corner piece, as before.
    // With FillHolesSolid, removed cells are filled by stretched wall pieces a whole cell thick instead, each laid along
    // its longer straight run (horizontal or vertical) and reaching into the outer wall where it touches it; sides
    // facing them get no wall. Removed cells a gate stands in front of keep the plain walls, so the gate stays in view.
    public partial class BoardViewController
    {
        // The art's straight piece is half a cell long and half a cell thick.
        private const float PiecesPerCell = 2f;
        private const double CornerReach = 0.75;
        private const float CornerYawTopRight = 0f;
        private const float CornerYawBottomRight = 90f;
        private const float CornerYawBottomLeft = 180f;
        private const float CornerYawTopLeft = 270f;
        private static readonly BoardEdge[] Edges = { BoardEdge.Top, BoardEdge.Bottom, BoardEdge.Left, BoardEdge.Right };
        private static readonly Quaternion AlongColumns = Quaternion.Euler(0f, 90f, 0f);

        private readonly Dictionary<(BoardEdge, int), SortedDictionary<int, CellCoord>> _wallLines =
            new Dictionary<(BoardEdge, int), SortedDictionary<int, CellCoord>>();

        private HashSet<CellCoord> _solidHoles;
        private IReadOnlyList<GateState> _wallGates;
        private ICollection<CellCoord> _holes;

        private void BuildWalls(IReadOnlyList<GateState> gates, IReadOnlyCollection<CellCoord> holes)
        {
            _wallGates = gates;
            _holes = holes as ICollection<CellCoord> ?? new HashSet<CellCoord>(holes);
            _solidHoles = _boardSettings.FillHolesSolid ? SolidHoles(gates, _holes) : new HashSet<CellCoord>();

            CollectWallSides();
            foreach (var line in _wallLines)
                AddRuns(line.Key.Item1, line.Key.Item2, line.Value);
            FillHoles();
        }

        // Removed cells with no gate in front of them.
        private HashSet<CellCoord> SolidHoles(IReadOnlyList<GateState> gates, ICollection<CellCoord> holes)
        {
            var solid = new HashSet<CellCoord>(holes);
            foreach (var gate in gates)
            {
                for (var i = 0; i < gate.Length; i++)
                    solid.Remove(GateGeometry.Outside(gate.Edge, gate.Depth, gate.Index + i, Layout.Cols, Layout.Rows));
            }

            return solid;
        }

        // Wall sides grouped by wall line (edge + depth) and ordered along it; corners are added on the way.
        private void CollectWallSides()
        {
            _wallLines.Clear();
            for (var row = 0; row < Layout.Rows; row++)
            {
                for (var col = 0; col < Layout.Cols; col++)
                {
                    var cell = new CellCoord(col, row);
                    if (_holes.Contains(cell)) continue;

                    foreach (var edge in Edges)
                    {
                        if (!IsWallSide(cell, edge)) continue;
                        GateGeometry.PlaceOf(cell, edge, Layout.Cols, Layout.Rows, out var index, out var depth);
                        if (IsGateCell(edge, index, depth)) continue;

                        if (!_wallLines.TryGetValue((edge, depth), out var line))
                            _wallLines[(edge, depth)] = line = new SortedDictionary<int, CellCoord>();
                        line[index] = cell;
                    }

                    AddCornerIfWalls(cell, BoardEdge.Top, BoardEdge.Right, CornerYawTopRight);
                    AddCornerIfWalls(cell, BoardEdge.Bottom, BoardEdge.Right, CornerYawBottomRight);
                    AddCornerIfWalls(cell, BoardEdge.Bottom, BoardEdge.Left, CornerYawBottomLeft);
                    AddCornerIfWalls(cell, BoardEdge.Top, BoardEdge.Left, CornerYawTopLeft);
                }
            }
        }

        // A side facing off the board or a removed cell that is not filled.
        private bool IsWallSide(CellCoord cell, BoardEdge edge) =>
            GateGeometry.IsWallSide(cell, edge, Layout.Cols, Layout.Rows, _holes) &&
            !_solidHoles.Contains(cell + GateGeometry.Normal(edge));

        private void AddRuns(BoardEdge edge, int depth, SortedDictionary<int, CellCoord> line)
        {
            var started = false;
            int first = 0, last = 0;
            foreach (var index in line.Keys)
            {
                if (started && index == last + 1)
                {
                    last = index;
                    continue;
                }

                if (started) AddRun(edge, depth, first, last);
                started = true;
                first = last = index;
            }

            if (started) AddRun(edge, depth, first, last);
        }

        // From the first cell's outer edge to the last one's, centred between them.
        private void AddRun(BoardEdge edge, int depth, int first, int last)
        {
            var piece = Spawn(_wallPool, _wallPieces);
            Place(piece.transform, Layout.EdgeLocal(edge, (first + last) * HalfCell, depth), Layout.GateRotation(edge));
            piece.SetStraight((last - first + 1) * PiecesPerCell, 1f);
            piece.SetColor(_boardSettings.WallColor);
        }

        // The corner mesh rounds toward +x/+z at yaw 0; each yaw step turns it a quarter clockwise. It sits a quarter cell
        // past the cell's corner on both sides, where the two walls' centre lines meet.
        private void AddCornerIfWalls(CellCoord cell, BoardEdge vertical, BoardEdge horizontal, float yaw)
        {
            if (!IsWallSide(cell, vertical) || !IsWallSide(cell, horizontal)) return;

            GateGeometry.PlaceOf(cell, vertical, Layout.Cols, Layout.Rows, out var index, out var depth);
            var sideways = horizontal == BoardEdge.Right ? CornerReach : -CornerReach;
            var piece = Spawn(_wallPool, _wallPieces);
            Place(piece.transform, Layout.EdgeLocal(vertical, index + sideways, depth), Quaternion.Euler(0f, yaw, 0f));
            piece.SetCorner();
            piece.SetColor(_boardSettings.WallColor);
        }

        // Each filled cell joins the longer straight run of filled cells through it; a run touching the board's edge
        // reaches on into the outer wall.
        private void FillHoles()
        {
            var done = new HashSet<CellCoord>();
            foreach (var cell in _solidHoles)
            {
                if (done.Contains(cell)) continue;

                var left = Reach(cell, new CellCoord(-1, 0), done);
                var right = Reach(cell, new CellCoord(1, 0), done);
                var up = Reach(cell, new CellCoord(0, -1), done);
                var down = Reach(cell, new CellCoord(0, 1), done);
                var horizontal = left + right >= up + down;
                var step = horizontal ? new CellCoord(1, 0) : new CellCoord(0, 1);
                var from = horizontal ? new CellCoord(cell.Col - left, cell.Row) : new CellCoord(cell.Col, cell.Row - up);
                var to = horizontal ? new CellCoord(cell.Col + right, cell.Row) : new CellCoord(cell.Col, cell.Row + down);
                for (var c = from; ; c = c + step)
                {
                    done.Add(c);
                    if (c == to) break;
                }

                AddHoleFill(from, to, horizontal);
            }
        }

        private int Reach(CellCoord cell, CellCoord step, HashSet<CellCoord> done)
        {
            var count = 0;
            for (var next = cell + step; _solidHoles.Contains(next) && !done.Contains(next); next = next + step)
                count++;
            return count;
        }

        private void AddHoleFill(CellCoord from, CellCoord to, bool horizontal)
        {
            double start = (horizontal ? from.Col : from.Row) - HalfCell;
            double end = (horizontal ? to.Col : to.Row) + HalfCell;
            var size = horizontal ? Layout.Cols : Layout.Rows;
            if ((horizontal ? from.Col : from.Row) == 0) start -= HalfCell;
            if ((horizontal ? to.Col : to.Row) == size - 1) end += HalfCell;

            var middle = (start + end) * HalfCell;
            var centre = horizontal ? new BoardPoint(middle, from.Row) : new BoardPoint(from.Col, middle);
            var piece = Spawn(_wallPool, _wallPieces);
            Place(piece.transform, Layout.PointToLocal(centre), horizontal ? Quaternion.identity : AlongColumns);
            piece.SetStraight((float)(end - start) * PiecesPerCell, PiecesPerCell);
            piece.SetColor(_boardSettings.WallColor);
        }

        private bool IsGateCell(BoardEdge edge, int index, int depth)
        {
            foreach (var gate in _wallGates)
            {
                if (gate.Edge == edge && gate.Depth == depth && index >= gate.Index && index < gate.Index + gate.Length)
                    return true;
            }

            return false;
        }
    }
}
