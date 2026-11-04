using System;
using System.Collections.Generic;
using ColorBlockJam.Core.Level;
using ColorBlockJam.Core.Settings;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    internal enum BoardHitKind
    {
        None,
        Cell,
        Wall
    }

    // What the mouse is over: a board cell, or a wall place (Edge, Index, Depth as in GateData) on a board cell's side
    // that faces off the board or a removed cell. Cell is always the grid position under the mouse; InGrid is true for
    // board cells and removed cells alike.
    internal struct BoardHit
    {
        public BoardHitKind Kind;
        public CellCoord Cell;
        public bool InGrid;
        public BoardEdge Edge;
        public int Index;
        public int Depth;
    }

    // Draws the level as a grid in the window, with a one-cell wall ring around it where the gates sit.
    // Row 0 is the top row, as in the game.
    internal sealed class LevelEditorBoardGui
    {
        private const float MaxCellSize = 64f;
        private const float Gap = 2f;

        private Vector2 _origin;
        private float _cell;
        private int _cols;
        private int _rows;

        public void Layout(Rect area, LevelData level)
        {
            _cols = level.Cols;
            _rows = level.Rows;
            _cell = Mathf.Min(MaxCellSize, Mathf.Min(area.width / (_cols + 2), area.height / (_rows + 2)));
            var size = new Vector2((_cols + 2) * _cell, (_rows + 2) * _cell);
            _origin = area.center - size / 2f;
        }

        public void Draw(LevelData level, Func<int, bool> isBlockSelected, Func<int, bool> isGateSelected,
            HashSet<int> errorBlocks, HashSet<int> errorGates, BoardHit hover, List<CellCoord> growPlaces, Color hoverColor)
        {
            EditorGUI.DrawRect(new Rect(_origin, new Vector2((_cols + 2) * _cell, (_rows + 2) * _cell)), LevelEditorStyle.Wall);
            var holes = new HashSet<CellCoord>(level.Holes ?? new CellCoord[0]);
            for (var row = 0; row < _rows; row++)
            for (var col = 0; col < _cols; col++)
            {
                var removed = holes.Contains(new CellCoord(col, row));
                EditorGUI.DrawRect(Inset(CellRect(col, row)), removed ? LevelEditorStyle.Background : LevelEditorStyle.Cell);
            }

            var gates = level.Gates ?? new GateData[0];
            for (var i = 0; i < gates.Length; i++) DrawGate(gates[i], isGateSelected(i), errorGates.Contains(i));

            var blocks = level.Blocks ?? new BlockData[0];
            for (var i = 0; i < blocks.Length; i++) DrawBlock(blocks[i], isBlockSelected(i), errorBlocks.Contains(i));
            foreach (var place in growPlaces) DrawPlus(CellRect(place.Col, place.Row));

            // Over the blocks and gates, so the red of an erase shows on what it takes away.
            if (hover.Kind == BoardHitKind.Wall)
                EditorGUI.DrawRect(StripRect(hover.Edge, hover.Index, hover.Depth), hoverColor);
            else if (hover.InGrid)
                EditorGUI.DrawRect(CellRect(hover.Cell.Col, hover.Cell.Row), hoverColor);
        }

        // A green "+" where the picked block or gate can grow.
        private void DrawPlus(Rect rect)
        {
            var inner = Inset(rect, Mathf.Max(Gap, rect.width * 0.12f));
            EditorGUI.DrawRect(inner, LevelEditorStyle.Grow);
            var bar = Mathf.Max(2f, inner.width * 0.12f);
            var arm = inner.width * 0.6f;
            EditorGUI.DrawRect(new Rect(inner.center.x - arm / 2f, inner.center.y - bar / 2f, arm, bar), Color.white);
            EditorGUI.DrawRect(new Rect(inner.center.x - bar / 2f, inner.center.y - arm / 2f, bar, arm), Color.white);
        }

        // Where a dragged shape would go: green when it fits, red when it does not.
        public void DrawGhost(List<CellCoord> cells, bool fits)
        {
            var color = fits ? new Color(0.2f, 0.8f, 0.3f, 0.6f) : new Color(1f, 0.25f, 0.25f, 0.6f);
            foreach (var cell in cells) EditorGUI.DrawRect(Inset(CellRect(cell.Col, cell.Row)), color);
        }

        // Outline of a selection box from cell min to cell max (wall cells included).
        public void DrawBox(CellCoord min, CellCoord max)
        {
            var from = CellRect(min.Col, min.Row);
            var to = CellRect(max.Col, max.Row);
            var box = Rect.MinMaxRect(from.xMin, from.yMin, to.xMax, to.yMax);
            Handles.DrawSolidRectangleWithOutline(box, new Color(1f, 1f, 1f, 0.06f), LevelEditorStyle.Selection);
        }

        // The grid cell under the mouse, held to the wall ring around the board; used for box corners.
        public CellCoord PointAt(Vector2 mouse)
        {
            if (_cell <= 0f) return default;
            var col = Mathf.FloorToInt((mouse.x - _origin.x) / _cell) - 1;
            var row = Mathf.FloorToInt((mouse.y - _origin.y) / _cell) - 1;
            return new CellCoord(Mathf.Clamp(col, -1, _cols), Mathf.Clamp(row, -1, _rows));
        }

        public BoardHit HitTest(Vector2 mouse, ICollection<CellCoord> holes)
        {
            if (_cell <= 0f) return new BoardHit();
            var x = (mouse.x - _origin.x) / _cell - 1f;
            var y = (mouse.y - _origin.y) / _cell - 1f;
            var point = new CellCoord(Mathf.FloorToInt(x), Mathf.FloorToInt(y));
            return HitAt(point, new Vector2(x - point.Col, y - point.Row), _cols, _rows, holes);
        }

        // What a grid position is. A board cell is a Cell; any other position next to board cells (the ring around the
        // board, or a removed cell) is the wall place on the board cell beside it that `within` (0..1 inside the
        // position, rows down) points to most. Shared by the window and the SceneView.
        public static BoardHit HitAt(CellCoord point, Vector2 within, int cols, int rows, ICollection<CellCoord> holes)
        {
            var hit = new BoardHit
            {
                Cell = point,
                InGrid = point.Col >= 0 && point.Row >= 0 && point.Col < cols && point.Row < rows
            };
            if (GateGeometry.IsBoardCell(point, cols, rows, holes))
            {
                hit.Kind = BoardHitKind.Cell;
                return hit;
            }

            var offset = within - new Vector2(0.5f, 0.5f);
            var best = float.NegativeInfinity;
            foreach (var edge in Edges)
            {
                // The neighbour on that side of the position, and the wall on its side facing back to the position.
                var toward = GateGeometry.Normal(edge);
                var neighbour = point + toward;
                if (!GateGeometry.IsBoardCell(neighbour, cols, rows, holes)) continue;

                var score = offset.x * toward.Col + offset.y * toward.Row;
                if (score <= best) continue;
                best = score;
                hit.Kind = BoardHitKind.Wall;
                hit.Edge = GateGeometry.EdgeOfNormal(new CellCoord(-toward.Col, -toward.Row));
                GateGeometry.PlaceOf(neighbour, hit.Edge, cols, rows, out hit.Index, out hit.Depth);
            }

            return hit;
        }

        private static readonly BoardEdge[] Edges = { BoardEdge.Top, BoardEdge.Bottom, BoardEdge.Left, BoardEdge.Right };

        private void DrawBlock(BlockData block, bool selected, bool hasError)
        {
            var cells = LevelValidator.AbsoluteCells(block);
            var color = LevelEditorStyle.Of(block.Color);
            foreach (var cell in cells)
            {
                var rect = CellRect(cell.Col, cell.Row);
                if (selected || hasError) EditorGUI.DrawRect(rect, hasError ? LevelEditorStyle.Error : LevelEditorStyle.Selection);
                var inner = Inset(rect, selected || hasError ? 4f : Gap);
                EditorGUI.DrawRect(inner, color);
                if (block.Ice > 0) EditorGUI.DrawRect(inner, IceTint);
                DrawAxisMark(inner, block.Axis);
            }

            if (cells.Count == 0) return;
            var label = CellRect(cells[0].Col, cells[0].Row);
            GUI.Label(label, block.Ice > 0 ? $"{block.Id} ❄{block.Ice}" : block.Id, EditorStyles.whiteBoldLabel);
        }

        private static readonly Color IceTint = new Color(0.85f, 0.95f, 1f, 0.55f);
        private static readonly Color AxisMark = new Color(1f, 1f, 1f, 0.8f);

        // A one-way block gets a bar through each cell along the way it can move.
        private static void DrawAxisMark(Rect cell, BlockAxis axis)
        {
            var thickness = Mathf.Max(2f, cell.width * 0.12f);
            if (axis == BlockAxis.Horizontal)
                EditorGUI.DrawRect(new Rect(cell.x, cell.center.y - thickness / 2f, cell.width, thickness), AxisMark);
            else if (axis == BlockAxis.Vertical)
                EditorGUI.DrawRect(new Rect(cell.center.x - thickness / 2f, cell.y, thickness, cell.height), AxisMark);
        }

        private void DrawGate(GateData gate, bool selected, bool hasError)
        {
            for (var i = gate.Index; i < gate.Index + gate.Length; i++)
            {
                var rect = StripRect(gate.Edge, i, gate.Depth);
                if (selected || hasError) EditorGUI.DrawRect(rect, hasError ? LevelEditorStyle.Error : LevelEditorStyle.Selection);
                EditorGUI.DrawRect(Inset(rect, selected || hasError ? 4f : Gap), LevelEditorStyle.Of(gate.Color));
            }
        }

        private Rect CellRect(int col, int row) =>
            new Rect(_origin.x + (col + 1) * _cell, _origin.y + (row + 1) * _cell, _cell, _cell);

        // The half of the cell past a wall place that lies against the wall, where its gate is drawn (so gates on two
        // sides of one removed cell do not cover each other).
        private Rect StripRect(BoardEdge edge, int index, int depth)
        {
            var cell = GateGeometry.Outside(edge, depth, index, _cols, _rows);
            var rect = CellRect(cell.Col, cell.Row);
            var half = rect.width / 2f;
            switch (edge)
            {
                case BoardEdge.Top: return new Rect(rect.x, rect.y + half, rect.width, half);
                case BoardEdge.Bottom: return new Rect(rect.x, rect.y, rect.width, half);
                case BoardEdge.Left: return new Rect(rect.x + half, rect.y, half, rect.height);
                default: return new Rect(rect.x, rect.y, half, rect.height);
            }
        }

        private static Rect Inset(Rect rect, float by = Gap) =>
            new Rect(rect.x + by, rect.y + by, rect.width - by * 2f, rect.height - by * 2f);
    }
}
