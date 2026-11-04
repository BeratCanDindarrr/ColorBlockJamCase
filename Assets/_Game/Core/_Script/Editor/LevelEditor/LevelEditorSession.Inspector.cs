using System.Collections.Generic;
using System.Linq;
using ColorBlockJam.Core.Level;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Right panel: fields of one selected block or gate, shared actions for several, and the raw asset data.
    public sealed partial class LevelEditorSession
    {
        private void DrawInspector()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(InspectorWidth));
            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);
            DrawSelectionInspector();

            GUILayout.Space(8f);
            _showRaw = EditorGUILayout.Foldout(_showRaw, "Raw data", true);
            if (_showRaw) DrawRawData();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // What can be done to the selection: ice and axis of blocks, then rotate / mirror / duplicate / delete.
        private void DrawSelectionInspector()
        {
            PruneSelection();
            DropFieldFocusOnNewSelection();
            var blocks = SelectedBlockIndices();
            if (blocks.Count > 0)
            {
                DrawBlockRules(blocks);
                DrawBlockActions();
            }
            else if (selectedGates.Count > 0)
            {
                if (GUILayout.Button("Delete")) DeleteSelection();
            }
            else
            {
                EditorGUILayout.HelpBox("Select a block or gate to edit it.", MessageType.None);
            }
        }

        // The SceneView properties overlay: the palette, then the selection.
        internal void DrawScenePanel(int swatchesPerRow, float width)
        {
            if (EnsureWorking() == null) return;
            DrawPalette(swatchesPerRow, width);
            GUILayout.Space(6f);
            DrawSelectionInspector();
        }

        private string _inspectedSelection;

        // A field being typed in keeps its text in IMGUI; a new selection must not take it over, so focus is dropped.
        private void DropFieldFocusOnNewSelection()
        {
            var key = string.Join(",", selectedBlocks) + "|" + string.Join(",", selectedGates.Select(g => $"{g.Edge}{g.Index}:{g.Depth}"));
            if (key == _inspectedSelection) return;
            _inspectedSelection = key;
            GUIUtility.keyboardControl = 0;
        }

        // Ice and move axis of the given blocks; a field shows "—" while they differ and sets all of them when changed.
        private void DrawBlockRules(List<int> blocks)
        {
            var first = working.Blocks[blocks[0]];

            EditorGUI.showMixedValue = blocks.Any(i => working.Blocks[i].Ice != first.Ice);
            EditorGUI.BeginChangeCheck();
            var ice = Mathf.Max(0, EditorGUILayout.IntField(
                new GUIContent("Ice", "Frozen until this many blocks have left through gates; 0 = no ice"), first.Ice));
            if (EditorGUI.EndChangeCheck()) Edit("Set Ice", l => blocks.Aggregate(false, (any, i) => Set(ref l.Blocks[i].Ice, ice) | any));

            EditorGUI.showMixedValue = blocks.Any(i => working.Blocks[i].Axis != first.Axis);
            EditorGUI.BeginChangeCheck();
            var axis = (BlockAxis)EditorGUILayout.EnumPopup(
                new GUIContent("Axis", "Free moves anywhere; Horizontal only along a row, Vertical only along a column"), first.Axis);
            if (EditorGUI.EndChangeCheck()) Edit("Set Axis", l => blocks.Aggregate(false, (any, i) => Set(ref l.Blocks[i].Axis, axis) | any));
            EditorGUI.showMixedValue = false;
        }

        private void DrawBlockActions()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rotate")) RotateSelection();
            if (GUILayout.Button("Mirror")) MirrorSelection();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Duplicate")) DuplicateSelection();
            if (GUILayout.Button("Delete")) DeleteSelection();
            EditorGUILayout.EndHorizontal();
        }

        // Plain Unity inspector of the working copy (knit_Out drew this with Odin's PropertyTree).
        private void DrawRawData()
        {
            if (_rawEditor == null || _rawEditor.target != working) _rawEditor = UnityEditor.Editor.CreateEditor(working);

            EditorGUI.BeginChangeCheck();
            _rawEditor.OnInspectorGUI();
            if (!EditorGUI.EndChangeCheck()) return;

            PruneSelection();
            RefreshIssues();
            NotifyWorkingChanged();
        }

        private void DestroyRawEditor()
        {
            if (_rawEditor != null) DestroyImmediate(_rawEditor);
            _rawEditor = null;
        }

        private static bool Set<T>(ref T field, T value)
        {
            if (Equals(field, value)) return false;
            field = value;
            return true;
        }
    }
}
