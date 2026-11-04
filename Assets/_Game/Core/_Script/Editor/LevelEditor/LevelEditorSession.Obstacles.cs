using System.Collections.Generic;
using System.Linq;
using ColorBlockJam.Core.Level;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Obstacles of the selected blocks in the properties panel: only the ones a block has are listed (each with its
    // setting and a × to take it off), and "Add Obstacle" puts a new one on. With several blocks picked, a row shows
    // when any of them has it, "—" where they differ, and every change goes to all of them.
    public sealed partial class LevelEditorSession
    {
        private const int NewIce = 1;
        private const BlockAxis NewAxis = BlockAxis.Horizontal;
        private const float RemoveWidth = 22f;

        private void DrawObstacles(List<int> blocks)
        {
            var hasIce = blocks.Any(i => working.Blocks[i].Ice > 0);
            var hasAxis = blocks.Any(i => working.Blocks[i].Axis != BlockAxis.Free);

            if (hasIce || hasAxis) GUILayout.Label("Obstacles", EditorStyles.miniBoldLabel);
            if (hasIce) DrawIceRow(blocks);
            if (hasAxis) DrawAxisRow(blocks);

            if (hasIce && hasAxis) return;
            var rect = GUILayoutUtility.GetRect(new GUIContent("+ Add Obstacle"), EditorStyles.miniButton);
            if (GUI.Button(rect, new GUIContent("+ Add Obstacle", "Put ice or a one-way axis on the selected blocks"),
                    EditorStyles.miniButton))
                ObstacleMenu(blocks, hasIce, hasAxis).DropDown(rect);
        }

        private GenericMenu ObstacleMenu(List<int> blocks, bool hasIce, bool hasAxis)
        {
            var menu = new GenericMenu();
            AddMenuItem(menu, "Ice", !hasIce, () => SetIce(blocks, NewIce, "Add Ice"));
            AddMenuItem(menu, "Axis (one way)", !hasAxis, () => SetAxis(blocks, NewAxis, "Add Axis"));
            return menu;
        }

        private static void AddMenuItem(GenericMenu menu, string label, bool enabled, GenericMenu.MenuFunction action)
        {
            if (enabled) menu.AddItem(new GUIContent(label), false, action);
            else menu.AddDisabledItem(new GUIContent(label));
        }

        private void DrawIceRow(List<int> blocks)
        {
            var iced = blocks.Where(i => working.Blocks[i].Ice > 0).ToList();
            var first = working.Blocks[iced[0]].Ice;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.showMixedValue = iced.Count < blocks.Count || iced.Any(i => working.Blocks[i].Ice != first);
            EditorGUI.BeginChangeCheck();
            var ice = Mathf.Max(1, EditorGUILayout.IntField(
                new GUIContent("Ice", "Frozen until this many blocks have left through gates"), first));
            if (EditorGUI.EndChangeCheck()) SetIce(blocks, ice, "Set Ice");
            EditorGUI.showMixedValue = false;
            if (GUILayout.Button(new GUIContent("×", "Remove ice"), EditorStyles.miniButton, GUILayout.Width(RemoveWidth)))
                SetIce(blocks, 0, "Remove Ice");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAxisRow(List<int> blocks)
        {
            var oneWay = blocks.Where(i => working.Blocks[i].Axis != BlockAxis.Free).ToList();
            var first = working.Blocks[oneWay[0]].Axis;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.showMixedValue = oneWay.Count < blocks.Count || oneWay.Any(i => working.Blocks[i].Axis != first);
            EditorGUI.BeginChangeCheck();
            var horizontal = EditorGUILayout.Popup(
                new GUIContent("Axis", "Horizontal moves only along a row, Vertical only along a column"),
                first == BlockAxis.Vertical ? 1 : 0, AxisNames) == 0;
            if (EditorGUI.EndChangeCheck()) SetAxis(blocks, horizontal ? BlockAxis.Horizontal : BlockAxis.Vertical, "Set Axis");
            EditorGUI.showMixedValue = false;
            if (GUILayout.Button(new GUIContent("×", "Remove the axis (moves freely again)"), EditorStyles.miniButton,
                    GUILayout.Width(RemoveWidth)))
                SetAxis(blocks, BlockAxis.Free, "Remove Axis");
            EditorGUILayout.EndHorizontal();
        }

        // Ice of the selected gates: a gate with ice takes no block until that many blocks have left the board.
        private void DrawGateObstacles(List<int> gates)
        {
            var iced = gates.Where(i => working.Gates[i].Ice > 0).ToList();
            if (iced.Count == 0)
            {
                var rect = GUILayoutUtility.GetRect(new GUIContent("+ Add Obstacle"), EditorStyles.miniButton);
                if (GUI.Button(rect, new GUIContent("+ Add Obstacle", "Put ice on the selected gates"), EditorStyles.miniButton))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Ice"), false, () => SetGateIce(gates, NewIce, "Add Gate Ice"));
                    menu.DropDown(rect);
                }

                return;
            }

            GUILayout.Label("Gate obstacles", EditorStyles.miniBoldLabel);
            var first = working.Gates[iced[0]].Ice;
            EditorGUILayout.BeginHorizontal();
            EditorGUI.showMixedValue = iced.Count < gates.Count || iced.Any(i => working.Gates[i].Ice != first);
            EditorGUI.BeginChangeCheck();
            var ice = Mathf.Max(1, EditorGUILayout.IntField(
                new GUIContent("Ice", "Takes no block until this many blocks have left the board"), first));
            if (EditorGUI.EndChangeCheck()) SetGateIce(gates, ice, "Set Gate Ice");
            EditorGUI.showMixedValue = false;
            if (GUILayout.Button(new GUIContent("×", "Remove the ice"), EditorStyles.miniButton, GUILayout.Width(RemoveWidth)))
                SetGateIce(gates, 0, "Remove Gate Ice");
            EditorGUILayout.EndHorizontal();
        }

        private void SetGateIce(List<int> gates, int ice, string undoName) =>
            Edit(undoName, l => gates.Aggregate(false, (any, i) => Set(ref l.Gates[i].Ice, ice) | any));

        private static readonly string[] AxisNames = { "Horizontal", "Vertical" };

        private void SetIce(List<int> blocks, int ice, string undoName) =>
            Edit(undoName, l => blocks.Aggregate(false, (any, i) => Set(ref l.Blocks[i].Ice, ice) | any));

        private void SetAxis(List<int> blocks, BlockAxis axis, string undoName) =>
            Edit(undoName, l => blocks.Aggregate(false, (any, i) => Set(ref l.Blocks[i].Axis, axis) | any));
    }
}
