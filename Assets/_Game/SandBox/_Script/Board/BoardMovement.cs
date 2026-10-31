using System;
using System.Collections.Generic;

namespace ColorBlockJam.SandBox.Board
{
    // Collision and sliding of a block among the other (placed) blocks on the board. Every cell is
    // a unit square. Exiting blocks are neither obstacles nor pickable.
    public sealed class BoardMovement
    {
        private const double Eps = 1e-6;

        private readonly IReadOnlyList<BlockState> _blocks;
        private readonly int _cols;
        private readonly int _rows;
        private readonly double _assistRange;

        public BoardMovement(IReadOnlyList<BlockState> blocks, int cols, int rows, double assistRange)
        {
            _blocks = blocks;
            _cols = cols;
            _rows = rows;
            _assistRange = assistRange;
        }

        public BlockState BlockAt(double col, double row)
        {
            for (var i = 0; i < _blocks.Count; i++)
            {
                var block = _blocks[i];
                if (block.IsExiting)
                    continue;

                for (var k = 0; k < block.Cells.Count; k++)
                {
                    var cellCol = block.Origin.Col + block.Cells[k].Col;
                    var cellRow = block.Origin.Row + block.Cells[k].Row;
                    if (col >= cellCol && col < cellCol + 1 && row >= cellRow && row < cellRow + 1)
                        return block;
                }
            }

            return null;
        }

        public bool IsPlacementFree(BlockState moving, BoardPoint origin)
        {
            for (var k = 0; k < moving.Cells.Count; k++)
            {
                var col = origin.Col + moving.Cells[k].Col;
                var row = origin.Row + moving.Cells[k].Row;
                if (col < -Eps || row < -Eps || col + 1 > _cols + Eps || row + 1 > _rows + Eps)
                    return false;
                if (OverlapsObstacle(moving, col, row))
                    return false;
            }

            return true;
        }

        // Clamps the wanted delta by the most constrained cell, measured from the current position,
        // so a fast drag cannot tunnel through an obstacle.
        public double ResolveAxis(BlockState moving, BoardPoint origin, bool alongRow, double delta)
        {
            if (double.IsNaN(delta) || double.IsInfinity(delta))
                return 0;

            var back = double.NegativeInfinity;
            var forward = double.PositiveInfinity;
            var limit = alongRow ? _rows : _cols;
            for (var k = 0; k < moving.Cells.Count; k++)
            {
                var col = origin.Col + moving.Cells[k].Col;
                var row = origin.Row + moving.Cells[k].Row;
                var pos = alongRow ? row : col;
                var cross = alongRow ? col : row;
                AxisBounds(moving, pos, cross, alongRow, limit, out var min, out var max);
                back = Math.Max(back, min - pos);
                forward = Math.Min(forward, max - pos);
            }

            return Math.Min(Math.Max(delta, back), forward);
        }

        // The axis the finger moved further on is resolved first, then the cross axis.
        public BoardPoint SlideBlock(BlockState moving, BoardPoint origin, BoardPoint target)
        {
            var wantedCol = target.Col - origin.Col;
            var wantedRow = target.Row - origin.Row;
            var alongRow = Math.Abs(wantedRow) > Math.Abs(wantedCol);
            var wanted = alongRow ? wantedRow : wantedCol;

            var start = AlignForMove(moving, origin, alongRow, wanted);
            var moved = ResolveAxis(moving, start, alongRow, wanted);
            var afterPrimary = alongRow
                ? new BoardPoint(start.Col, start.Row + moved)
                : new BoardPoint(start.Col + moved, start.Row);

            var crossDelta = alongRow ? target.Col - afterPrimary.Col : target.Row - afterPrimary.Row;
            var drift = ResolveAxis(moving, afterPrimary, !alongRow, crossDelta);
            var slid = alongRow
                ? new BoardPoint(afterPrimary.Col + drift, afterPrimary.Row)
                : new BoardPoint(afterPrimary.Col, afterPrimary.Row + drift);

            // In a corner the lane assists of the two axes undo each other frame after frame; a slide
            // that brings the block no closer to the finger is dropped so a held finger keeps it still.
            return DistanceSquared(slid, target) < DistanceSquared(origin, target) - Eps ? slid : origin;
        }

        public CellCoord Snap(BlockState moving, BoardPoint origin, CellCoord fallback)
        {
            var snapped = new CellCoord(RoundHalfUp(origin.Col), RoundHalfUp(origin.Row));
            return IsPlacementFree(moving, new BoardPoint(snapped.Col, snapped.Row)) ? snapped : fallback;
        }

        private static double DistanceSquared(BoardPoint a, BoardPoint b)
        {
            var col = a.Col - b.Col;
            var row = a.Row - b.Row;
            return col * col + row * row;
        }

        // If the primary move is blocked and the block is nearly lane-aligned on the cross axis,
        // pull it into the lane when that lane is free and gives more reach.
        private BoardPoint AlignForMove(BlockState moving, BoardPoint origin, bool alongRow, double wanted)
        {
            var reachHere = Math.Abs(ResolveAxis(moving, origin, alongRow, wanted));
            var blocked = reachHere < Math.Abs(wanted) - Eps;
            var cross = alongRow ? origin.Col : origin.Row;
            double snapped = RoundHalfUp(cross);
            var offset = Math.Abs(snapped - cross);
            if (!blocked || offset < Eps || offset > _assistRange)
                return origin;

            var aligned = alongRow ? new BoardPoint(snapped, origin.Row) : new BoardPoint(origin.Col, snapped);
            if (!IsPlacementFree(moving, aligned))
                return origin;

            var reachAligned = Math.Abs(ResolveAxis(moving, aligned, alongRow, wanted));
            return reachAligned > reachHere + Eps ? aligned : origin;
        }

        private void AxisBounds(BlockState moving, double pos, double cross, bool alongRow, int limit,
            out double min, out double max)
        {
            min = 0;
            max = limit - 1;
            for (var i = 0; i < _blocks.Count; i++)
            {
                var other = _blocks[i];
                if (ReferenceEquals(other, moving) || other.IsExiting)
                    continue;

                for (var k = 0; k < other.Cells.Count; k++)
                    Narrow(pos, cross, alongRow, other.Origin.Col + other.Cells[k].Col,
                        other.Origin.Row + other.Cells[k].Row, ref min, ref max);
            }
        }

        // A cell in the lane of the moving cell stops it from one side or the other.
        private static void Narrow(double pos, double cross, bool alongRow, int otherCol, int otherRow,
            ref double min, ref double max)
        {
            var otherPos = alongRow ? otherRow : otherCol;
            var otherCross = alongRow ? otherCol : otherRow;
            if (!SpansOverlap(cross, otherCross))
                return;
            if (otherPos + 1 <= pos + Eps)
                min = Math.Max(min, otherPos + 1);
            if (otherPos >= pos + 1 - Eps)
                max = Math.Min(max, otherPos - 1);
        }

        private bool OverlapsObstacle(BlockState moving, double col, double row)
        {
            for (var i = 0; i < _blocks.Count; i++)
            {
                var other = _blocks[i];
                if (ReferenceEquals(other, moving) || other.IsExiting)
                    continue;

                for (var k = 0; k < other.Cells.Count; k++)
                    if (SpansOverlap(col, other.Origin.Col + other.Cells[k].Col) &&
                        SpansOverlap(row, other.Origin.Row + other.Cells[k].Row))
                        return true;
            }

            return false;
        }

        private static bool SpansOverlap(double a, double b) => a < b + 1 - Eps && b < a + 1 - Eps;

        private static int RoundHalfUp(double value) => (int)Math.Floor(value + 0.5);
    }
}
