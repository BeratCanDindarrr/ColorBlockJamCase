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
        public static int EdgeLength(BoardEdge edge, int cols, int rows) =>
            edge == BoardEdge.Top || edge == BoardEdge.Bottom ? cols : rows;

        public static List<LevelIssue> Validate(int cols, int rows, double timeLimitSec, BlockData[] blocks,
            GateData[] gates)
        {
            var issues = new List<LevelIssue>();
            if (cols <= 0 || rows <= 0) issues.Add(new LevelIssue($"Board size {cols}x{rows} must be positive", true));
            if (timeLimitSec <= 0) issues.Add(new LevelIssue("Time limit must be above 0", true));

            blocks ??= new BlockData[0];
            gates ??= new GateData[0];
            if (blocks.Length == 0) issues.Add(new LevelIssue("The level has no blocks", true));

            ValidateBlocks(cols, rows, blocks, issues);
            ValidateGates(cols, rows, gates, issues);

            for (var i = 0; i < blocks.Length; i++)
            {
                if (blocks[i] != null && !HasGateOfColor(gates, blocks[i].Color))
                    issues.Add(new LevelIssue($"Block {blocks[i].Id} ({blocks[i].Color}) has no gate of its colour", false, i));
            }

            return issues;
        }

        private static void ValidateBlocks(int cols, int rows, BlockData[] blocks, List<LevelIssue> issues)
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

                    if (owner.TryGetValue(cell, out var other) && other != i)
                    {
                        issues.Add(new LevelIssue($"Block {name} overlaps block {blocks[other].Id} at {cell}", true, i));
                        break;
                    }

                    owner[cell] = i;
                }
            }
        }

        private static void ValidateGates(int cols, int rows, GateData[] gates, List<LevelIssue> issues)
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
                if (gate.Length < 1 || gate.Index < 0 || gate.Index + gate.Length > length)
                {
                    issues.Add(new LevelIssue(
                        $"Gate #{i} ({gate.Edge} {gate.Index}, length {gate.Length}) does not fit the {gate.Edge} wall",
                        true, gateIndex: i));
                    continue;
                }

                for (var j = 0; j < i; j++)
                {
                    var other = gates[j];
                    if (other == null || other.Edge != gate.Edge) continue;
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

        private static bool HasGateOfColor(GateData[] gates, BlockColor color)
        {
            foreach (var gate in gates)
            {
                if (gate != null && gate.Color == color) return true;
            }

            return false;
        }
    }
}
