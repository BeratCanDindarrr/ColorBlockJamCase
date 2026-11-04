using System.Collections.Generic;

namespace ColorBlockJam.Core.Level
{
    public readonly struct LevelIssue
    {
        public readonly string Message;
        public readonly bool IsError;
        // The block or gate the issue is about, or -1.
        public readonly int BlockIndex;
        public readonly int GateIndex;

        public LevelIssue(string message, bool isError, int blockIndex = -1, int gateIndex = -1)
        {
            Message = message;
            IsError = isError;
            BlockIndex = blockIndex;
            GateIndex = gateIndex;
        }
    }

    // Checks a level layout before it is saved: errors make it unplayable, warnings make it unwinnable or odd.
    public static class LevelValidator
    {
        public static int EdgeLength(BoardEdge edge, int cols, int rows) => GateGeometry.LineLength(edge, cols, rows);

        public static List<LevelIssue> Validate(int cols, int rows, double timeLimitSec, BlockData[] blocks,
            GateData[] gates, CellCoord[] holes = null)
        {
            var issues = new List<LevelIssue>();
            if (cols <= 0 || rows <= 0) issues.Add(new LevelIssue($"Board size {cols}x{rows} must be positive", true));
            if (timeLimitSec <= 0) issues.Add(new LevelIssue("Time limit must be above 0", true));

            blocks ??= new BlockData[0];
            gates ??= new GateData[0];
            var holeSet = new HashSet<CellCoord>(holes ?? new CellCoord[0]);
            if (blocks.Length == 0) issues.Add(new LevelIssue("The level has no blocks", true));

            foreach (var hole in holeSet)
            {
                if (!IsInside(hole, cols, rows)) issues.Add(new LevelIssue($"Removed cell {hole} is off the board", true));
            }

            ValidateBlocks(cols, rows, blocks, holeSet, issues);
            ValidateGates(cols, rows, gates, holeSet, issues);

            // An iced gate opens after that many blocks have left; at least one has to leave through another gate first.
            for (var i = 0; i < gates.Length; i++)
            {
                var gate = gates[i];
                if (gate == null) continue;
                if (gate.Ice < 0) issues.Add(new LevelIssue($"Gate #{i} has negative ice", true, gateIndex: i));
                else if (gate.Ice > blocks.Length - 1)
                    issues.Add(new LevelIssue($"Gate #{i} needs {gate.Ice} exits to thaw but only {blocks.Length - 1} other blocks exist", true, gateIndex: i));
            }

            for (var i = 0; i < blocks.Length; i++)
            {
                var block = blocks[i];
                if (block == null) continue;

                // A block with no gate it can leave through makes the level impossible to finish: an error, so Save is blocked.
                if (!HasGateOfColor(gates, block.Color, BlockAxis.Free))
                    issues.Add(new LevelIssue($"Block {block.Id} ({block.Color}) has no gate of its colour", true, i));
                else if (!HasGateOfColor(gates, block.Color, block.Axis))
                    issues.Add(new LevelIssue($"Block {block.Id} moves only {block.Axis} but has no {block.Color} gate that way", true, i));
                else if (!HasGateItFits(gates, block))
                    issues.Add(new LevelIssue(TooNarrowMessage(block), true, i));

                // Every other block leaving first is the most exits that can happen before it thaws.
                if (block.Ice < 0) issues.Add(new LevelIssue($"Block {block.Id} has negative ice", true, i));
                else if (block.Ice > blocks.Length - 1)
                    issues.Add(new LevelIssue($"Block {block.Id} needs {block.Ice} exits to thaw but only {blocks.Length - 1} other blocks exist", true, i));
            }

            return issues;
        }

        private static bool IsInside(CellCoord cell, int cols, int rows) =>
            cell.Col >= 0 && cell.Row >= 0 && cell.Col < cols && cell.Row < rows;

        private static void ValidateBlocks(int cols, int rows, BlockData[] blocks, HashSet<CellCoord> holes,
            List<LevelIssue> issues)
        {
            var ids = new HashSet<string>();
            var owner = new Dictionary<CellCoord, int>();
            for (var i = 0; i < blocks.Length; i++)
            {
                var block = blocks[i];
                if (block == null)
                {
                    issues.Add(new LevelIssue($"Block #{i} is empty", true, i));
                    continue;
                }

                var name = string.IsNullOrEmpty(block.Id) ? $"#{i}" : block.Id;
                if (string.IsNullOrEmpty(block.Id)) issues.Add(new LevelIssue($"Block {name} has no id", true, i));
                else if (!ids.Add(block.Id)) issues.Add(new LevelIssue($"Block id {block.Id} is used twice", true, i));

                var cells = AbsoluteCells(block);
                if (cells.Count == 0)
                {
                    issues.Add(new LevelIssue($"Block {name} has no cells", true, i));
                    continue;
                }

                if (new HashSet<CellCoord>(cells).Count != cells.Count)
                    issues.Add(new LevelIssue($"Block {name} lists a cell twice", true, i));
                if (!IsConnected(cells)) issues.Add(new LevelIssue($"Block {name} is not in one piece", true, i));

                foreach (var cell in cells)
                {
                    if (cell.Col < 0 || cell.Row < 0 || cell.Col >= cols || cell.Row >= rows)
                    {
                        issues.Add(new LevelIssue($"Block {name} sticks out of the board at {cell}", true, i));
                        break;
                    }

                    if (holes.Contains(cell))
                    {
                        issues.Add(new LevelIssue($"Block {name} stands on removed cell {cell}", true, i));
                        break;
                    }

                    if (owner.TryGetValue(cell, out var other) && other != i)
                    {
                        issues.Add(new LevelIssue($"Block {name} overlaps block {blocks[other].Id} at {cell}", true, i));
                        break;
                    }

                    owner[cell] = i;
                }
            }
        }

        private static void ValidateGates(int cols, int rows, GateData[] gates, HashSet<CellCoord> holes,
            List<LevelIssue> issues)
        {
            for (var i = 0; i < gates.Length; i++)
            {
                var gate = gates[i];
                if (gate == null)
                {
                    issues.Add(new LevelIssue($"Gate #{i} is empty", true, gateIndex: i));
                    continue;
                }

                var length = EdgeLength(gate.Edge, cols, rows);
                var depthOk = gate.Depth >= 0 && gate.Depth < GateGeometry.MaxDepth(gate.Edge, cols, rows);
                if (gate.Length < 1 || gate.Index < 0 || gate.Index + gate.Length > length || !depthOk)
                {
                    issues.Add(new LevelIssue(
                        $"Gate #{i} ({gate.Edge} {gate.Index}, length {gate.Length}, depth {gate.Depth}) does not fit the board",
                        true, gateIndex: i));
                    continue;
                }

                // Every cell of the gate must stand on a wall: a board cell whose far side is off the board or removed.
                for (var k = gate.Index; k < gate.Index + gate.Length; k++)
                {
                    var attached = GateGeometry.Attached(gate.Edge, gate.Depth, k, cols, rows);
                    if (GateGeometry.IsWallSide(attached, gate.Edge, cols, rows, holes)) continue;
                    var reason = holes.Contains(attached) ? $"stands on removed cell {attached}" : $"has board on both sides at {attached}";
                    issues.Add(new LevelIssue($"Gate #{i} {reason}", true, gateIndex: i));
                    break;
                }

                for (var j = 0; j < i; j++)
                {
                    var other = gates[j];
                    if (other == null || other.Edge != gate.Edge || other.Depth != gate.Depth) continue;
                    if (gate.Index < other.Index + other.Length && other.Index < gate.Index + gate.Length)
                        issues.Add(new LevelIssue($"Gate #{i} overlaps gate #{j} on the {gate.Edge} wall", true, gateIndex: i));
                }
            }
        }

        public static List<CellCoord> AbsoluteCells(BlockData block)
        {
            var cells = new List<CellCoord>();
            if (block?.Cells == null) return cells;
            var origin = new CellCoord(block.Col, block.Row);
            foreach (var offset in block.Cells) cells.Add(origin + offset);
            return cells;
        }

        // Cells touching side by side (not diagonally) form one piece.
        private static bool IsConnected(List<CellCoord> cells)
        {
            var all = new HashSet<CellCoord>(cells);
            var seen = new HashSet<CellCoord> { cells[0] };
            var open = new Stack<CellCoord>();
            open.Push(cells[0]);
            while (open.Count > 0)
            {
                var cell = open.Pop();
                foreach (var next in Neighbours(cell))
                {
                    if (all.Contains(next) && seen.Add(next)) open.Push(next);
                }
            }

            return seen.Count == all.Count;
        }

        private static IEnumerable<CellCoord> Neighbours(CellCoord cell)
        {
            yield return new CellCoord(cell.Col + 1, cell.Row);
            yield return new CellCoord(cell.Col - 1, cell.Row);
            yield return new CellCoord(cell.Col, cell.Row + 1);
            yield return new CellCoord(cell.Col, cell.Row - 1);
        }

        // A block locked to an axis can only leave through gates on the walls that axis runs into.
        private static bool HasGateOfColor(GateData[] gates, BlockColor color, BlockAxis axis)
        {
            foreach (var gate in gates)
            {
                if (gate != null && gate.Color == color && RunsInto(axis, gate.Edge)) return true;
            }

            return false;
        }

        // A block leaves through a gate only with every cell inside the gate's span along its wall (GateFit.Fits), so the
        // gate must be at least as long as the block is wide (top/bottom walls) or tall (left/right walls).
        private static bool HasGateItFits(GateData[] gates, BlockData block)
        {
            foreach (var gate in gates)
            {
                if (gate == null || gate.Color != block.Color || !RunsInto(block.Axis, gate.Edge)) continue;
                if (gate.Length >= SpanAlong(block, gate.Edge)) return true;
            }

            return false;
        }

        private static string TooNarrowMessage(BlockData block) =>
            $"Block {block.Id} ({block.Color}) is {SpanAlong(block, BoardEdge.Top)} wide and {SpanAlong(block, BoardEdge.Left)} tall: " +
            $"no {block.Color} gate is long enough (top/bottom need {SpanAlong(block, BoardEdge.Top)}, left/right need {SpanAlong(block, BoardEdge.Left)})";

        // How many cells the block covers along the given wall: its width on the top and bottom walls, its height on the sides.
        private static int SpanAlong(BlockData block, BoardEdge edge)
        {
            var cells = block.Cells;
            if (cells == null || cells.Length == 0) return 0;
            var horizontal = edge == BoardEdge.Top || edge == BoardEdge.Bottom;
            var min = int.MaxValue;
            var max = int.MinValue;
            foreach (var cell in cells)
            {
                var along = horizontal ? cell.Col : cell.Row;
                if (along < min) min = along;
                if (along > max) max = along;
            }

            return max - min + 1;
        }

        private static bool RunsInto(BlockAxis axis, BoardEdge edge)
        {
            switch (axis)
            {
                case BlockAxis.Horizontal: return edge == BoardEdge.Left || edge == BoardEdge.Right;
                case BlockAxis.Vertical: return edge == BoardEdge.Top || edge == BoardEdge.Bottom;
                default: return true;
            }
        }
    }
}
