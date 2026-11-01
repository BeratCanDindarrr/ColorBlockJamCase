using System;
using System.Linq;
using ColorBlockJam.Core.Level;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Layout: header bar, then tools | board | inspector, then the problem list.
    public sealed partial class LevelEditorWindow
    {
        private const float SideWidth = 200f;
        private const float InspectorWidth = 260f;
        private const float StatusHeight = 120f;
        private const float SwatchSize = 26f;

        private static readonly string[] ToolNames = { "1 Select", "2 Block", "3 Gate", "4 Cell" };

        private Vector2 _statusScroll;
        private Vector2 _inspectorScroll;
        private bool _showRaw;
        private UnityEditor.Editor _rawEditor;

        private void OnGUI()
        {
            if (working == null)
            {
                DrawNoLevel();
                return;
            }

            HandleShortcuts();
            DrawHeader();

            EditorGUILayout.BeginHorizontal();
            DrawToolPanel();
            var boardRect = GUILayoutUtility.GetRect(10f, 10f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawBoard(boardRect);
            DrawInspector();
            EditorGUILayout.EndHorizontal();

            DrawStatus();
        }

        private void DrawNoLevel()
        {
            EditorGUILayout.HelpBox("No LevelData asset found.", MessageType.Info);
            if (GUILayout.Button("New Level", GUILayout.Width(160f))) CreateLevel();
        }

        private void DrawHeader()
        {
            var levels = LevelEditorAssets.FindLevels();
            var current = levels.IndexOf(source);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            using (new EditorGUI.DisabledScope(current <= 0))
            {
                if (GUILayout.Button("‹", EditorStyles.toolbarButton, GUILayout.Width(24f))) TryOpen(levels[current - 1]);
            }

            var names = levels.Select(l => l.name).ToArray();
            var picked = EditorGUILayout.Popup(current, names, EditorStyles.toolbarPopup, GUILayout.Width(160f));
            if (picked != current && picked >= 0) TryOpen(levels[picked]);

            using (new EditorGUI.DisabledScope(current < 0 || current >= levels.Count - 1))
            {
                if (GUILayout.Button("›", EditorStyles.toolbarButton, GUILayout.Width(24f))) TryOpen(levels[current + 1]);
            }

            var playIndex = LevelEditorAssets.PlayIndexOf(source);
            GUILayout.Label(playIndex >= 0 ? $"plays as level {playIndex + 1}" : "not in LevelSettings", EditorStyles.miniLabel);
            if (IsDirty) GUILayout.Label("● unsaved", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(!IsDirty))
            {
                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50f))) Save();
                if (GUILayout.Button("Revert", EditorStyles.toolbarButton, GUILayout.Width(55f))) Revert();
            }

            if (GUILayout.Button("New Level", EditorStyles.toolbarButton, GUILayout.Width(75f))) CreateLevel();
            if (GUILayout.Button("Check All", EditorStyles.toolbarButton, GUILayout.Width(70f))) LevelEditorAssets.CheckAllLevels();
            if (GUILayout.Button("▶ Play", EditorStyles.toolbarButton, GUILayout.Width(60f))) PlayLevel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(SideWidth));
            GUILayout.Label("Tool", EditorStyles.boldLabel);
            CurrentTool = (LevelEditorTool)GUILayout.SelectionGrid((int)tool, ToolNames, 1);

            GUILayout.Space(8f);
            GUILayout.Label("Colour", EditorStyles.boldLabel);
            DrawSwatches();

            GUILayout.Space(8f);
            GUILayout.Label("Level", EditorStyles.boldLabel);
            DrawLevelFields();

            GUILayout.Space(8f);
            EditorGUILayout.HelpBox("Select: Shift/Ctrl+click adds,\ndrag on empty = box\nBlock/Gate: Shift erases\n" +
                                    "Cell: click/drag removes or\nrestores cells, Shift restores\n" +
                                    "Del: delete   R: rotate\nCtrl+D: duplicate\nCtrl+C / Ctrl+V: copy, paste\n" +
                                    "Ctrl+S: save   Esc: deselect", MessageType.None);
            EditorGUILayout.EndVertical();
        }

        // Picks the brush colour; with a block or gate selected it also recolours it.
        private void DrawSwatches()
        {
            var colors = (BlockColor[])Enum.GetValues(typeof(BlockColor));
            const int perRow = 6;
            for (var i = 0; i < colors.Length; i += perRow)
            {
                var row = GUILayoutUtility.GetRect(SideWidth, SwatchSize + 4f);
                for (var j = 0; j < perRow && i + j < colors.Length; j++)
                {
                    var color = colors[i + j];
                    var rect = new Rect(row.x + j * (SwatchSize + 4f), row.y, SwatchSize, SwatchSize);
                    if (color == brush) EditorGUI.DrawRect(rect, LevelEditorStyle.Selection);
                    EditorGUI.DrawRect(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), LevelEditorStyle.Of(color));
                    if (GUI.Button(rect, new GUIContent("", color.ToString()), GUIStyle.none)) PickColor(color);
                }
            }

            GUILayout.Label(brush.ToString(), EditorStyles.miniLabel);
        }

        private void PickColor(BlockColor color)
        {
            brush = color;
            RaiseStateChanged();
            var blocks = SelectedBlockIndices();
            var gates = SelectedGateIndices();
            if (blocks.Count + gates.Count == 0) return;

            Edit("Recolour Selection", l =>
            {
                var changed = false;
                foreach (var i in blocks) changed |= Set(ref l.Blocks[i].Color, color);
                foreach (var i in gates) changed |= Set(ref l.Gates[i].Color, color);
                return changed;
            });
        }

        private void DrawLevelFields()
        {
            var cols = EditorGUILayout.DelayedIntField("Cols", working.Cols);
            var rows = EditorGUILayout.DelayedIntField("Rows", working.Rows);
            if (cols != working.Cols || rows != working.Rows)
            {
                Edit("Resize Board", l =>
                {
                    l.Cols = Mathf.Max(1, cols);
                    l.Rows = Mathf.Max(1, rows);
                    LevelEditorBlockOps.PullInside(l);
                    LevelEditorGateOps.ClampAll(l);
                    return true;
                });
            }

            var time = EditorGUILayout.DelayedFloatField("Time (sec)", working.TimeLimitSec);
            if (!Mathf.Approximately(time, working.TimeLimitSec))
            {
                Edit("Set Time Limit", l =>
                {
                    l.TimeLimitSec = Mathf.Max(0f, time);
                    return true;
                });
            }
        }

        private void DrawBoard(Rect rect)
        {
            EditorGUI.DrawRect(rect, LevelEditorStyle.Background);
            _board.Layout(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f), working);
            if (Event.current.type == EventType.Repaint)
            {
                _board.Draw(working, IsBlockSelected, IsGateSelected, _errorBlocks, _errorGates, _hover, GrowPlaces());
                if (_boxing) _board.DrawBox(BoxMin, BoxMax);
            }
            HandleBoardInput(rect);
        }

        private void DrawStatus()
        {
            var errors = _issues.Count(i => i.IsError);
            var warnings = _issues.Count - errors;
            EditorGUILayout.BeginVertical(GUILayout.Height(StatusHeight));
            GUILayout.Label($"Blocks {working.Blocks.Length}   Gates {working.Gates.Length}   " +
                            $"Errors {errors}   Warnings {warnings}", EditorStyles.boldLabel);
            _statusScroll = EditorGUILayout.BeginScrollView(_statusScroll);
            if (_issues.Count == 0) GUILayout.Label("No problems.", EditorStyles.miniLabel);
            foreach (var issue in _issues)
            {
                EditorGUILayout.BeginHorizontal();
                var type = issue.IsError ? MessageType.Error : MessageType.Warning;
                EditorGUILayout.HelpBox(issue.Message, type);
                if ((issue.BlockIndex >= 0 || issue.GateIndex >= 0) && GUILayout.Button("Select", GUILayout.Width(60f)))
                    SelectOnly(issue.BlockIndex, issue.GateIndex);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }
}
