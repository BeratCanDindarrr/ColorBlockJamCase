using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;
using UnityEngine.UIElements;

namespace ColorBlockJam.LevelEditor
{
    // SceneView overlays of the level editor: a tools toolbar like Unity's own and a properties panel. They show when
    // the level editor starts editing in the editor scene and hide when it stops; in between they stay as left.
    internal static class LevelEditorSceneOverlays
    {
        public const string ToolsId = "colorblockjam-level-editor-tools";
        public const string PanelId = "colorblockjam-level-editor-properties";
        public const string ShapesId = "colorblockjam-level-editor-shapes";
        public const string LevelBarId = "colorblockjam-level-editor-level-bar";
        public const string ModeId = "colorblockjam-level-editor-mode";
        public const string LevelOrderId = "colorblockjam-level-editor-level-order";

        public static bool InEditorScene => SceneManager.GetActiveScene().path == LevelEditorSceneHook.EditorScenePath;

        public static bool IsEditing => InEditorScene && LevelEditorSession.CanEditFromScene;

        [MenuItem("Tools/Level Editor/Show Scene Panels")]
        public static void ShowAll()
        {
            if (!InEditorScene) return;
            foreach (SceneView sceneView in SceneView.sceneViews)
            {
                foreach (var id in new[] { ToolsId, LevelBarId, PanelId, ShapesId })
                {
                    if (!sceneView.TryGetOverlay(id, out var overlay)) continue;
                    overlay.displayed = true;
                    overlay.collapsed = false;
                }
            }
        }

        public static void SetShown(string id, bool shown)
        {
            foreach (SceneView sceneView in SceneView.sceneViews)
            {
                if (sceneView.TryGetOverlay(id, out var overlay)) overlay.displayed = shown;
            }
        }

        public static bool IsShown(string id)
        {
            var sceneView = SceneView.lastActiveSceneView;
            return sceneView != null && sceneView.TryGetOverlay(id, out var overlay) && overlay.displayed;
        }

        private const float CloseSize = 16f;
        private static readonly GUIContent CloseContent =
            new GUIContent("×", "Close (open it again from the level bar's Panels)");

        // A × at the top-right of a panel's content that closes it; Unity gives overlays no room in their own title.
        public static void DrawCloseButton(string id)
        {
            var row = GUILayoutUtility.GetRect(0f, CloseSize, GUILayout.ExpandWidth(true));
            if (GUI.Button(new Rect(row.xMax - CloseSize, row.y, CloseSize, CloseSize), CloseContent, EditorStyles.miniButton))
                SetShown(id, false);
        }
    }

    // Shows the panel when editing starts and hides it when it stops, following LevelEditorSession.StateChanged. While
    // editing, what the user does to it (closing, folding, moving) is remembered (LevelEditorPanelMemory, unless
    // `remember` is false) and given back the next time editing starts; a panel never opened or closed by hand opens by
    // itself only when `opensItself`.
    internal sealed class EditingVisibility
    {
        private readonly Overlay _overlay;
        private readonly Action _onState;
        private readonly Func<bool> _shouldShow;
        private readonly bool _opensItself;
        private readonly LevelEditorPanelMemory _memory;
        private bool _wasEditing;
        private bool _synced;
        // Set while this class itself shows, hides or places the panel, so that is not taken for the user's doing.
        private bool _applying;

        public EditingVisibility(Overlay overlay, Action onState = null, Func<bool> shouldShow = null, bool opensItself = true,
            bool remember = true)
        {
            _overlay = overlay;
            _onState = onState;
            _shouldShow = shouldShow ?? (() => LevelEditorSceneOverlays.IsEditing);
            _opensItself = opensItself;
            _memory = remember ? new LevelEditorPanelMemory(overlay.id) : null;
            _wasEditing = _shouldShow();
            Apply(_wasEditing);
            _overlay.displayedChanged += OnDisplayedChanged;
            _overlay.collapsedChanged += OnCollapsedChanged;
            _overlay.floatingChanged += OnPlaced;
            _overlay.floatingPositionChanged += OnPlaced;
            LevelEditorSession.StateChanged += Sync;
            EditorApplication.hierarchyChanged += Sync;
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
            // Overlays are made during a script reload, before the scene and assets can be read; checking on every Scene
            // view draw catches the editing state as soon as it is known (only a change shows or hides the overlay).
            SceneView.duringSceneGui += OnSceneGui;
        }

        public bool HasSavedPosition => _memory != null && _memory.HasPosition;

        private void OnSceneGui(SceneView sceneView)
        {
            if (Event.current.type != EventType.Layout) return;
            if (!_synced)
            {
                // Unity restores an overlay's saved visibility after creating it, so the first Scene view draw sets it
                // once to the editing state and the remembered look, whatever was restored.
                _synced = true;
                _wasEditing = _shouldShow();
                Apply(_wasEditing);
            }

            Sync();
        }

        public void Dispose()
        {
            _overlay.displayedChanged -= OnDisplayedChanged;
            _overlay.collapsedChanged -= OnCollapsedChanged;
            _overlay.floatingChanged -= OnPlaced;
            _overlay.floatingPositionChanged -= OnPlaced;
            SceneView.duringSceneGui -= OnSceneGui;
            LevelEditorSession.StateChanged -= Sync;
            EditorApplication.hierarchyChanged -= Sync;
            EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
        }

        private void OnSceneChanged(Scene from, Scene to) => Sync();

        private void Sync()
        {
            // Outside the editor scene the level editor's overlays are never shown, whatever a saved layout says.
            if (!LevelEditorSceneOverlays.InEditorScene)
            {
                if (_overlay.displayed) Apply(false);
                _wasEditing = false;
                return;
            }

            var isEditing = _shouldShow();
            if (isEditing != _wasEditing)
            {
                _wasEditing = isEditing;
                Apply(isEditing);
            }

            _onState?.Invoke();
        }

        // Shows the panel the way it was left (open or closed, folded, where it floated) while editing, hides it otherwise.
        private void Apply(bool editing)
        {
            _applying = true;
            try
            {
                var open = editing && (_memory?.Shown(_opensItself) ?? _opensItself);
                if (_overlay.displayed != open) _overlay.displayed = open;
                if (open) _memory?.Restore(_overlay);
            }
            finally
            {
                _applying = false;
            }
        }

        private bool IsUsersDoing => _memory != null && _synced && !_applying && _wasEditing &&
                                     LevelEditorSceneOverlays.InEditorScene;

        private void OnDisplayedChanged(bool shown)
        {
            if (IsUsersDoing) _memory.SaveShown(shown);
        }

        private void OnCollapsedChanged(bool collapsed)
        {
            if (IsUsersDoing) _memory.SaveCollapsed(collapsed);
        }

        private void OnPlaced(bool floating) => OnPlaced();

        private void OnPlaced(Vector3 position) => OnPlaced();

        private void OnPlaced()
        {
            if (IsUsersDoing && _overlay.displayed) _memory.SaveFloating(_overlay);
        }
    }

    [Overlay(typeof(SceneView), LevelEditorSceneOverlays.ToolsId, "Level Editor Tools", defaultDisplay = true,
        defaultDockZone = DockZone.LeftColumn, defaultLayout = Layout.VerticalToolbar)]
    internal sealed class LevelEditorToolsOverlay : ToolbarOverlay
    {
        private EditingVisibility _visibility;

        public LevelEditorToolsOverlay() : base(SelectToolButton.Id, BlockToolButton.Id, GateToolButton.Id,
            CellToolButton.Id)
        {
        }

        public override void OnCreated() => _visibility = new EditingVisibility(this);

        public override void OnWillBeDestroyed() => _visibility?.Dispose();
    }

    // Colour palette and the selected block or gate, drawn by the level editor itself.
    // Level, save / play, size, time, problems and the palette: a flat bar along the top of the Scene view.
    [Overlay(typeof(SceneView), LevelEditorSceneOverlays.LevelBarId, "Level", defaultDisplay = true,
        defaultDockZone = DockZone.Floating)]
    internal sealed class LevelEditorLevelBarOverlay : Overlay
    {
        // First place, right of the tools column; where it is moved to later is kept by Unity.
        private static readonly Vector2 StartPosition = new Vector2(60f, 4f);

        private IMGUIContainer _content;
        private EditingVisibility _visibility;

        public override VisualElement CreatePanelContent()
        {
            _content = new IMGUIContainer(() => LevelEditorSession.Current?.DrawLevelBar());
            _content.style.width = LevelEditorSession.BarWidth;
            return _content;
        }

        public override void OnCreated()
        {
            _visibility = new EditingVisibility(this, () => _content?.MarkDirtyRepaint());
            // First place only; after that it goes back to where it was left.
            if (!_visibility.HasSavedPosition) floatingPosition = StartPosition;
        }

        public override void OnWillBeDestroyed() => _visibility?.Dispose();
    }

    // The colour palette and what can be done to the selected blocks or gates; shown while editing.
    [Overlay(typeof(SceneView), LevelEditorSceneOverlays.PanelId, "Properties", defaultDisplay = true,
        defaultDockZone = DockZone.RightColumn)]
    internal sealed class LevelEditorPropertiesOverlay : Overlay
    {
        private const float Width = 250f;
        private const int SwatchesPerRow = 7;

        private IMGUIContainer _content;
        private EditingVisibility _visibility;

        public override VisualElement CreatePanelContent()
        {
            _content = new IMGUIContainer(() =>
            {
                LevelEditorSceneOverlays.DrawCloseButton(LevelEditorSceneOverlays.PanelId);
                LevelEditorSession.Current?.DrawScenePanel(SwatchesPerRow, Width);
            });
            _content.style.width = Width;
            return _content;
        }

        // Open / folded / where it is are remembered by EditingVisibility.
        public override void OnCreated() => _visibility = new EditingVisibility(this, () => _content?.MarkDirtyRepaint());

        public override void OnWillBeDestroyed() => _visibility?.Dispose();
    }

    // Often used shapes (LevelEditorShapeLibrary) to drag onto the board, shown while editing.
    [Overlay(typeof(SceneView), LevelEditorSceneOverlays.ShapesId, "Shapes", defaultDisplay = true,
        defaultDockZone = DockZone.RightColumn)]
    internal sealed class LevelEditorShapesOverlay : Overlay
    {
        private const float Width = 250f;
        private const float Thumb = 52f;
        private const float Gap = 6f;
        private const int PerRow = 4;
        private const float EditIcon = 16f;
        private static readonly GUIContent EditContent =
            new GUIContent(EditorGUIUtility.IconContent("d_editicon.sml").image, "Rename or delete");

        private IMGUIContainer _content;
        private EditingVisibility _visibility;

        public override VisualElement CreatePanelContent()
        {
            _content = new IMGUIContainer(Draw);
            _content.style.width = Width;
            return _content;
        }

        public override void OnCreated() => _visibility = new EditingVisibility(this, () => _content?.MarkDirtyRepaint());

        public override void OnWillBeDestroyed() => _visibility?.Dispose();

        private static void Draw()
        {
            LevelEditorSceneOverlays.DrawCloseButton(LevelEditorSceneOverlays.ShapesId);
            var session = LevelEditorSession.Current;
            var library = LevelEditorShapeLibrary.Get();
            GUILayout.Label("Drag a shape onto the board", EditorStyles.miniBoldLabel);

            for (var i = 0; i < library.Shapes.Count; i += PerRow)
            {
                var row = GUILayoutUtility.GetRect(Width, Thumb + 14f);
                for (var j = 0; j < PerRow && i + j < library.Shapes.Count; j++)
                    DrawShape(library, i + j, new Rect(row.x + j * (Thumb + Gap), row.y, Thumb, Thumb), session);
            }

            using (new EditorGUI.DisabledScope(!session.CanAddSelectedShape))
            {
                if (GUILayout.Button(new GUIContent("+ Add selected block's shape",
                        "Pick one block (Select tool), then keep its shape here"))) session.AddSelectedToShapes();
            }

            GUILayout.Label("Pencil: rename or delete a shape", EditorStyles.centeredGreyMiniLabel);
        }

        // A thumbnail of the shape in the brush colour; pressing it starts the drag, its pencil edits it.
        private static void DrawShape(LevelEditorShapeLibrary library, int index, Rect rect, LevelEditorSession session)
        {
            var shape = library.Shapes[index];
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.25f));
            var cols = shape.Cells.Max(c => c.Col) + 1;
            var rows = shape.Cells.Max(c => c.Row) + 1;
            var cell = Mathf.Min((rect.width - 8f) / cols, (rect.height - 8f) / rows);
            var origin = new Vector2(rect.center.x - cols * cell / 2f, rect.center.y - rows * cell / 2f);
            var color = LevelEditorStyle.Of(session.Brush);
            foreach (var c in shape.Cells)
                EditorGUI.DrawRect(new Rect(origin.x + c.Col * cell + 1f, origin.y + c.Row * cell + 1f, cell - 2f, cell - 2f), color);
            GUI.Label(new Rect(rect.x, rect.yMax, rect.width, 14f), shape.Name, EditorStyles.centeredGreyMiniLabel);

            // Right-click never reaches an overlay (the Scene view keeps it for the camera), so editing has its own button.
            var editRect = new Rect(rect.xMax - EditIcon - 1f, rect.y + 1f, EditIcon, EditIcon);
            if (GUI.Button(editRect, EditContent, EditorStyles.iconButton))
            {
                UnityEditor.PopupWindow.Show(editRect, new EditShapePopup(index));
                return;
            }

            var e = Event.current;
            if (!rect.Contains(e.mousePosition)) return;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new UnityEngine.Object[0];
                DragAndDrop.SetGenericData(LevelEditorSession.ShapeDragKey, index);
                DragAndDrop.StartDrag(shape.Name);
                e.Use();
            }
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
                LevelEditorSession.StateChanged += Refresh;
                Refresh();
            });
            RegisterCallback<DetachFromPanelEvent>(_ => LevelEditorSession.StateChanged -= Refresh);
        }

        private void OnToggled(ChangeEvent<bool> change)
        {
            var window = LevelEditorSession.Current;
            if (window != null && change.newValue) window.CurrentTool = _tool;
            Refresh();
        }

        // The button of the current tool, for the "+ / −" panel to sit beside.
        internal static ToolButton Active { get; private set; }

        // A tool button cannot be switched off by clicking it; another tool takes over.
        private void Refresh()
        {
            var window = LevelEditorSession.Current;
            SetValueWithoutNotify(window != null && window.CurrentTool == _tool);
            if (value) Active = this;
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
            "Block (2)\n+ paints cells in the brush colour, − erases block cells (Shift flips)")
        {
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    internal sealed class GateToolButton : ToolButton
    {
        public const string Id = "ColorBlockJam/LevelEditor/Gate";

        public GateToolButton() : base(LevelEditorTool.Gate, "Grid.BoxTool",
            "Gate (3)\n+ adds a gate on a wall around the board or beside a removed cell, − removes the pressed gate place (Shift flips)")
        {
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    internal sealed class CellToolButton : ToolButton
    {
        public const string Id = "ColorBlockJam/LevelEditor/Cell";

        public CellToolButton() : base(LevelEditorTool.Cell, "Grid.EraserTool",
            "Cell (4)\n− removes board cells, + puts them back (Shift flips)")
        {
        }
    }
}
