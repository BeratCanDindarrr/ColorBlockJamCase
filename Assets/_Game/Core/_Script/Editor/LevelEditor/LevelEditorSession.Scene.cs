using System.Collections.Generic;
using ColorBlockJam.Core.Level;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Editing from the SceneView. The scene preview (SandBox editor code) maps the mouse to grid positions — board
    // cells, plus the wall ring at -1 and Cols/Rows — and drives the same tools as the window's board.
    public sealed partial class LevelEditorSession
    {
        public static bool CanEditFromScene => instance.EnsureWorking() != null;

        // `within` is where in the grid position the mouse is (0..1, rows down): it picks the wall side of a removed cell.
        public static void ScenePress(CellCoord point, Vector2 within, bool shift, bool command)
        {
            if (!CanEditFromScene) return;
            var session = instance;
            Undo.IncrementCurrentGroup();
            session._undoGroup = Undo.GetCurrentGroup();
            session._erasing = session.ErasesNow(shift);
            session._adding = shift || command;
            session.Press(session.SceneHit(point, within), point);
            Repaint();
        }

        public static void SceneDrag(CellCoord point, Vector2 within)
        {
            if (!CanEditFromScene) return;
            instance.Drag(instance.SceneHit(point, within), point);
            Repaint();
        }

        public static void SceneRelease()
        {
            if (!CanEditFromScene) return;
            instance.Release();
        }

        // What the scene overlay draws: every grid position of the selected blocks and gates, the "+" places where the
        // picked one can grow, and the box being dragged out, if any.
        public static bool GetSceneMarks(List<CellCoord> selected, List<CellCoord> growPlaces, out CellCoord boxMin,
            out CellCoord boxMax)
        {
            selected.Clear();
            growPlaces.Clear();
            boxMin = boxMax = default;
            if (!CanEditFromScene) return false;

            var window = instance;
            var level = window.working;
            foreach (var i in window.SelectedBlockIndices()) selected.AddRange(LevelValidator.AbsoluteCells(level.Blocks[i]));
            foreach (var i in window.SelectedGateIndices())
            {
                var gate = level.Gates[i];
                for (var k = gate.Index; k < gate.Index + gate.Length; k++)
                    selected.Add(GateGeometry.Outside(gate.Edge, gate.Depth, k, level.Cols, level.Rows));
            }

            growPlaces.AddRange(window.GrowPlaces());
            boxMin = window.BoxMin;
            boxMax = window.BoxMax;
            return window._boxing;
        }

        public static bool SceneShortcut(Event e) => CanEditFromScene && instance.HandleShortcut(e);

        public static bool SceneCommand(string name, bool execute) => CanEditFromScene && instance.HandleCommand(name, execute);

        private BoardHit SceneHit(CellCoord point, Vector2 within) =>
            LevelEditorBoardGui.HitAt(point, within, working.Cols, working.Rows, Holes);
    }
}
