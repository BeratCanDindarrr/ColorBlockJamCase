using System;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ColorBlockJam.LevelEditor
{
    // SceneView overlays of the level editor: a tools toolbar like Unity's own and a properties panel. They show when
    // the level editor starts editing in the editor scene and hide when it stops; in between they stay as left.
    internal static class LevelEditorSceneOverlays
    {
        public const string ToolsId = "colorblockjam-level-editor-tools";
        public const string PanelId = "colorblockjam-level-editor-properties";

        public static bool IsEditing =>
            LevelEditorWindow.CanEditFromScene && SceneManager.GetActiveScene().path == LevelEditorSceneHook.EditorScenePath;

        [MenuItem("Tools/Level Editor/Show Scene Panels")]
        public static void ShowAll()
        {
            foreach (SceneView sceneView in SceneView.sceneViews)
            {
                foreach (var id in new[] { ToolsId, PanelId })
                {
                    if (!sceneView.TryGetOverlay(id, out var overlay)) continue;
                    overlay.displayed = true;
                    overlay.collapsed = false;
                }
            }
        }

        public static void SetPanelShown(bool shown)
        {
            foreach (SceneView sceneView in SceneView.sceneViews)
            {
                if (sceneView.TryGetOverlay(PanelId, out var panel)) panel.displayed = shown;
            }
        }

        public static bool IsPanelShown()
        {
            var sceneView = SceneView.lastActiveSceneView;
            return sceneView != null && sceneView.TryGetOverlay(PanelId, out var panel) && panel.displayed;
        }
    }

    // Shows itself when editing starts and hides when it stops, following LevelEditorWindow.StateChanged.
    internal sealed class EditingVisibility
    {
        private readonly Overlay _overlay;
        private readonly Action _onState;
        private bool _wasEditing;

        public EditingVisibility(Overlay overlay, Action onState = null)
        {
            _overlay = overlay;
            _onState = onState;
            _wasEditing = LevelEditorSceneOverlays.IsEditing;
            if (!_wasEditing) _overlay.displayed = false;
            LevelEditorWindow.StateChanged += Sync;
            EditorApplication.hierarchyChanged += Sync;
        }

        public void Dispose()
        {
            LevelEditorWindow.StateChanged -= Sync;
            EditorApplication.hierarchyChanged -= Sync;
        }

        private void Sync()
        {
            var isEditing = LevelEditorSceneOverlays.IsEditing;
            if (isEditing != _wasEditing)
            {
                _wasEditing = isEditing;
                _overlay.displayed = isEditing;
            }

            _onState?.Invoke();
        }
    }

    [Overlay(typeof(SceneView), LevelEditorSceneOverlays.ToolsId, "Level Editor Tools", defaultDisplay = true,
        defaultDockZone = DockZone.LeftColumn, defaultLayout = Layout.VerticalToolbar)]
    internal sealed class LevelEditorToolsOverlay : ToolbarOverlay
    {
        private EditingVisibility _visibility;

        public LevelEditorToolsOverlay() : base(SelectToolButton.Id, BlockToolButton.Id, GateToolButton.Id,
            PanelButton.Id)
        {
        }

        public override void OnCreated() => _visibility = new EditingVisibility(this);

        public override void OnWillBeDestroyed() => _visibility?.Dispose();
    }

    // Colour palette and the selected block or gate, drawn by the level editor itself.
    [Overlay(typeof(SceneView), LevelEditorSceneOverlays.PanelId, "Level Editor", defaultDisplay = true,
        defaultDockZone = DockZone.RightColumn)]
    internal sealed class LevelEditorPropertiesOverlay : Overlay
    {
        private const float Width = 250f;

        private IMGUIContainer _content;
        private EditingVisibility _visibility;
        private bool _hadSelection;

        public override VisualElement CreatePanelContent()
        {
            _content = new IMGUIContainer(() => LevelEditorWindow.Current?.DrawScenePanel());
            _content.style.width = Width;
            return _content;
        }

        public override void OnCreated() => _visibility = new EditingVisibility(this, OnState);

        public override void OnWillBeDestroyed() => _visibility?.Dispose();

        // Opens when something gets picked, so its properties are at hand.
        private void OnState()
        {
            var window = LevelEditorWindow.Current;
            var hasSelection = window != null && window.HasAnySelection;
            if (hasSelection && !_hadSelection && LevelEditorSceneOverlays.IsEditing)
            {
                displayed = true;
                collapsed = false;
            }

            _hadSelection = hasSelection;
            _content?.MarkDirtyRepaint();
        }
    }

    // One tool of the level editor; on while it is the current tool.
    internal abstract class ToolButton : EditorToolbarToggle
    {
        private readonly LevelEditorTool _tool;

        protected ToolButton(LevelEditorTool tool, string iconName, string tooltipText)
        {
            _tool = tool;
            icon = EditorGUIUtility.IconContent(iconName).image as Texture2D;
            tooltip = tooltipText;
            this.RegisterValueChangedCallback(OnToggled);
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                LevelEditorWindow.StateChanged += Refresh;
                Refresh();
            });
            RegisterCallback<DetachFromPanelEvent>(_ => LevelEditorWindow.StateChanged -= Refresh);
        }

        private void OnToggled(ChangeEvent<bool> change)
        {
            var window = LevelEditorWindow.Current;
            if (window != null && change.newValue) window.CurrentTool = _tool;
            Refresh();
        }

        // A tool button cannot be switched off by clicking it; another tool takes over.
        private void Refresh()
        {
            var window = LevelEditorWindow.Current;
            SetValueWithoutNotify(window != null && window.CurrentTool == _tool);
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    internal sealed class SelectToolButton : ToolButton
    {
        public const string Id = "ColorBlockJam/LevelEditor/Select";

        public SelectToolButton() : base(LevelEditorTool.Select, "Grid.Default",
            "Select (1)\nClick picks, Shift/Ctrl+click adds, drag on empty space draws a box, drag a block to move it")
        {
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    internal sealed class BlockToolButton : ToolButton
    {
        public const string Id = "ColorBlockJam/LevelEditor/Block";

        public BlockToolButton() : base(LevelEditorTool.Block, "Grid.PaintTool",
            "Block (2)\nClick or drag to paint cells in the brush colour, Shift erases")
        {
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    internal sealed class GateToolButton : ToolButton
    {
        public const string Id = "ColorBlockJam/LevelEditor/Gate";

        public GateToolButton() : base(LevelEditorTool.Gate, "Grid.BoxTool",
            "Gate (3)\nClick a wall slot around the board to add a gate in the brush colour, Shift deletes")
        {
        }
    }

    // Shows or hides the properties panel.
    [EditorToolbarElement(Id, typeof(SceneView))]
    internal sealed class PanelButton : EditorToolbarToggle
    {
        public const string Id = "ColorBlockJam/LevelEditor/Panel";

        public PanelButton()
        {
            icon = EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow").image as Texture2D;
            tooltip = "Properties\nColour palette and the selected block or gate";
            this.RegisterValueChangedCallback(change => LevelEditorSceneOverlays.SetPanelShown(change.newValue));
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                LevelEditorWindow.StateChanged += Refresh;
                EditorApplication.update += Refresh;
                Refresh();
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                LevelEditorWindow.StateChanged -= Refresh;
                EditorApplication.update -= Refresh;
            });
        }

        // The panel can also be closed from its own menu; the button follows it.
        private void Refresh()
        {
            var shown = LevelEditorSceneOverlays.IsPanelShown();
            if (value != shown) SetValueWithoutNotify(shown);
        }
    }
}
