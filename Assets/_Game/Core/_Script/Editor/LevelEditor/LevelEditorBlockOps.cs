using System.Collections.Generic;
using System.Linq;
using ColorBlockJam.Core.Level;
using ColorBlockJam.Core.Settings;

namespace ColorBlockJam.LevelEditor
{
    // Block edits on a level. A block is stored as an origin plus cell offsets; the editor keeps the origin at the
    // block's top-left corner so the offsets never go negative.
    internal static class LevelEditorBlockOps
    {
        private static readonly CellCoord[] Sides =
        {
            new CellCoord(1, 0), new CellCoord(-1, 0), new CellCoord(0, 1), new CellCoord(0, -1)
        };

        public static int BlockAt(LevelData level, CellCoord cell)
        {
            var blocks = level.Blocks ?? new BlockData[0];
            for (var i = 0; i < blocks.Length; i++)
            {
                if (LevelValidator.AbsoluteCells(blocks[i]).Contains(cell)) return i;
            }

            return -1;
        }

        // Starts a new one-cell block on a free cell. Returns its index.
        public static int AddBlock(LevelData level, CellCoord cell, BlockColor color) =>
            AddBlock(level, new List<CellCoord> { cell }, color);

        // Adds a block over the given board cells (a shape dropped from the library). Returns its index.
        public static int AddBlock(LevelData level, List<CellCoord> cells, BlockColor color)
        {
            var blocks = (level.Blocks ?? new BlockData[0]).ToList();
            var block = new BlockData { Id = NextId(blocks), Color = color };
            SetCells(block, cells);
            blocks.Add(block);
            level.Blocks = blocks.ToArray();
            return blocks.Count - 1;
        }

        // Adds a free cell next to the block to it (one of the block's "+" places).
        public static bool Grow(LevelData level, int index, CellCoord cell)
        {
            if (BlockAt(level, cell) >= 0) return false;
            var cells = LevelValidator.AbsoluteCells(level.Blocks[index]);
            cells.Add(cell);
            SetCells(level.Blocks[index], cells);
            return true;
        }

        // Free board cells side by side with the block: where it can grow.
        public static List<CellCoord> GrowCells(LevelData level, int index)
        {
            var cells = LevelValidator.AbsoluteCells(level.Blocks[index]);
            var result = new List<CellCoord>();
            foreach (var cell in cells)
            {
                foreach (var side in Sides)
                {
                    var next = cell + side;
                    var holes = level.Holes ?? new CellCoord[0];
                    if (GateGeometry.IsBoardCell(next, level.Cols, level.Rows, holes) && !result.Contains(next) &&
                        BlockAt(level, next) < 0) result.Add(next);
                }
            }

            return result;
        }

        // Erases a cell; a block left with no cells is deleted. Returns true when something was erased.
        public static bool RemoveCell(LevelData level, CellCoord cell, out bool blockDeleted)
        {
            blockDeleted = false;
            var index = BlockAt(level, cell);
            if (index < 0) return false;

            var cells = LevelValidator.AbsoluteCells(level.Blocks[index]);
            cells.Remove(cell);
            if (cells.Count == 0)
            {
                Delete(level, index);
                blockDeleted = true;
                return true;
            }

            SetCells(level.Blocks[index], cells);
            return true;
        }

        // Quarter turn clockwise (rows grow downwards), kept at the same top-left corner.
        public static void Rotate(LevelData level, int index) =>
            Transform(level.Blocks[index], c => new CellCoord(-c.Row, c.Col));

        public static void Mirror(LevelData level, int index) =>
            Transform(level.Blocks[index], c => new CellCoord(-c.Col, c.Row));

        // Moves several blocks together by whole cells; the group stops as a whole at the board edge.
        public static bool MoveMany(LevelData level, List<int> indices, int deltaCol, int deltaRow)
        {
            var cells = indices.SelectMany(i => LevelValidator.AbsoluteCells(level.Blocks[i])).ToList();
            if (cells.Count == 0) return false;
            deltaCol = ClampDelta(deltaCol, cells.Min(c => c.Col), cells.Max(c => c.Col), level.Cols);
            deltaRow = ClampDelta(deltaRow, cells.Min(c => c.Row), cells.Max(c => c.Row), level.Rows);
            if (deltaCol == 0 && deltaRow == 0) return false;

            foreach (var i in indices)
            {
                level.Blocks[i].Col += deltaCol;
                level.Blocks[i].Row += deltaRow;
            }

            return true;
        }

        // Adds a copy of the template (any block, also one from another level) at the first free place of the
        // board, scanning rows top to bottom. Returns its index, or -1 when it fits nowhere.
        public static int AddCopy(LevelData level, BlockData template)
        {
            var offsets = template.Cells ?? new CellCoord[0];
            if (offsets.Length == 0) return -1;

            var taken = new HashSet<CellCoord>(level.Blocks.SelectMany(LevelValidator.AbsoluteCells));
            taken.UnionWith(level.Holes ?? new CellCoord[0]);
            for (var row = 0; row < level.Rows; row++)
            for (var col = 0; col < level.Cols; col++)
            {
                var origin = new CellCoord(col, row);
                if (!offsets.All(o => Fits(origin + o, level, taken))) continue;

                var blocks = level.Blocks.ToList();
                blocks.Add(new BlockData
                {
                    Id = NextId(blocks), Col = col, Row = row, Color = template.Color,
                    Cells = (CellCoord[])offsets.Clone()
                });
                level.Blocks = blocks.ToArray();
                return blocks.Count - 1;
            }

            return -1;
        }

        private static bool Fits(CellCoord cell, LevelData level, HashSet<CellCoord> taken) =>
            cell.Col >= 0 && cell.Row >= 0 && cell.Col < level.Cols && cell.Row < level.Rows && !taken.Contains(cell);

        public static void Delete(LevelData level, int index) => DeleteMany(level, new List<int> { index });

        public static void DeleteMany(LevelData level, List<int> indices)
        {
            var blocks = level.Blocks.ToList();
            foreach (var i in indices.OrderByDescending(i => i)) blocks.RemoveAt(i);
            level.Blocks = blocks.ToArray();
        }

        // Takes a board cell out (removed) or puts it back. A cell under a block is not removed.
        public static bool SetRemoved(LevelData level, CellCoord cell, bool removed)
        {
            var holes = (level.Holes ?? new CellCoord[0]).ToList();
            if (removed == holes.Contains(cell)) return false;
            if (removed && BlockAt(level, cell) >= 0) return false;

            if (removed) holes.Add(cell);
            else holes.Remove(cell);
            level.Holes = holes.ToArray();
            return true;
        }

        // After the board shrinks, slides blocks back in where they can go and drops removed cells off the board.
        public static void PullInside(LevelData level)
        {
            level.Holes = (level.Holes ?? new CellCoord[0])
                .Where(h => h.Col >= 0 && h.Row >= 0 && h.Col < level.Cols && h.Row < level.Rows).ToArray();
            for (var i = 0; i < (level.Blocks?.Length ?? 0); i++)
            {
                var cells = LevelValidator.AbsoluteCells(level.Blocks[i]);
                if (cells.Count == 0) continue;
                var maxCol = cells.Max(c => c.Col);
                var maxRow = cells.Max(c => c.Row);
                if (maxCol >= level.Cols) level.Blocks[i].Col -= maxCol - level.Cols + 1;
                if (maxRow >= level.Rows) level.Blocks[i].Row -= maxRow - level.Rows + 1;
                if (level.Blocks[i].Col < 0) level.Blocks[i].Col = 0;
                if (level.Blocks[i].Row < 0) level.Blocks[i].Row = 0;
            }
        }

        private static void Transform(BlockData block, System.Func<CellCoord, CellCoord> map)
        {
            var cells = LevelValidator.AbsoluteCells(block);
            var corner = new CellCoord(cells.Min(c => c.Col), cells.Min(c => c.Row));
            var turned = cells.Select(c => map(new CellCoord(c.Col - corner.Col, c.Row - corner.Row))).ToList();
            var turnedCorner = new CellCoord(turned.Min(c => c.Col), turned.Min(c => c.Row));
            SetCells(block, turned.Select(c => new CellCoord(c.Col - turnedCorner.Col + corner.Col,
                c.Row - turnedCorner.Row + corner.Row)).ToList());
        }

        private static void SetCells(BlockData block, List<CellCoord> cells)
        {
            var corner = new CellCoord(cells.Min(c => c.Col), cells.Min(c => c.Row));
            block.Col = corner.Col;
            block.Row = corner.Row;
            block.Cells = cells.Select(c => new CellCoord(c.Col - corner.Col, c.Row - corner.Row)).ToArray();
        }

        private static int ClampDelta(int delta, int min, int max, int size) =>
            delta < 0 ? System.Math.Max(delta, -min) : System.Math.Min(delta, size - 1 - max);

        // A, B, ... Z, then B27, B28, ... so ids stay short and readable on the board.
        private static string NextId(List<BlockData> blocks)
        {
            var used = new HashSet<string>(blocks.Select(b => b.Id));
            for (var c = 'A'; c <= 'Z'; c++)
            {
                if (!used.Contains(c.ToString())) return c.ToString();
            }

            var n = 27;
            while (used.Contains("B" + n)) n++;
            return "B" + n;
        }
    }
}
