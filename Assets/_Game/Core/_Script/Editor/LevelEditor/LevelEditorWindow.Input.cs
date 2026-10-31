using System.Collections.Generic;
using ColorBlockJam.Core.Level;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Board mouse input per tool, and keyboard shortcuts. One mouse press to release is one undo step.
    public sealed partial class LevelEditorWindow
    {
        private bool _erasing;
        private bool _adding;
        private bool _draggingBlocks;
        private bool _draggingGate;
        private bool _boxing;
        private CellCoord _dragCell;
        private CellCoord _boxStart;
        private CellCoord _boxEnd;
        private int _undoGroup;
        private BoardHit _hover;

        private void HandleBoardInput(Rect boardRect)
        {
            var e = Event.current;
            var id = GUIUtility.GetControlID(FocusType.Passive, boardRect);
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseMove:
                    var hover = boardRect.Contains(e.mousePosition) ? _board.HitTest(e.mousePosition) : new BoardHit();
                    if (hover.Kind != _hover.Kind || hover.Cell != _hover.Cell || hover.Edge != _hover.Edge || hover.Index != _hover.Index)
                    {
                        _hover = hover;
                        Repaint();
                    }
                    break;
                case EventType.MouseDown when e.button == 0 && boardRect.Contains(e.mousePosition):
                    GUIUtility.hotControl = id;
                    Undo.IncrementCurrentGroup();
                    _undoGroup = Undo.GetCurrentGroup();
                    _erasing = e.shift;
                    _adding = e.shift || e.control || e.command;
                    Press(_board.HitTest(e.mousePosition), _board.PointAt(e.mousePosition));
                    e.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    Drag(_board.HitTest(e.mousePosition), _board.PointAt(e.mousePosition));
                    e.Use();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    Release();
                    e.Use();
                    break;
            }
        }

        private void Press(BoardHit hit, CellCoord point)
        {
            switch (tool)
            {
                case LevelEditorTool.Select:
                    PressSelect(hit, point);
                    break;
                case LevelEditorTool.Block:
                    if (hit.Kind == BoardHitKind.Cell) PaintCell(hit.Cell);
                    break;
                case LevelEditorTool.Gate:
                    if (hit.Kind == BoardHitKind.Wall) PressGate(hit);
                    break;
            }
        }

        private void Drag(BoardHit hit, CellCoord point)
        {
            if (tool == LevelEditorTool.Block)
            {
                if (hit.Kind == BoardHitKind.Cell) PaintCell(hit.Cell);
                return;
            }

            if (tool != LevelEditorTool.Select) return;

            if (_boxing)
            {
                _boxEnd = point;
                Repaint();
            }
            else if (_draggingBlocks && hit.Kind == BoardHitKind.Cell && hit.Cell != _dragCell)
            {
                DragBlocks(hit.Cell);
            }
            else if (_draggingGate && hit.Kind == BoardHitKind.Wall)
            {
                var gate = PrimaryGate;
                if (gate < 0) return;
                var before = GateKey.Of(working.Gates[gate]);
                Edit("Move Gate", l => LevelEditorGateOps.Move(l, gate, hit.Edge, hit.Index));
                FollowGate(before, gate);
            }
        }

        private void Release()
        {
            if (_boxing) SelectInBox(BoxMin, BoxMax);
            _boxing = false;
            _draggingBlocks = false;
            _draggingGate = false;
            Undo.CollapseUndoOperations(_undoGroup);
            Repaint();
        }

        private CellCoord BoxMin => new CellCoord(Mathf.Min(_boxStart.Col, _boxEnd.Col), Mathf.Min(_boxStart.Row, _boxEnd.Row));
        private CellCoord BoxMax => new CellCoord(Mathf.Max(_boxStart.Col, _boxEnd.Col), Mathf.Max(_boxStart.Row, _boxEnd.Row));

        // On a block or gate: picks it (Shift/Ctrl toggles it) and starts dragging what is picked.
        // On nothing: starts a selection box, dropping the selection unless adding.
        private void PressSelect(BoardHit hit, CellCoord point)
        {
            var block = hit.Kind == BoardHitKind.Cell ? LevelEditorBlockOps.BlockAt(working, hit.Cell) : -1;
            var gate = hit.Kind == BoardHitKind.Wall ? LevelEditorGateOps.GateAt(working, hit.Edge, hit.Index) : -1;

            if (block >= 0)
            {
                if (_adding)
                {
                    ToggleBlock(block);
                    return;
                }

                if (!IsBlockSelected(block)) SelectOnly(block, -1);
                _draggingBlocks = true;
                _dragCell = hit.Cell;
            }
            else if (gate >= 0)
            {
                if (_adding)
                {
                    ToggleGate(gate);
                    return;
                }

                SelectOnly(-1, gate);
                _draggingGate = true;
            }
            else
            {
                if (!_adding) ClearSelection();
                _boxing = true;
                _boxStart = point;
                _boxEnd = point;
            }
        }

        // Every selected block moves with the one under the mouse.
        private void DragBlocks(CellCoord cell)
        {
            var blocks = SelectedBlockIndices();
            if (blocks.Count == 0) return;

            var first = working.Blocks[blocks[0]];
            var before = new CellCoord(first.Col, first.Row);
            Edit("Move Blocks", l => LevelEditorBlockOps.MoveMany(l, blocks, cell.Col - _dragCell.Col, cell.Row - _dragCell.Row));
            // Only the part of the move the board allowed counts, so the blocks stay under the same cell.
            _dragCell = _dragCell + new CellCoord(first.Col - before.Col, first.Row - before.Row);
        }

        // Paints with the brush colour; Shift (held from the press) erases.
        private void PaintCell(CellCoord cell)
        {
            if (_erasing)
            {
                Edit("Erase Cell", l => LevelEditorBlockOps.RemoveCell(l, cell, out _));
                PruneSelection();
                return;
            }

            var owner = LevelEditorBlockOps.BlockAt(working, cell);
            if (owner >= 0)
            {
                // A press on a block picks it, so the next painted cells grow it.
                if (owner != PrimaryBlock) SelectOnly(owner, -1);
                return;
            }

            var grown = -1;
            Edit("Paint Cell", l =>
            {
                grown = LevelEditorBlockOps.AddCell(l, cell, brush, PrimaryBlock, out var changed);
                return changed;
            });
            if (grown >= 0 && grown != PrimaryBlock) SelectOnly(grown, -1);
        }

        // Adds a gate on an empty wall slot, picks an existing one; Shift deletes it.
        private void PressGate(BoardHit hit)
        {
            var gate = LevelEditorGateOps.GateAt(working, hit.Edge, hit.Index);
            if (_erasing)
            {
                if (gate < 0) return;
                Edit("Delete Gate", l =>
                {
                    LevelEditorGateOps.Delete(l, gate);
                    return true;
                });
                PruneSelection();
                return;
            }

            if (gate >= 0)
            {
                SelectOnly(-1, gate);
                return;
            }

            var added = -1;
            Edit("Add Gate", l =>
            {
                added = LevelEditorGateOps.Add(l, hit.Edge, hit.Index, brush);
                return true;
            });
            SelectOnly(-1, added);
        }

        private void HandleShortcuts()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown || EditorGUIUtility.editingTextField) return;

            var command = e.control || e.command;
            switch (e.keyCode)
            {
                case KeyCode.Alpha1: tool = LevelEditorTool.Select; break;
                case KeyCode.Alpha2: tool = LevelEditorTool.Block; break;
                case KeyCode.Alpha3: tool = LevelEditorTool.Gate; break;
                case KeyCode.Delete:
                case KeyCode.Backspace: DeleteSelection(); break;
                case KeyCode.R when !command: RotateSelection(); break;
                case KeyCode.D when command: DuplicateSelection(); break;
                case KeyCode.C when command: LevelEditorClipboard.Copy(working, SelectedBlockIndices()); break;
                case KeyCode.V when command: PasteBlocks(LevelEditorClipboard.Paste(), "Paste"); break;
                case KeyCode.S when command: Save(); break;
                case KeyCode.Escape: ClearSelection(); break;
                default: return;
            }

            e.Use();
            Repaint();
        }

        private void DeleteSelection()
        {
            var blocks = SelectedBlockIndices();
            var gates = SelectedGateIndices();
            if (blocks.Count + gates.Count == 0) return;

            Edit("Delete Selection", l =>
            {
                LevelEditorBlockOps.DeleteMany(l, blocks);
                LevelEditorGateOps.DeleteMany(l, gates);
                return true;
            });
            ClearSelection();
        }

        private void RotateSelection() => ForEachSelectedBlock("Rotate", LevelEditorBlockOps.Rotate);

        private void MirrorSelection() => ForEachSelectedBlock("Mirror", LevelEditorBlockOps.Mirror);

        private void ForEachSelectedBlock(string undoName, System.Action<Core.Settings.LevelData, int> change)
        {
            var blocks = SelectedBlockIndices();
            if (blocks.Count == 0) return;
            Edit(undoName, l =>
            {
                foreach (var i in blocks) change(l, i);
                return true;
            });
        }

        // Copies every selected block to the first free places of the board, and picks the copies.
        private void DuplicateSelection()
        {
            var templates = new List<BlockData>();
            foreach (var i in SelectedBlockIndices()) templates.Add(working.Blocks[i]);
            PasteBlocks(templates, "Duplicate");
        }

        private void PasteBlocks(List<BlockData> templates, string undoName)
        {
            if (templates.Count == 0) return;

            var copies = new List<int>();
            Edit(undoName, l =>
            {
                foreach (var template in templates)
                {
                    var copy = LevelEditorBlockOps.AddCopy(l, template);
                    if (copy >= 0) copies.Add(copy);
                }

                return copies.Count > 0;
            });

            if (copies.Count < templates.Count)
                ShowNotification(new GUIContent($"{templates.Count - copies.Count} block(s) found no free place"));
            ClearSelection();
            AddToSelection(copies, new int[0]);
        }
    }
}
