using System;
using System.Collections.Generic;
using System.Linq;
using ColorBlockJam.Core.Level;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Every block colour in one list: change its colour and name, then Save. Favourites are starred in the palette.
    internal sealed class LevelEditorColorsWindow : EditorWindow
    {
        private const float ColorWidth = 70f;

        private readonly Dictionary<BlockColor, Color> _values = new Dictionary<BlockColor, Color>();
        private readonly Dictionary<BlockColor, string> _names = new Dictionary<BlockColor, string>();
        private readonly Dictionary<BlockColor, string> _locked = new Dictionary<BlockColor, string>();
        private Vector2 _scroll;

        public static void Open()
        {
            var window = GetWindow<LevelEditorColorsWindow>(true, "Block Colours");
            window.minSize = new Vector2(320f, 240f);
            window.Reload();
        }

        // Also runs after the recompile a rename causes, so the list shows the new names.
        private void OnEnable() => Reload();

        private static BlockColor[] All => (BlockColor[])Enum.GetValues(typeof(BlockColor));

        private void Reload()
        {
            _values.Clear();
            _names.Clear();
            _locked.Clear();
            foreach (var color in All)
            {
                _values[color] = LevelEditorColors.Of(color);
                _names[color] = color.ToString();
                _locked[color] = LevelEditorColors.CheckRenamable(color);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Names used by code (tests, BoardSettings defaults) can't change.\n" +
                                    "Double-click a colour in the palette to make it a favourite.", MessageType.None);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var color in All) DrawRow(color);
            EditorGUILayout.EndScrollView();

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
                using (new EditorGUI.DisabledScope(_locked[color] != null))
                    _names[color] = EditorGUILayout.TextField(new GUIContent("", _locked[color]), _names[color]);
            }
        }

        private bool HasChanges() =>
            All.Any(c => _values[c] != LevelEditorColors.Of(c) || _names[c].Trim() != c.ToString());

        // Null when every changed name can be used.
        private string Problem()
        {
            var renamed = All.Where(c => _names[c].Trim() != c.ToString()).ToList();
            var finalNames = All.Select(c => _names[c].Trim()).ToList();
            foreach (var color in renamed)
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
            foreach (var color in All.Where(c => _values[c] != LevelEditorColors.Of(c)))
                LevelEditorColors.SetColor(color, _values[color]);

            foreach (var color in All.Where(c => _names[c].Trim() != c.ToString()))
            {
                if (!LevelEditorColors.Rename(color, _names[color].Trim()))
                    Debug.LogWarning($"Level editor: could not rename {color} to {_names[color].Trim()}.");
            }

            LevelEditorSession.Current.RefreshViews();
        }
    }
}
