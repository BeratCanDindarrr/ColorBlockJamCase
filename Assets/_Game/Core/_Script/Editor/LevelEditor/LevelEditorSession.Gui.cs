using System;
using System.Linq;
using ColorBlockJam.Core.Level;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Layout: header bar, then tools | board | inspector, then the problem list.
    public sealed partial class LevelEditorSession
    {
        private const float SideWidth = 200f;
        private const float InspectorWidth = 260f;
        private const float StatusHeight = 120f;
        private const float SwatchSize = 26f;
        // The SceneView level bar.
        internal const float BarWidth = 620f;
        private const float BarLabelWidth = 62f;
        private const float BarFieldWidth = 105f;
        private const int BarSwatchesPerRow = 20;

        private static readonly string[] ToolNames = { "1 Select", "2 Block", "3 Gate", "4 Cell" };

        private Vector2 _statusScroll;
        private Vector2 _inspectorScroll;
        private bool _showRaw;
        private UnityEditor.Editor _rawEditor;

        internal void DrawWindow()
        {
            if (EnsureWorking() == null)
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

        // The SceneView level bar, three short rows: which level and save / play; its size, time and problems; the palette.
        internal void DrawLevelBar()
        {
            if (EnsureWorking() == null) return;
            var levels = LevelEditorAssets.FindLevels();
            var current = levels.IndexOf(source);

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(current <= 0))
            {
                if (GUILayout.Button("‹", EditorStyles.miniButtonLeft, GUILayout.Width(22f))) TryOpen(levels[current - 1]);
            }

            var picked = EditorGUILayout.Popup(current, levels.Select(l => l.name).ToArray(), GUILayout.Width(130f));
            if (picked != current && picked >= 0) TryOpen(levels[picked]);

            using (new EditorGUI.DisabledScope(current < 0 || current >= levels.Count - 1))
            {
                if (GUILayout.Button("›", EditorStyles.miniButtonRight, GUILayout.Width(22f))) TryOpen(levels[current + 1]);
            }

            var playIndex = LevelEditorAssets.PlayIndexOf(source);
            var state = playIndex >= 0 ? $"Level {playIndex + 1}" : "Not in LevelSettings";
            GUILayout.Label(IsDirty ? state + "   ● unsaved" : state, EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(!IsDirty))
            {
                if (GUILayout.Button("Save", EditorStyles.miniButtonLeft, GUILayout.Width(50f))) Save();
                if (GUILayout.Button("Revert", EditorStyles.miniButtonMid, GUILayout.Width(55f))) Revert();
            }

            if (GUILayout.Button("New", EditorStyles.miniButtonMid, GUILayout.Width(45f))) CreateLevel();
            if (GUILayout.Button("▶ Play", EditorStyles.miniButtonRight, GUILayout.Width(55f))) PlayLevel();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = BarLabelWidth;
            DrawLevelFields(GUILayout.Width(BarFieldWidth));
            EditorGUIUtility.labelWidth = labelWidth;
            GUILayout.Space(8f);
            DrawProblemsLine();
            EditorGUILayout.EndHorizontal();

            DrawSwatches(BarSwatchesPerRow, BarWidth);
        }

        // One line: the first error (or warning) and the counts; the tooltip lists them all.
        private void DrawProblemsLine()
        {
            var errors = _issues.Count(i => i.IsError);
            var warnings = _issues.Count - errors;
            if (errors + warnings == 0)
            {
                GUILayout.Label("No problems", EditorStyles.miniLabel);
                return;
            }

            var first = errors > 0 ? _issues.First(i => i.IsError) : _issues[0];
            var icon = EditorGUIUtility.IconContent(errors > 0 ? "console.erroricon.sml" : "console.warnicon.sml").image;
            var all = string.Join("\n", _issues.Select(i => i.Message));
            GUILayout.Label(new GUIContent($"{errors} error(s), {warnings} warning(s): {first.Message}", icon, all),
                EditorStyles.miniLabel, GUILayout.MinWidth(0f));
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
        // The favourite colours (all of them after "…", or when none is starred; a double click stars one), then "+" adds
        // a colour, the pencil opens the colour list and "…" shows the rest.
        private void DrawSwatches(int perRow = 6, float width = SideWidth)
        {
            var all = (BlockColor[])Enum.GetValues(typeof(BlockColor));
            var favorites = all.Where(LevelEditorColors.IsFavorite).ToArray();
            var hasMore = favorites.Length > 0 && favorites.Length < all.Length;
            var colors = hasMore && !_showAllColors ? favorites : all;
            // After the colours: eyedropper, "+", pencil, and "…" when some colours are hidden.
            var slots = colors.Length + (hasMore ? 4 : 3);
            for (var i = 0; i < slots; i += perRow)
            {
                var row = GUILayoutUtility.GetRect(width, SwatchSize + 4f);
                for (var j = 0; j < perRow && i + j < slots; j++)
                {
                    var rect = new Rect(row.x + j * (SwatchSize + 4f), row.y, SwatchSize, SwatchSize);
                    var slot = i + j - colors.Length - 1;
                    if (slot == -1)
                    {
                        if (_pickingColor) EditorGUI.DrawRect(rect, LevelEditorStyle.Selection);
                        var eyedropper = new GUIContent(EditorGUIUtility.IconContent("d_eyeDropper.Large").image,
                            "Eyedropper: click a block or gate to copy its colour (Esc cancels)");
                        if (GUI.Button(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), eyedropper))
                            ToggleEyedropper();
                    }
                    else if (slot == 0)
                    {
                        if (GUI.Button(rect, new GUIContent("+", "Add a colour"))) PopupWindow.Show(rect, new AddColorPopup());
                    }
                    else if (slot == 1)
                    {
                        if (GUI.Button(rect, new GUIContent(EditorGUIUtility.IconContent("d_editicon.sml").image,
                                "Edit colours and favourites"))) LevelEditorColorsWindow.Open();
                    }
                    else if (slot == 2)
                    {
                        if (GUI.Button(rect, new GUIContent(_showAllColors ? "▴" : "…",
                                _showAllColors ? "Show only favourites" : "Show all colours")))
                            _showAllColors = !_showAllColors;
                    }
                    else
                    {
                        DrawSwatch(rect, colors[i + j]);
                    }
                }
            }

            GUILayout.Label(brush.ToString(), EditorStyles.miniLabel);
        }

        // A click picks the colour, a double click stars or unstars it as a favourite.
        private void DrawSwatch(Rect rect, BlockColor color)
        {
            var favorite = LevelEditorColors.IsFavorite(color);
            if (color == brush) EditorGUI.DrawRect(rect, LevelEditorStyle.Selection);
            EditorGUI.DrawRect(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), LevelEditorStyle.Of(color));
            if (favorite) GUI.Label(new Rect(rect.xMax - 13f, rect.y, 13f, 13f), "★", EditorStyles.miniBoldLabel);

            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && e.clickCount == 2 && rect.Contains(e.mousePosition))
            {
                LevelEditorColors.SetFavorite(color, !favorite);
                e.Use();
                RaiseStateChanged();
                return;
            }

            var tip = $"{color}\nDouble-click: {(favorite ? "remove from" : "add to")} favourites";
            if (GUI.Button(rect, new GUIContent("", tip), GUIStyle.none)) PickColor(color);
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

        private void DrawLevelFields(params GUILayoutOption[] options)
        {
            var cols = EditorGUILayout.DelayedIntField("Cols", working.Cols, options);
            var rows = EditorGUILayout.DelayedIntField("Rows", working.Rows, options);
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

            var time = EditorGUILayout.DelayedFloatField("Time (sec)", working.TimeLimitSec, options);
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
                if (_ghost.Count > 0) _board.DrawGhost(_ghost, _ghostFits);
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
