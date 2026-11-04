using System;
using System.Collections.Generic;
using System.Linq;
using ColorBlockJam.Core.Level;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Every block colour in one list: change its colour and name, add a new one, then Save. A rename also updates the
    // scripts that name the colour (asked first); levels keep their colours. Favourites are starred in the palette.
    internal sealed class LevelEditorColorsWindow : EditorWindow
    {
        private const float ColorWidth = 70f;

        private readonly Dictionary<BlockColor, Color> _values = new Dictionary<BlockColor, Color>();
        private readonly Dictionary<BlockColor, string> _names = new Dictionary<BlockColor, string>();
        private Vector2 _scroll;

        public static void Open()
        {
            var window = GetWindow<LevelEditorColorsWindow>(true, "Block Colours");
            window.minSize = new Vector2(320f, 260f);
            window.Reload();
        }

        // Also runs after the recompile a rename or a new colour causes, so the list shows them.
        private void OnEnable() => Reload();

        private static BlockColor[] All => (BlockColor[])Enum.GetValues(typeof(BlockColor));

        private void Reload()
        {
            _values.Clear();
            _names.Clear();
            foreach (var color in All)
            {
                _values[color] = LevelEditorColors.Of(color);
                _names[color] = color.ToString();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Renaming a colour also updates the scripts that name it; levels keep their colours.\n" +
                                    "Double-click a colour in the palette to make it a favourite.", MessageType.None);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var color in All) DrawRow(color);
            EditorGUILayout.EndScrollView();

            var addRect = GUILayoutUtility.GetRect(new GUIContent("+ Add colour"), GUI.skin.button);
            if (GUI.Button(addRect, new GUIContent("+ Add colour", "A new block colour (Unity recompiles)")))
                UnityEditor.PopupWindow.Show(addRect, new AddColorPopup());

            var problem = Problem();
            if (problem != null) EditorGUILayout.HelpBox(problem, MessageType.Warning);

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Revert")) Reload();
                using (new EditorGUI.DisabledScope(problem != null || !HasChanges()))
                {
                    if (GUILayout.Button("Save")) Save();
                }
            }
        }

        private void DrawRow(BlockColor color)
        {
            if (!_values.ContainsKey(color)) Reload();
            using (new GUILayout.HorizontalScope())
            {
                _values[color] = EditorGUILayout.ColorField(GUIContent.none, _values[color], GUILayout.Width(ColorWidth));
                _names[color] = EditorGUILayout.TextField(_names[color]);
            }
        }

        private bool HasChanges() =>
            All.Any(c => _values[c] != LevelEditorColors.Of(c) || _names[c].Trim() != c.ToString());

        private List<BlockColor> Renamed() => All.Where(c => _names[c].Trim() != c.ToString()).ToList();

        // Null when every changed name can be used.
        private string Problem()
        {
            var finalNames = All.Select(c => _names[c].Trim()).ToList();
            foreach (var color in Renamed())
            {
                var name = _names[color].Trim();
                if (finalNames.Count(n => n == name) > 1) return $"{name} is used twice.";
                // A name in use now is refused even when its colour is renamed in the same Save.
                var problem = LevelEditorColors.CheckName(name);
                if (problem != null) return $"{color}: {problem}";
            }

            return null;
        }

        private void Save()
        {
            var renamed = Renamed();
            if (!ConfirmScriptChanges(renamed)) return;

            foreach (var color in All.Where(c => _values[c] != LevelEditorColors.Of(c)))
                LevelEditorColors.SetColor(color, _values[color]);

            foreach (var color in renamed)
            {
                if (!LevelEditorColors.Rename(color, _names[color].Trim()))
                    Debug.LogWarning($"Level editor: could not rename {color} to {_names[color].Trim()}.");
            }

            if (renamed.Count > 0) AssetDatabase.Refresh();
            LevelEditorSession.Current.RefreshViews();
        }

        // Renaming rewrites other scripts (tests, BoardSettings defaults): say which ones and ask before touching them.
        private bool ConfirmScriptChanges(List<BlockColor> renamed)
        {
            var users = renamed.SelectMany(LevelEditorColors.UsersOf).Distinct().ToList();
            if (users.Count == 0) return true;

            var names = string.Join(", ", renamed.Select(c => $"{c} → {_names[c].Trim()}"));
            var files = string.Join("\n", users.Select(System.IO.Path.GetFileName));
            return EditorUtility.DisplayDialog("Rename colours",
                $"{names}\n\nThese scripts name the colour(s) and will be updated too:\n{files}\n\nUnity recompiles afterwards.",
                "Rename", "Cancel");
        }
    }
}
