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
        Gate,
        // Removes board cells and puts them back.
        Cell
    }

    // The level editor's session: edits a working copy of a LevelData asset (one undo step per edit) and writes it back
    // on Save. It lives without any window (one per editor, kept across script reloads), so the editor scene's preview,
    // SceneView editing and overlays work on their own; LevelEditorWindow only draws it. State and saving live here;
    // input in LevelEditorSession.Input.cs, panels in LevelEditorSession.Gui.cs / .Inspector.cs.
    public sealed partial class LevelEditorSession : ScriptableSingleton<LevelEditorSession>
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

        // The level being edited, after every change. Scene previews (SandBox editor code, which this assembly cannot
        // see) listen to it.
        public static event Action<LevelData> WorkingLevelChanged;

        // The level being edited; the last (or first) level is opened when none is yet.
        public static LevelData CurrentWorking => instance.EnsureWorking();

        // Tool, brush, selection or level changed: the SceneView overlays and marks and the window refresh.
        internal static event Action StateChanged;

        internal static LevelEditorSession Current => instance;

        internal BlockColor Brush => brush;

        internal LevelEditorTool CurrentTool
        {
            get => tool;
            set
            {
                if (tool == value) return;
                SwitchTool(value);
            }
        }

        // Picking a tool (also the same one again, with its number key) leaves what was picked.
        internal void SwitchTool(LevelEditorTool next)
        {
            tool = next;
            selectedBlocks.Clear();
            selectedGates.Clear();
            RaiseStateChanged();
        }

        internal void RaiseStateChanged()
        {
            Repaint();
            StateChanged?.Invoke();
            SceneView.RepaintAll();
        }

        private HashSet<CellCoord> Holes => new HashSet<CellCoord>(working.Holes ?? new CellCoord[0]);

        private bool IsDirty => working != null && JsonUtility.ToJson(working) != savedJson;
        private bool HasErrors => _issues.Exists(i => i.IsError);

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.wantsToQuit += OnWantsToQuit;
            RefreshIssues();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.wantsToQuit -= OnWantsToQuit;
            DestroyRawEditor();
        }

        internal LevelData EnsureWorking()
        {
            if (working != null) return working;

            var levels = LevelEditorAssets.FindLevels();
            if (source == null && levels.Count > 0) source = levels[0];
            if (source != null) LoadWorkingCopy(source);
            return working;
        }

        // Unsaved edits are asked about when Unity quits (closing the window keeps them).
        private bool OnWantsToQuit()
        {
            if (!IsDirty) return true;
            var choice = EditorUtility.DisplayDialogComplex("Level Editor",
                $"Save the changes to {source.name} before quitting?", "Save", "Cancel", "Discard");
            return choice == 2 || (choice == 0 && Save());
        }

        // Every open Level Editor window shows this session.
        private static void Repaint()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<LevelEditorWindow>()) window.Repaint();
        }

        private static void ShowNotification(GUIContent message)
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<LevelEditorWindow>()) window.ShowNotification(message);
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.ShowNotification(message);
        }

        // One undoable step on the working copy; change returns whether it changed anything.
        private void Edit(string undoName, Func<LevelData, bool> change)
        {
            Undo.RecordObject(working, undoName);
            if (!change(working)) return;

            EditorUtility.SetDirty(working);
            RefreshIssues();
            NotifyWorkingChanged();
            RaiseStateChanged();
        }

        // Redraws the panels and rebuilds the scene preview, e.g. after a palette colour changed.
        internal void RefreshViews()
        {
            NotifyWorkingChanged();
            RaiseStateChanged();
        }

        private void NotifyWorkingChanged()
        {
            WorkingLevelChanged?.Invoke(working);
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
            working.Holes ??= new CellCoord[0];
            savedJson = JsonUtility.ToJson(working);
            selectedBlocks.Clear();
            selectedGates.Clear();
            DestroyRawEditor();
            RefreshIssues();
            NotifyWorkingChanged();
            RaiseStateChanged();
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
            NotifyWorkingChanged();
            RaiseStateChanged();
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

            _issues = LevelValidator.Validate(working.Cols, working.Rows, working.TimeLimitSec, working.Blocks, working.Gates,
                working.Holes);
            foreach (var issue in _issues)
            {
                if (!issue.IsError) continue;
                if (issue.BlockIndex >= 0) _errorBlocks.Add(issue.BlockIndex);
                if (issue.GateIndex >= 0) _errorGates.Add(issue.GateIndex);
            }
        }
    }
}
