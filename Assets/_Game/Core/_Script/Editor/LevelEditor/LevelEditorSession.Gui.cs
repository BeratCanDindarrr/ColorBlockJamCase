using System;
using System.Collections.Generic;
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
        private const float PanelButtonWidth = 30f;
        private const float PanelButtonHeight = 20f;
        private const string FavoritesFoldKey = "ColorBlockJam.LevelEditor.PaletteFavoritesOpen";
        private const string AllColorsFoldKey = "ColorBlockJam.LevelEditor.PaletteAllOpen";

        private static readonly string[] ToolNames = { "1 Select", "2 Block", "3 Gate", "4 Cell" };
        private static readonly string[] ModeNames = { "+ Add", "− Erase" };
        private bool _lastShift;
        private readonly List<CellCoord> _growMarks = new List<CellCoord>();
        private readonly List<CellCoord> _shrinkMarks = new List<CellCoord>();

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

            LevelEditorLevelPicker.Draw(levels, current, l => TryOpen(l), EditorStyles.toolbarPopup, 160f);

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

        // The SceneView level bar: which level and save / play; its size, time and problems; switches for the other panels.
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

            LevelEditorLevelPicker.Draw(levels, current, l => TryOpen(l), EditorStyles.popup, 130f);

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

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Panels", EditorStyles.miniBoldLabel, GUILayout.Width(BarLabelWidth));
            DrawPanelToggle(LevelEditorSceneOverlays.PanelId, "d_UnityEditor.InspectorWindow",
                "Properties\nColours and the selected blocks or gates");
            DrawPanelToggle(LevelEditorSceneOverlays.ShapesId, "d_Prefab Icon", "Shapes\nSaved block shapes to drag onto the board");
            DrawPanelToggle(LevelEditorSceneOverlays.LevelOrderId, "d_VerticalLayoutGroup Icon",
                "Level Order\nThe order the game plays the levels in, and the boosters' unlock levels, pictures and texts");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // Opens or closes one of the Scene view panels: a small icon button, lit while the panel is open.
        private static void DrawPanelToggle(string overlayId, string iconName, string tip)
        {
            var shown = LevelEditorSceneOverlays.IsShown(overlayId);
            var content = new GUIContent(EditorGUIUtility.IconContent(iconName).image, tip);
            var now = GUILayout.Toggle(shown, content, EditorStyles.miniButton, GUILayout.Width(PanelButtonWidth),
                GUILayout.Height(PanelButtonHeight));
            if (now != shown) LevelEditorSceneOverlays.SetShown(overlayId, now);
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
            if (HasModes) EraseMode = GUILayout.Toolbar(EraseMode ? 1 : 0, ModeNames) == 1;

            GUILayout.Space(8f);
            GUILayout.Label("Colour", EditorStyles.boldLabel);
            DrawPalette();

            GUILayout.Space(8f);
            GUILayout.Label("Level", EditorStyles.boldLabel);
            DrawLevelFields();

            GUILayout.Space(8f);
            EditorGUILayout.HelpBox("Select: Shift/Ctrl+click adds,\ndrag on empty = box\nBlock/Gate/Cell: + adds, − erases\n" +
                                    "(red = will be erased),\nShift flips + / −\n" +
                                    "Del: delete   R: rotate\nCtrl+D: duplicate\nCtrl+C / Ctrl+V: copy, paste\n" +
                                    "Ctrl+S: save   Esc: deselect", MessageType.None);
            EditorGUILayout.EndVertical();
        }

        // Picks the brush colour; with a block or gate selected it also recolours it. A tools row (eyedropper, "+" adds a
        // colour, pencil edits them), then two lists that fold away: the favourite colours and all colours.
        private void DrawPalette(int perRow = 6, float width = SideWidth)
        {
            DrawPaletteTools();
            var all = (BlockColor[])Enum.GetValues(typeof(BlockColor));
            var favorites = all.Where(LevelEditorColors.IsFavorite).ToArray();

            if (DrawColorFold(FavoritesFoldKey, new GUIContent($"Favourites ({favorites.Length})",
                    "Double-click a colour to add it to or remove it from favourites")))
            {
                if (favorites.Length == 0) GUILayout.Label("None yet", EditorStyles.centeredGreyMiniLabel);
                DrawSwatchGrid(favorites, perRow, width);
            }

            if (DrawColorFold(AllColorsFoldKey, new GUIContent($"All colours ({all.Length})"))) DrawSwatchGrid(all, perRow, width);

            GUILayout.Label($"Brush: {brush}", EditorStyles.miniBoldLabel);
        }

        private void DrawPaletteTools()
        {
            var row = GUILayoutUtility.GetRect(SwatchSize * 3f + 8f, SwatchSize + 4f, GUILayout.ExpandWidth(false));
            var rect = new Rect(row.x, row.y, SwatchSize, SwatchSize);

            if (_pickingColor) EditorGUI.DrawRect(rect, LevelEditorStyle.Selection);
            var eyedropper = new GUIContent(EditorGUIUtility.IconContent("d_eyeDropper.Large").image,
                "Eyedropper: click a block or gate to copy its colour (Esc cancels)");
            if (GUI.Button(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), eyedropper))
                ToggleEyedropper();

            rect.x += SwatchSize + 4f;
            if (GUI.Button(rect, new GUIContent("+", "Add a colour"))) PopupWindow.Show(rect, new AddColorPopup());

            rect.x += SwatchSize + 4f;
            if (GUI.Button(rect, new GUIContent(EditorGUIUtility.IconContent("d_editicon.sml").image,
                    "Edit colours and favourites"))) LevelEditorColorsWindow.Open();
        }

        // A folding header whose open state is kept per user.
        private static bool DrawColorFold(string key, GUIContent label)
        {
            var open = EditorPrefs.GetBool(key, true);
            var now = EditorGUILayout.Foldout(open, label, true);
            if (now != open) EditorPrefs.SetBool(key, now);
            return now;
        }

        private void DrawSwatchGrid(BlockColor[] colors, int perRow, float width)
        {
            for (var i = 0; i < colors.Length; i += perRow)
            {
                var row = GUILayoutUtility.GetRect(width, SwatchSize + 4f);
                for (var j = 0; j < perRow && i + j < colors.Length; j++)
                    DrawSwatch(new Rect(row.x + j * (SwatchSize + 4f), row.y, SwatchSize, SwatchSize), colors[i + j]);
            }
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
            // The hover tint follows Shift, so a Shift press or release redraws the board.
            if (Event.current.shift != _lastShift)
            {
                _lastShift = Event.current.shift;
                Repaint();
            }

            _board.Layout(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f), working);
            if (Event.current.type == EventType.Repaint)
            {
                MarkPlaces(Event.current.shift, _growMarks, _shrinkMarks);
                _board.Draw(working, IsBlockSelected, IsGateSelected, _errorBlocks, _errorGates, _hover, _growMarks, _shrinkMarks,
                    HoverColor(Event.current.shift));
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
