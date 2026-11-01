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
        private bool _removingCells;
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
                    var hover = boardRect.Contains(e.mousePosition) ? _board.HitTest(e.mousePosition, Holes) : new BoardHit();
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
                    Press(_board.HitTest(e.mousePosition, Holes), _board.PointAt(e.mousePosition));
                    e.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    Drag(_board.HitTest(e.mousePosition, Holes), _board.PointAt(e.mousePosition));
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
                    else if (HasSelection) ClearSelection();
                    break;
                case LevelEditorTool.Cell:
                    if (hit.InGrid) PressCell(hit.Cell);
                    break;
            }
        }

        // Cell tool: the first cell decides whether this press removes cells or puts them back (Shift always puts back);
        // dragging keeps doing the same to every cell it passes.
        private void PressCell(CellCoord cell)
        {
            var isRemoved = Holes.Contains(cell);
            _removingCells = !_erasing && !isRemoved;
            SetCellRemoved(cell);
        }

        private void SetCellRemoved(CellCoord cell)
        {
            var removing = _removingCells;
            Edit(removing ? "Remove Cell" : "Restore Cell", l => LevelEditorBlockOps.SetRemoved(l, cell, removing));
        }

        private void Drag(BoardHit hit, CellCoord point)
        {
            if (tool == LevelEditorTool.Block)
            {
                if (hit.Kind == BoardHitKind.Cell) DragPaint(hit.Cell);
                return;
            }

            if (tool == LevelEditorTool.Cell)
            {
                if (hit.InGrid) SetCellRemoved(hit.Cell);
                return;
            }

            if (tool == LevelEditorTool.Gate)
            {
                // Dragging along the wall keeps growing the picked gate.
                if (!_erasing && IsGateGrowHit(hit)) GrowGate(hit);
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
                Edit("Move Gate", l => LevelEditorGateOps.Move(l, gate, hit.Edge, hit.Index, hit.Depth));
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
            var gate = hit.Kind == BoardHitKind.Wall ? LevelEditorGateOps.GateAt(working, hit.Edge, hit.Index, hit.Depth) : -1;

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

        // Block tool press: Shift erases; a block picks it (its "+" places show); a "+" grows the picked block; any
        // other free cell leaves the picked block, or starts a new one when nothing is picked.
        private void PaintCell(CellCoord cell)
        {
            if (_erasing)
            {
                EraseCell(cell);
                return;
            }

            var owner = LevelEditorBlockOps.BlockAt(working, cell);
            if (owner >= 0)
            {
                if (owner != PrimaryBlock || selectedBlocks.Count + selectedGates.Count != 1) SelectOnly(owner, -1);
                return;
            }

            if (IsGrowPlace(cell))
            {
                GrowBlock(cell);
                return;
            }

            if (HasSelection)
            {
                ClearSelection();
                return;
            }

            var added = -1;
            Edit("Add Block", l =>
            {
                added = LevelEditorBlockOps.AddBlock(l, cell, brush);
                return true;
            });
            SelectOnly(added, -1);
        }

        // Block tool drag: keeps erasing, or keeps growing the picked block through its "+" places.
        private void DragPaint(CellCoord cell)
        {
            if (_erasing) EraseCell(cell);
            else if (IsGrowPlace(cell)) GrowBlock(cell);
        }

        private void EraseCell(CellCoord cell)
        {
            Edit("Erase Cell", l => LevelEditorBlockOps.RemoveCell(l, cell, out _));
            PruneSelection();
        }

        // Gate tool press: Shift deletes; a gate picks it (its "+" places show); a "+" grows the picked gate; any other
        // free slot leaves the picked gate, or adds a new one when nothing is picked.
        private void PressGate(BoardHit hit)
        {
            var gate = LevelEditorGateOps.GateAt(working, hit.Edge, hit.Index, hit.Depth);
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

            // A "+" next to the picked gate makes it longer; any other free slot leaves it first.
            if (IsGateGrowHit(hit))
            {
                GrowGate(hit);
                return;
            }

            if (HasSelection)
            {
                ClearSelection();
                return;
            }

            var added = -1;
            Edit("Add Gate", l =>
            {
                added = LevelEditorGateOps.Add(l, hit.Edge, hit.Index, hit.Depth, brush);
                return true;
            });
            SelectOnly(-1, added);
        }

        private void HandleShortcuts()
        {
            var e = Event.current;
            if (HandleShortcut(e)) e.Use();
        }

        // The editor's keys, from the window or the SceneView. Returns whether the key was one of them.
        internal bool HandleShortcut(Event e)
        {
            if (e.type != EventType.KeyDown || EditorGUIUtility.editingTextField || working == null) return false;

            var command = e.control || e.command;
            switch (e.keyCode)
            {
                case KeyCode.Alpha1: SwitchTool(LevelEditorTool.Select); break;
                case KeyCode.Alpha2: SwitchTool(LevelEditorTool.Block); break;
                case KeyCode.Alpha3: SwitchTool(LevelEditorTool.Gate); break;
                case KeyCode.Alpha4: SwitchTool(LevelEditorTool.Cell); break;
                case KeyCode.Delete:
                case KeyCode.Backspace: DeleteSelection(); break;
                case KeyCode.R when !command: RotateSelection(); break;
                case KeyCode.D when command: DuplicateSelection(); break;
                case KeyCode.C when command: CopySelection(); break;
                case KeyCode.V when command: PasteBlocks(LevelEditorClipboard.Paste(), "Paste"); break;
                case KeyCode.S when command: Save(); break;
                case KeyCode.Escape: ClearSelection(); break;
                default: return false;
            }

            Repaint();
            return true;
        }

        // Edit menu commands (Cmd/Ctrl+D, C, V, Delete) as Unity sends them to a focused view. Returns whether the
        // command is one the editor takes; execute false only asks.
        internal bool HandleCommand(string name, bool execute)
        {
            if (working == null || EditorGUIUtility.editingTextField) return false;

            switch (name)
            {
                case "Duplicate":
                    if (execute) DuplicateSelection();
                    return true;
                case "Copy":
                    if (execute) CopySelection();
                    return true;
                case "Paste":
                    if (execute) PasteBlocks(LevelEditorClipboard.Paste(), "Paste");
                    return true;
                case "Delete":
                case "SoftDelete":
                    if (execute) DeleteSelection();
                    return true;
                default:
                    return false;
            }
        }

        private void CopySelection() => LevelEditorClipboard.Copy(working, SelectedBlockIndices());

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
