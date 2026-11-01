using System.Collections.Generic;
using System.Linq;
using ColorBlockJam.Core.Level;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Shapes from the library dragged onto the board (Shapes panel, window or SceneView): a ghost shows where the shape
    // would go and whether it fits; dropping adds it as a block in the brush colour.
    public sealed partial class LevelEditorSession
    {
        // DragAndDrop generic data key; its value is the shape's index in the library.
        public const string ShapeDragKey = "ColorBlockJam.LevelEditor.Shape";

        private readonly List<CellCoord> _ghost = new List<CellCoord>();
        private bool _ghostFits;

        internal bool CanAddSelectedShape => selectedBlocks.Count == 1 && selectedGates.Count == 0 && PrimaryBlock >= 0;

        internal void AddSelectedToShapes()
        {
            if (!CanAddSelectedShape) return;
            var block = working.Blocks[PrimaryBlock];
            var library = LevelEditorShapeLibrary.Get();
            library.Add($"Shape {library.Shapes.Count + 1}", block.Cells);
            ShowNotification(new GUIContent("Shape added to the Shapes panel"));
            RaiseStateChanged();
        }

        // A shape dragged over the board with its top-left corner at `at` (null: off the board). Updates the ghost and,
        // when `drop`, adds the block. Returns whether it fits there.
        internal bool DragShape(int index, CellCoord? at, bool drop)
        {
            var shapes = LevelEditorShapeLibrary.Get().Shapes;
            _ghost.Clear();
            _ghostFits = false;
            if (EnsureWorking() == null || index < 0 || index >= shapes.Count || !at.HasValue)
            {
                RaiseStateChanged();
                return false;
            }

            var holes = Holes;
            _ghostFits = true;
            foreach (var offset in shapes[index].Cells)
            {
                var cell = at.Value + offset;
                _ghost.Add(cell);
                if (!GateGeometry.IsBoardCell(cell, working.Cols, working.Rows, holes) ||
                    LevelEditorBlockOps.BlockAt(working, cell) >= 0) _ghostFits = false;
            }

            if (drop)
            {
                var fits = _ghostFits;
                var cells = _ghost.ToList();
                ClearShapeGhost();
                if (!fits)
                {
                    ShowNotification(new GUIContent("The shape does not fit there"));
                    return false;
                }

                var added = -1;
                Edit("Add Shape", l =>
                {
                    added = LevelEditorBlockOps.AddBlock(l, cells, brush);
                    return true;
                });
                SelectOnly(added, -1);
                return true;
            }

            RaiseStateChanged();
            return _ghostFits;
        }

        internal void ClearShapeGhost()
        {
            if (_ghost.Count == 0) return;
            _ghost.Clear();
            RaiseStateChanged();
        }

        // The shape being dragged in from the Shapes panel, if any.
        internal static int DraggedShape() => DragAndDrop.GetGenericData(ShapeDragKey) is int index ? index : -1;

        public static bool SceneDragShape(CellCoord? at, bool drop)
        {
            var index = DraggedShape();
            return index >= 0 && instance.DragShape(index, at, drop);
        }

        public static void SceneEndShapeDrag() => instance.ClearShapeGhost();

        public static bool GetShapeGhost(List<CellCoord> cells, out bool fits)
        {
            cells.Clear();
            cells.AddRange(instance._ghost);
            fits = instance._ghostFits;
            return cells.Count > 0;
        }

        // A left click off the board in the SceneView leaves what was picked (and its "+" places).
        public static void SceneClickOutside()
        {
            if (!CanEditFromScene) return;
            instance.CancelEyedropper();
            if (instance.HasSelection) instance.ClearSelection();
        }
    }
}
