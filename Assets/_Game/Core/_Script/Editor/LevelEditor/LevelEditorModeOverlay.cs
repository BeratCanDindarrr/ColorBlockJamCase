using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace ColorBlockJam.LevelEditor
{
    // A small "+ / −" panel stuck to the current tool's button (Block, Gate, Cell; none for Select): below it on a
    // horizontal tools toolbar, to its right on a vertical one. It follows the toolbar when that is moved.
    [Overlay(typeof(SceneView), LevelEditorSceneOverlays.ModeId, "Mode", defaultDisplay = false,
        defaultDockZone = DockZone.Floating)]
    internal sealed class LevelEditorModeOverlay : Overlay
    {
        private const float Gap = 4f;
        private const float ButtonSize = 28f;
        private static readonly Color AddOn = new Color(0.20f, 0.62f, 0.30f);
        private static readonly Color EraseOn = new Color(0.78f, 0.22f, 0.22f);

        private VisualElement _content;
        private Button _add;
        private Button _erase;
        private EditingVisibility _visibility;

        public override VisualElement CreatePanelContent()
        {
            _content = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            _add = NewButton("+", false);
            _erase = NewButton("−", true);
            _content.Add(_add);
            _content.Add(_erase);
            Refresh();
            return _content;
        }

        // Not remembered: it follows the current tool and sticks to its button.
        public override void OnCreated() => _visibility = new EditingVisibility(this, Refresh,
            () => LevelEditorSceneOverlays.IsEditing && LevelEditorSession.Current.HasModes, remember: false);

        public override void OnWillBeDestroyed() => _visibility?.Dispose();

        private static Button NewButton(string label, bool erase)
        {
            var button = new Button(() => LevelEditorSession.Current.EraseMode = erase) { text = label };
            button.style.width = ButtonSize;
            button.style.height = ButtonSize - 4f;
            button.style.fontSize = 16;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            return button;
        }

        // Runs on every state change and Scene view layout: colours the picked mode and keeps the panel by the tool.
        private void Refresh()
        {
            var session = LevelEditorSession.Current;
            if (_content == null || session == null || !session.HasModes) return;

            var erase = session.EraseMode;
            Paint(_add, !erase, AddOn);
            Paint(_erase, erase, EraseOn);
            _add.tooltip = Tip(session.CurrentTool, false);
            _erase.tooltip = Tip(session.CurrentTool, true);
            StickToTool();
        }

        private static void Paint(Button button, bool on, Color color)
        {
            button.style.backgroundColor = on ? new StyleColor(color) : new StyleColor(StyleKeyword.Null);
            button.style.color = on ? new StyleColor(Color.white) : new StyleColor(StyleKeyword.Null);
        }

        // Moves the panel so its whole frame (title included) sits just outside the tools toolbar's frame, with its
        // buttons lined up with the tool's button.
        private void StickToTool()
        {
            var tool = ToolButton.Active;
            if (!floating || tool?.panel == null || tool.panel != _content.panel) return;

            var frame = OverlayFrame(_content);
            var toolsFrame = OverlayFrame(tool);
            if (frame == null || toolsFrame == null) return;

            var button = tool.worldBound;
            var own = frame.worldBound;
            var inset = _content.worldBound.position - own.position;
            var horizontal = tool.parent != null && tool.parent.resolvedStyle.flexDirection == FlexDirection.Row;
            var target = horizontal
                ? new Vector2(button.xMin - inset.x, toolsFrame.worldBound.yMax + Gap)
                : new Vector2(toolsFrame.worldBound.xMax + Gap, button.yMin - inset.y);
            var delta = target - own.position;
            if (delta.sqrMagnitude > 0.25f) floatingPosition += delta;
        }

        // The overlay's outer frame (Unity gives it the "unity-overlay" class).
        private static VisualElement OverlayFrame(VisualElement element)
        {
            for (var e = element; e != null; e = e.parent)
                if (e.ClassListContains("unity-overlay")) return e;
            return null;
        }

        private static string Tip(LevelEditorTool tool, bool erase)
        {
            switch (tool)
            {
                case LevelEditorTool.Block:
                    return erase ? "Erase: remove block cells (red shows what goes)" : "Add: paint new blocks, grow a picked one";
                case LevelEditorTool.Gate:
                    return erase ? "Erase: remove gate places, one at a time (red shows what goes)" : "Add: put gates on walls, lengthen a picked one";
                default:
                    return erase ? "Erase: remove board cells (red shows what goes)" : "Add: put removed cells back";
            }
        }
    }
}
