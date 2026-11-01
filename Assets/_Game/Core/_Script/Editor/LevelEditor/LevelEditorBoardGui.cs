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

    // What the mouse is over: a board cell, or a slot on a wall just outside the board.
    internal struct BoardHit
    {
        public BoardHitKind Kind;
        public CellCoord Cell;
        public BoardEdge Edge;
        public int Index;
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
            HashSet<int> errorBlocks, HashSet<int> errorGates, BoardHit hover, List<CellCoord> growPlaces)
        {
            EditorGUI.DrawRect(new Rect(_origin, new Vector2((_cols + 2) * _cell, (_rows + 2) * _cell)), LevelEditorStyle.Wall);
            for (var row = 0; row < _rows; row++)
            for (var col = 0; col < _cols; col++)
                EditorGUI.DrawRect(Inset(CellRect(col, row)), LevelEditorStyle.Cell);

            if (hover.Kind == BoardHitKind.Wall) EditorGUI.DrawRect(SlotRect(hover.Edge, hover.Index), LevelEditorStyle.WallSlotHover);

            var gates = level.Gates ?? new GateData[0];
            for (var i = 0; i < gates.Length; i++) DrawGate(gates[i], isGateSelected(i), errorGates.Contains(i));

            var blocks = level.Blocks ?? new BlockData[0];
            for (var i = 0; i < blocks.Length; i++) DrawBlock(blocks[i], isBlockSelected(i), errorBlocks.Contains(i));
            foreach (var place in growPlaces) DrawPlus(CellRect(place.Col, place.Row));
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

        // The grid position of a wall cell: one cell outside the board.
        public static CellCoord SlotCell(BoardEdge edge, int index, int cols, int rows)
        {
            switch (edge)
            {
                case BoardEdge.Top: return new CellCoord(index, -1);
                case BoardEdge.Bottom: return new CellCoord(index, rows);
                case BoardEdge.Left: return new CellCoord(-1, index);
                default: return new CellCoord(cols, index);
            }
        }

        public BoardHit HitTest(Vector2 mouse)
        {
            if (_cell <= 0f) return new BoardHit();
            var col = Mathf.FloorToInt((mouse.x - _origin.x) / _cell) - 1;
            var row = Mathf.FloorToInt((mouse.y - _origin.y) / _cell) - 1;
            return HitAt(new CellCoord(col, row), _cols, _rows);
        }

        // What a grid position is: a board cell, a wall slot in the ring around the board (corners excluded), or nothing.
        // Shared by the window and the SceneView, which map the mouse to grid positions their own way.
        public static BoardHit HitAt(CellCoord point, int cols, int rows)
        {
            // Cell is the grid position for wall slots too (the wall ring), as the "+" places use.
            var hit = new BoardHit { Cell = point };
            var inCols = point.Col >= 0 && point.Col < cols;
            var inRows = point.Row >= 0 && point.Row < rows;
            if (inCols && inRows)
            {
                hit.Kind = BoardHitKind.Cell;
            }
            else if (inCols && (point.Row == -1 || point.Row == rows))
            {
                hit.Kind = BoardHitKind.Wall;
                hit.Edge = point.Row == -1 ? BoardEdge.Top : BoardEdge.Bottom;
                hit.Index = point.Col;
            }
            else if (inRows && (point.Col == -1 || point.Col == cols))
            {
                hit.Kind = BoardHitKind.Wall;
                hit.Edge = point.Col == -1 ? BoardEdge.Left : BoardEdge.Right;
                hit.Index = point.Row;
            }

            return hit;
        }

        private void DrawBlock(BlockData block, bool selected, bool hasError)
        {
            var cells = LevelValidator.AbsoluteCells(block);
            var color = LevelEditorStyle.Of(block.Color);
            foreach (var cell in cells)
            {
                var rect = CellRect(cell.Col, cell.Row);
                if (selected || hasError) EditorGUI.DrawRect(rect, hasError ? LevelEditorStyle.Error : LevelEditorStyle.Selection);
                EditorGUI.DrawRect(Inset(rect, selected || hasError ? 4f : Gap), color);
            }

            if (cells.Count == 0) return;
            var label = CellRect(cells[0].Col, cells[0].Row);
            GUI.Label(label, block.Id, EditorStyles.whiteBoldLabel);
        }

        private void DrawGate(GateData gate, bool selected, bool hasError)
        {
            for (var i = gate.Index; i < gate.Index + gate.Length; i++)
            {
                var rect = SlotRect(gate.Edge, i);
                if (selected || hasError) EditorGUI.DrawRect(rect, hasError ? LevelEditorStyle.Error : LevelEditorStyle.Selection);
                EditorGUI.DrawRect(Inset(rect, selected || hasError ? 4f : Gap), LevelEditorStyle.Of(gate.Color));
            }
        }

        private Rect CellRect(int col, int row) =>
            new Rect(_origin.x + (col + 1) * _cell, _origin.y + (row + 1) * _cell, _cell, _cell);

        private Rect SlotRect(BoardEdge edge, int index)
        {
            var cell = SlotCell(edge, index, _cols, _rows);
            return CellRect(cell.Col, cell.Row);
        }

        private static Rect Inset(Rect rect, float by = Gap) =>
            new Rect(rect.x + by, rect.y + by, rect.width - by * 2f, rect.height - by * 2f);
    }
}
