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
            HashSet<int> errorBlocks, HashSet<int> errorGates, BoardHit hover)
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
            var hit = new BoardHit();
            if (_cell <= 0f) return hit;

            var col = Mathf.FloorToInt((mouse.x - _origin.x) / _cell) - 1;
            var row = Mathf.FloorToInt((mouse.y - _origin.y) / _cell) - 1;
            var inCols = col >= 0 && col < _cols;
            var inRows = row >= 0 && row < _rows;
            if (inCols && inRows)
            {
                hit.Kind = BoardHitKind.Cell;
                hit.Cell = new CellCoord(col, row);
            }
            else if (inCols && (row == -1 || row == _rows))
            {
                hit.Kind = BoardHitKind.Wall;
                hit.Edge = row == -1 ? BoardEdge.Top : BoardEdge.Bottom;
                hit.Index = col;
            }
            else if (inRows && (col == -1 || col == _cols))
            {
                hit.Kind = BoardHitKind.Wall;
                hit.Edge = col == -1 ? BoardEdge.Left : BoardEdge.Right;
                hit.Index = row;
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
