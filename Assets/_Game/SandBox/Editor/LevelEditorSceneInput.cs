using System.Collections.Generic;
using ColorBlockJam.Core.Level;
using ColorBlockJam.LevelEditor;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.SandBox.Editor
{
    // Edits the level from the SceneView: the mouse is cast onto the preview board at the game's pick height (as the
    // game's own drag does) and turned into a grid position for the level editor's tools. Left mouse edits; Alt and the
    // right/middle buttons keep moving the view. Draws the hovered cell, the selection and the selection box.
    [InitializeOnLoad]
    internal static class LevelEditorSceneInput
    {
        // Marks float just above the pick plane so they read over the blocks.
        private const float MarkLift = 0.02f;
        private static readonly Color SelectionFill = new Color(1f, 1f, 1f, 0.18f);
        private static readonly Color BoxFill = new Color(1f, 1f, 1f, 0.06f);

        private static readonly int ControlHint = "LevelEditorSceneInput".GetHashCode();
        private static readonly List<CellCoord> Marks = new List<CellCoord>();
        private static readonly List<CellCoord> GrowPlaces = new List<CellCoord>();
        private static readonly List<CellCoord> Ghost = new List<CellCoord>();
        private static readonly Color GhostFits = new Color(0.2f, 0.8f, 0.3f, 0.55f);
        private static readonly Color GhostBlocked = new Color(1f, 0.25f, 0.25f, 0.55f);
        private static readonly Color GrowFill = new Color(0.20f, 0.72f, 0.30f, 0.85f);
        private static readonly Vector3[] Quad = new Vector3[4];
        private static CellCoord? _hover;
        private static bool _lastShift;

        static LevelEditorSceneInput()
        {
            SceneView.duringSceneGui += OnSceneGui;
        }

        private static void OnSceneGui(SceneView sceneView)
        {
            var board = LevelEditorScenePreview.Board;
            if (board == null || board.Layout == null || !LevelEditorSession.CanEditFromScene) return;

            var e = Event.current;
            if (HandleKeys(e)) return;
            // The problem card's buttons get presses on it; only presses, so a drag ending there still finishes.
            if (e.type == EventType.MouseDown && LevelEditorSession.IsOverProblemToast(e.mousePosition)) return;

            var id = GUIUtility.GetControlID(ControlHint, FocusType.Passive);
            // Clicks on the board edit the level instead of picking scene objects.
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(id);

            // While a press is held the point is kept to the wall ring, so a drag past the board stays at its edge.
            var holding = GUIUtility.hotControl == id;
            var point = PointUnderMouse(board, e.mousePosition, holding, out var within);
            if (HandleShapeDrag(e, point)) return;

            // The hover tint follows Shift (it flips + / −), so pressing or letting go of it redraws.
            if (e.shift != _lastShift)
            {
                _lastShift = e.shift;
                sceneView.Repaint();
            }

            if (_hover.HasValue && LevelEditorSession.ErasesFromScene(e.shift))
                EditorGUIUtility.AddCursorRect(new Rect(0f, 0f, sceneView.position.width, sceneView.position.height),
                    MouseCursor.ArrowMinus);

            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseMove:
                    if (!Equals(point, _hover))
                    {
                        _hover = point;
                        sceneView.Repaint();
                    }
                    break;
                case EventType.MouseDown when e.button == 0 && !e.alt && !point.HasValue:
                    // Off the board: leave what was picked (and its "+" places).
                    LevelEditorSession.SceneClickOutside();
                    break;
                case EventType.MouseDown when e.button == 0 && !e.alt && point.HasValue:
                    GUIUtility.hotControl = id;
                    LevelEditorSession.ScenePress(point.Value, within, e.shift, e.control || e.command);
                    e.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    if (point.HasValue) LevelEditorSession.SceneDrag(point.Value, within);
                    _hover = point;
                    e.Use();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    LevelEditorSession.SceneRelease();
                    e.Use();
                    break;
                case EventType.Repaint:
                    DrawMarks(board);
                    break;
            }
        }

        // A shape dragged in from the Shapes panel: its ghost follows the mouse, a drop adds it as a block.
        private static bool HandleShapeDrag(Event e, CellCoord? point)
        {
            switch (e.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (DragAndDrop.GetGenericData(LevelEditorSession.ShapeDragKey) == null) return false;
                    var drop = e.type == EventType.DragPerform;
                    var fits = LevelEditorSession.SceneDragShape(point, drop);
                    DragAndDrop.visualMode = fits ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                    if (drop) DragAndDrop.AcceptDrag();
                    e.Use();
                    return true;
                case EventType.DragExited:
                    LevelEditorSession.SceneEndShapeDrag();
                    return false;
                default:
                    return false;
            }
        }

        // The editor's keys and Edit menu commands (duplicate, copy, paste, delete) work in the SceneView too.
        private static bool HandleKeys(Event e)
        {
            switch (e.type)
            {
                case EventType.KeyDown when LevelEditorSession.SceneShortcut(e):
                    e.Use();
                    return true;
                case EventType.ValidateCommand when LevelEditorSession.SceneCommand(e.commandName, false):
                case EventType.ExecuteCommand when LevelEditorSession.SceneCommand(e.commandName, true):
                    e.Use();
                    return true;
                default:
                    return false;
            }
        }

        // The grid position under the mouse, within the wall ring around the board; null off the board plane, or beyond
        // the ring unless `clampToRing` (then the nearest ring position).
        private static CellCoord? PointUnderMouse(BoardViewController board, Vector2 mouse, bool clampToRing,
            out Vector2 within)
        {
            within = new Vector2(0.5f, 0.5f);
            var root = board.transform;
            var plane = new Plane(root.up, root.position + root.up * LevelEditorScenePreview.Settings.PickPlaneHeight);
            var ray = HandleUtility.GUIPointToWorldRay(mouse);
            if (!plane.Raycast(ray, out var enter)) return null;

            var layout = board.Layout;
            var point = layout.LocalToBoard(root.InverseTransformPoint(ray.GetPoint(enter)));
            var col = Mathf.FloorToInt((float)point.Col);
            var row = Mathf.FloorToInt((float)point.Row);
            var outside = col < -1 || row < -1 || col > layout.Cols || row > layout.Rows;
            if (outside && !clampToRing) return null;
            if (outside) return new CellCoord(Mathf.Clamp(col, -1, layout.Cols), Mathf.Clamp(row, -1, layout.Rows));
            within = new Vector2((float)point.Col - col, (float)point.Row - row);
            return new CellCoord(col, row);
        }

        private static void DrawMarks(BoardViewController board)
        {
            var boxing = LevelEditorSession.GetSceneMarks(Marks, GrowPlaces, out var boxMin, out var boxMax);
            foreach (var cell in Marks) DrawRect(board, cell, cell, SelectionFill, Color.white);
            foreach (var place in GrowPlaces) DrawPlus(board, place);
            if (LevelEditorSession.GetShapeGhost(Ghost, out var fits))
            {
                var fill = fits ? GhostFits : GhostBlocked;
                foreach (var cell in Ghost) DrawRect(board, cell, cell, fill, fill);
            }
            if (boxing) DrawRect(board, boxMin, boxMax, BoxFill, Color.white);
            if (!_hover.HasValue) return;
            var hoverFill = LevelEditorSession.HoverColor(Event.current.shift);
            DrawRect(board, _hover.Value, _hover.Value, hoverFill, new Color(hoverFill.r, hoverFill.g, hoverFill.b, 0.9f));
        }

        // A green "+" where the picked block or gate can grow.
        private static void DrawPlus(BoardViewController board, CellCoord place)
        {
            DrawRect(board, place, place, GrowFill, GrowFill);
            var centre = (CornerWorld(board, place.Col, place.Row) + CornerWorld(board, place.Col + 1, place.Row + 1)) * 0.5f;
            var arm = board.Layout.CellSize * 0.3f;
            var thickness = 4f;
            using (new Handles.DrawingScope(Color.white))
            {
                Handles.DrawLine(centre + Vector3.left * arm, centre + Vector3.right * arm, thickness);
                Handles.DrawLine(centre + Vector3.back * arm, centre + Vector3.forward * arm, thickness);
            }
        }

        // Outline of the cells from min to max (grid positions, wall ring included) on the pick plane.
        private static void DrawRect(BoardViewController board, CellCoord min, CellCoord max, Color fill, Color outline)
        {
            Quad[0] = CornerWorld(board, min.Col, min.Row);
            Quad[1] = CornerWorld(board, max.Col + 1, min.Row);
            Quad[2] = CornerWorld(board, max.Col + 1, max.Row + 1);
            Quad[3] = CornerWorld(board, min.Col, max.Row + 1);
            Handles.DrawSolidRectangleWithOutline(Quad, fill, outline);
        }

        // World position of a cell corner (corner-origin board coordinates, the inverse of BoardLayout.LocalToBoard).
        private static Vector3 CornerWorld(BoardViewController board, int col, int row)
        {
            var layout = board.Layout;
            var height = LevelEditorScenePreview.Settings.PickPlaneHeight + MarkLift;
            var local = new Vector3((col - layout.Cols * 0.5f) * layout.CellSize, height,
                -(row - layout.Rows * 0.5f) * layout.CellSize);
            return board.transform.TransformPoint(local);
        }
    }
}
