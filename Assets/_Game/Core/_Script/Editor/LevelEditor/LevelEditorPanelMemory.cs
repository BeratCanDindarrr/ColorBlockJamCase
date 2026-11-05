using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // How the user left one level editor panel: open or closed, folded or not, and where it floated. Kept per user and
    // project in EditorPrefs, so it survives script reloads, scene and layout switches and Unity quitting in EditorScene
    // (when the editor scene's window layout was never written back).
    internal sealed class LevelEditorPanelMemory
    {
        private readonly string _key;

        public LevelEditorPanelMemory(string overlayId)
        {
            _key = "ColorBlockJam.LevelEditor.Panel." + Application.dataPath + "." + overlayId + ".";
        }

        public bool HasPosition => EditorPrefs.HasKey(_key + "X");

        public bool Shown(bool byDefault) => EditorPrefs.GetBool(_key + "Shown", byDefault);

        public void SaveShown(bool shown) => EditorPrefs.SetBool(_key + "Shown", shown);

        public void SaveCollapsed(bool collapsed) => EditorPrefs.SetBool(_key + "Collapsed", collapsed);

        // Docked panels are left to Unity's layout; a floating one keeps its place here.
        public void SaveFloating(Overlay overlay)
        {
            EditorPrefs.SetBool(_key + "Floating", overlay.floating);
            if (!overlay.floating) return;
            EditorPrefs.SetFloat(_key + "X", overlay.floatingPosition.x);
            EditorPrefs.SetFloat(_key + "Y", overlay.floatingPosition.y);
        }

        public void Restore(Overlay overlay)
        {
            if (EditorPrefs.HasKey(_key + "Collapsed")) overlay.collapsed = EditorPrefs.GetBool(_key + "Collapsed");
            if (!EditorPrefs.GetBool(_key + "Floating", false) || !HasPosition) return;

            if (!overlay.floating) overlay.Undock();
            overlay.floatingPosition = new Vector2(EditorPrefs.GetFloat(_key + "X"), EditorPrefs.GetFloat(_key + "Y"));
        }
    }
}
