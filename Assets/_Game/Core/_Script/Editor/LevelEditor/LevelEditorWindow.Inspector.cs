using ColorBlockJam.Core.Level;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Right panel: fields of one selected block or gate, shared actions for several, and the raw asset data.
    public sealed partial class LevelEditorWindow
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

        private void DrawSelectionInspector()
        {
            PruneSelection();
            var count = selectedBlocks.Count + selectedGates.Count;
            if (count > 1) DrawManyInspector();
            else if (PrimaryBlock >= 0) DrawBlockInspector(PrimaryBlock);
            else if (PrimaryGate >= 0) DrawGateInspector(PrimaryGate);
            else EditorGUILayout.HelpBox("Nothing selected.\nSelect tool: click a block or gate, or drag a box.", MessageType.None);
        }

        // The SceneView properties overlay: the colour palette (brush, and recolours what is selected) and the
        // selected block or gate.
        internal void DrawScenePanel()
        {
            if (working == null) return;
            GUILayout.Label("Colour", EditorStyles.boldLabel);
            DrawSwatches();
            GUILayout.Space(6f);
            DrawSelectionInspector();
        }

        internal bool HasAnySelection => HasSelection;

        private void DrawBlockInspector(int index)
        {
            var block = working.Blocks[index];
            GUILayout.Label($"Block {block.Id}", EditorStyles.boldLabel);

            var id = EditorGUILayout.DelayedTextField("Id", block.Id);
            if (id != block.Id)
            {
                var before = block.Id;
                Edit("Rename Block", l => Set(ref l.Blocks[index].Id, id));
                FollowBlock(before, index);
            }

            var color = (BlockColor)EditorGUILayout.EnumPopup("Colour", block.Color);
            if (color != block.Color) Edit("Recolour Block", l => Set(ref l.Blocks[index].Color, color));

            EditorGUILayout.LabelField("Origin", $"{block.Col}, {block.Row}");
            EditorGUILayout.LabelField("Cells", block.Cells?.Length.ToString() ?? "0");
            DrawBlockActions();
        }

        private void DrawGateInspector(int index)
        {
            var gate = working.Gates[index];
            GUILayout.Label($"Gate #{index}", EditorStyles.boldLabel);
            var before = GateKey.Of(gate);

            var edge = (BoardEdge)EditorGUILayout.EnumPopup("Wall", gate.Edge);
            var slot = EditorGUILayout.DelayedIntField("Index", gate.Index);
            var depth = EditorGUILayout.DelayedIntField(new GUIContent("Depth", "Cells in from the outer edge; 0 is the outer wall"), gate.Depth);
            if (edge != gate.Edge || slot != gate.Index || depth != gate.Depth)
                Edit("Move Gate", l => LevelEditorGateOps.Move(l, index, edge, slot, depth));

            var length = EditorGUILayout.DelayedIntField("Length", gate.Length);
            if (length != gate.Length)
            {
                Edit("Resize Gate", l =>
                {
                    var wall = LevelValidator.EdgeLength(l.Gates[index].Edge, l.Cols, l.Rows);
                    l.Gates[index].Length = Mathf.Clamp(length, 1, wall);
                    LevelEditorGateOps.Move(l, index, l.Gates[index].Edge, l.Gates[index].Index, l.Gates[index].Depth);
                    return true;
                });
            }

            FollowGate(before, index);

            var color = (BlockColor)EditorGUILayout.EnumPopup("Colour", gate.Color);
            if (color != gate.Color) Edit("Recolour Gate", l => Set(ref l.Gates[index].Color, color));

            if (GUILayout.Button("Delete")) DeleteSelection();
        }

        // Several picked: what can be done to all of them at once.
        private void DrawManyInspector()
        {
            GUILayout.Label($"{selectedBlocks.Count} block(s), {selectedGates.Count} gate(s) selected", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Pick a colour in the palette to recolour them all.", MessageType.None);
            if (selectedBlocks.Count > 0) DrawBlockActions();
            else if (GUILayout.Button("Delete")) DeleteSelection();
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
