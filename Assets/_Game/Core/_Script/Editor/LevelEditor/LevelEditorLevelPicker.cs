using System;
using System.Collections.Generic;
using ColorBlockJam.Core.Settings;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // The level list with a search field (Unity's AdvancedDropdown filters by name as you type).
    internal sealed class LevelEditorLevelPicker : AdvancedDropdown
    {
        private static readonly Vector2 MinSize = new Vector2(220f, 320f);

        private readonly List<LevelData> _levels;
        private readonly Action<LevelData> _picked;

        private LevelEditorLevelPicker(List<LevelData> levels, Action<LevelData> picked) : base(new AdvancedDropdownState())
        {
            _levels = levels;
            _picked = picked;
            minimumSize = MinSize;
        }

        // A popup-looking button showing the current level; pressing it opens the searchable list.
        public static void Draw(List<LevelData> levels, int current, Action<LevelData> picked, GUIStyle style, float width)
        {
            var label = new GUIContent(current >= 0 ? levels[current].name : "Pick a level");
            var rect = GUILayoutUtility.GetRect(label, style, GUILayout.Width(width));
            if (EditorGUI.DropdownButton(rect, label, FocusType.Passive, style))
                new LevelEditorLevelPicker(levels, picked).Show(rect);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Levels");
            for (var i = 0; i < _levels.Count; i++) root.AddChild(new AdvancedDropdownItem(_levels[i].name) { id = i });
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item.id >= 0 && item.id < _levels.Count) _picked(_levels[item.id]);
        }
    }
}
