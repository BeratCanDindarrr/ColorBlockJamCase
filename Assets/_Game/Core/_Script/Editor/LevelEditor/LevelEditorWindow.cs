using System;
using System.Collections.Generic;
using ColorBlockJam.Const;
using ColorBlockJam.Core.Level;
using ColorBlockJam.Core.Settings;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    internal enum LevelEditorTool
    {
        Select,
        Block,
        Gate
    }

    // Level editor: edits a working copy of a LevelData asset (one undo step per edit) and writes it back on Save.
    // State and saving live here; input in LevelEditorWindow.Input.cs, panels in LevelEditorWindow.Gui.cs.
    public sealed partial class LevelEditorWindow : EditorWindow
    {
        [SerializeField] private LevelData source;
        [SerializeField] private LevelData working;
        [SerializeField] private string savedJson;
        [SerializeField] private LevelEditorTool tool = LevelEditorTool.Block;
        [SerializeField] private BlockColor brush;

        private readonly LevelEditorBoardGui _board = new LevelEditorBoardGui();
        private List<LevelIssue> _issues = new List<LevelIssue>();
        private readonly HashSet<int> _errorBlocks = new HashSet<int>();
        private readonly HashSet<int> _errorGates = new HashSet<int>();

        private bool IsDirty => working != null && JsonUtility.ToJson(working) != savedJson;
        private bool HasErrors => _issues.Exists(i => i.IsError);

        [MenuItem("Tools/Level Editor/Open Level Editor")]
        public static void Open()
        {
            var window = GetWindow<LevelEditorWindow>("Level Editor");
            window.minSize = new Vector2(760f, 480f);
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            if (working == null)
            {
                var levels = LevelEditorAssets.FindLevels();
                if (source == null && levels.Count > 0) source = levels[0];
                if (source != null) LoadWorkingCopy(source);
            }

            RefreshIssues();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            DestroyRawEditor();
        }

        private void OnDestroy()
        {
            if (IsDirty && EditorUtility.DisplayDialog("Level Editor", $"Save the changes to {source.name}?", "Save", "Discard"))
                Save();
            if (working != null) DestroyImmediate(working);
        }

        // One undoable step on the working copy; change returns whether it changed anything.
        private void Edit(string undoName, Func<LevelData, bool> change)
        {
            Undo.RecordObject(working, undoName);
            if (!change(working)) return;

            EditorUtility.SetDirty(working);
            RefreshIssues();
            Repaint();
        }

        private bool TryOpen(LevelData level)
        {
            if (level == null || level == source) return false;
            if (!ConfirmLeave()) return false;

            source = level;
            LoadWorkingCopy(level);
            return true;
        }

        // Unsaved changes: Save, Discard or stay. Returns false to stay.
        private bool ConfirmLeave()
        {
            if (!IsDirty) return true;
            var choice = EditorUtility.DisplayDialogComplex("Level Editor", $"Save the changes to {source.name}?",
                "Save", "Cancel", "Discard");
            return choice == 2 || (choice == 0 && Save());
        }

        private void LoadWorkingCopy(LevelData level)
        {
            if (working != null) DestroyImmediate(working);
            working = Instantiate(level);
            working.name = level.name;
            working.hideFlags = HideFlags.DontSave;
            working.Blocks ??= new BlockData[0];
            working.Gates ??= new GateData[0];
            savedJson = JsonUtility.ToJson(working);
            selectedBlocks.Clear();
            selectedGates.Clear();
            DestroyRawEditor();
            RefreshIssues();
        }

        private bool Save()
        {
            if (working == null) return false;
            if (HasErrors)
            {
                EditorUtility.DisplayDialog("Level Editor", "Fix the errors listed at the bottom before saving.", "OK");
                return false;
            }

            Undo.RecordObject(source, "Save Level");
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(working), source);
            EditorUtility.SetDirty(source);
            AssetDatabase.SaveAssetIfDirty(source);
            savedJson = JsonUtility.ToJson(working);
            return true;
        }

        private void Revert()
        {
            if (source != null) LoadWorkingCopy(source);
        }

        private void CreateLevel()
        {
            if (!ConfirmLeave()) return;
            var level = LevelEditorAssets.CreateLevel();
            source = level;
            LoadWorkingCopy(level);
        }

        // Saves, then starts the game (from the main scene, see PlayModeStartScene) straight into this level.
        private void PlayLevel()
        {
            if (IsDirty && !Save()) return;

            var index = LevelEditorAssets.PlayIndexOf(source);
            if (index < 0)
            {
                EditorUtility.DisplayDialog("Level Editor", $"{source.name} is not in LevelSettings.Levels.", "OK");
                return;
            }

            SessionState.SetInt(GlobalConst.LevelEditor.PlayLevelKey, index);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            // The next Play from the toolbar is a normal game again.
            if (change == PlayModeStateChange.EnteredEditMode) SessionState.EraseInt(GlobalConst.LevelEditor.PlayLevelKey);
        }

        private void OnUndoRedo()
        {
            if (working == null) return;
            PruneSelection();
            RefreshIssues();
            Repaint();
        }

        private void RefreshIssues()
        {
            _errorBlocks.Clear();
            _errorGates.Clear();
            if (working == null)
            {
                _issues.Clear();
                return;
            }

            _issues = LevelValidator.Validate(working.Cols, working.Rows, working.TimeLimitSec, working.Blocks, working.Gates);
            foreach (var issue in _issues)
            {
                if (!issue.IsError) continue;
                if (issue.BlockIndex >= 0) _errorBlocks.Add(issue.BlockIndex);
                if (issue.GateIndex >= 0) _errorGates.Add(issue.GateIndex);
            }
        }
    }
}
