using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // The level editor's window: draws the LevelEditorSession (header, tools, board, inspector, problems). Optional — the
    // editor scene's preview, SceneView editing and overlays work without it, and closing it keeps the edits.
    public sealed class LevelEditorWindow : EditorWindow
    {
        [MenuItem("Tools/Level Editor/Open Level Editor")]
        public static void Open()
        {
            var window = GetWindow<LevelEditorWindow>("Level Editor");
            window.minSize = new Vector2(760f, 480f);
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            LevelEditorSession.StateChanged += Repaint;
        }

        private void OnDisable()
        {
            LevelEditorSession.StateChanged -= Repaint;
        }

        private void OnGUI()
        {
            LevelEditorSession.instance.DrawWindow();
        }
    }
}
