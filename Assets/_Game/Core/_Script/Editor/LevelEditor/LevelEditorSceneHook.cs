using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorBlockJam.LevelEditor
{
    // The editor scene has its own window layout. Opening it saves the current layout as the "normal" one and loads the
    // editor scene's layout (first time: only the Scene and Game views, side by side); leaving it saves the editor scene's
    // layout, with whatever windows were added there (the Level Editor, ...), and brings the normal one back. Layouts are
    // kept per user in UserSettings/Layouts (not in git).
    [InitializeOnLoad]
    internal static class LevelEditorSceneHook
    {
        internal const string EditorScenePath = "Assets/Scenes/EditorScene.unity";

        private const string LayoutFolder = "UserSettings/Layouts";
        private const string EditorLayoutFile = LayoutFolder + "/ColorBlockJam_EditorScene.wlt";
        private const string NormalLayoutFile = LayoutFolder + "/ColorBlockJam_Normal.wlt";

        // Whether the editor scene's layout is the one on screen; kept across editor restarts.
        private static string ActiveKey => "ColorBlockJam.EditorSceneLayout.Active." + Application.dataPath;

        private static bool IsEditorLayoutActive
        {
            get => EditorPrefs.GetBool(ActiveKey, false);
            set => EditorPrefs.SetBool(ActiveKey, value);
        }

        // The Camera gizmo state from before the editor scene hid it ("icon,gizmo"), restored on leaving.
        private static string CameraGizmoKey => "ColorBlockJam.EditorSceneLayout.CameraGizmo." + Application.dataPath;

        static LevelEditorSceneHook()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            // Already in the editor scene after a reload or restart: the transition that hides the gizmo has passed.
            EditorApplication.delayCall += () =>
            {
                if (SceneManager.GetActiveScene().path == EditorScenePath) HideCameraGizmo(true);
            };
        }

        [MenuItem("Tools/Level Editor/Reset Editor Scene Layout")]
        private static void ResetEditorLayout()
        {
            if (File.Exists(EditorLayoutFile)) File.Delete(EditorLayoutFile);
            if (SceneManager.GetActiveScene().path == EditorScenePath) BuildDefaultLayout();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (mode != OpenSceneMode.Single || EditorApplication.isPlayingOrWillChangePlaymode) return;

            var toEditor = scene.path == EditorScenePath;
            if (toEditor == IsEditorLayoutActive) return;

            // Window layouts cannot change inside the scene-open callback.
            EditorApplication.delayCall += () => SwitchLayout(toEditor);
        }

        private static void SwitchLayout(bool toEditor)
        {
            Directory.CreateDirectory(LayoutFolder);
            HideCameraGizmo(toEditor);
            if (!SaveLayout(toEditor ? NormalLayoutFile : EditorLayoutFile)) return;
            IsEditorLayoutActive = toEditor;

            var target = toEditor ? EditorLayoutFile : NormalLayoutFile;
            if (File.Exists(target)) EditorUtility.LoadWindowLayout(Path.GetFullPath(target));
            else if (toEditor) BuildDefaultLayout();
        }

        // Only the Scene and Game views, docked side by side; everything else is added by hand and kept in the layout.
        private static void BuildDefaultLayout()
        {
            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (!(window is SceneView) && window.GetType() != gameViewType) window.Close();
            }

            var sceneView = SceneView.lastActiveSceneView != null
                ? SceneView.lastActiveSceneView
                : EditorWindow.GetWindow<SceneView>();
            // A Game view already in its own panel beside the Scene view stays; otherwise it becomes a right-hand column.
            var gameView = EditorWindow.GetWindow(gameViewType, false, "Game", false);
            if (WindowDocking.IsBeside(sceneView, gameView) || WindowDocking.DockAtRight(sceneView, gameView))
                WindowDocking.SplitEvenly(gameView);
            else
                EditorWindow.GetWindow(gameViewType, false, "Game", true);
            sceneView.Focus();
        }

        // The Scene view is aligned to the preview camera, so its icon and frustum sit right in front of the board.
        // Gizmo settings are global: the editor scene turns the Camera ones off and gives them back when it is left.
        private static void HideCameraGizmo(bool hide)
        {
            var saved = EditorPrefs.GetString(CameraGizmoKey, "");
            if (hide)
            {
                if (saved.Length > 0 || !GizmoUtility.TryGetGizmoInfo(typeof(Camera), out var info)) return;
                EditorPrefs.SetString(CameraGizmoKey, $"{info.iconEnabled},{info.gizmoEnabled}");
                GizmoUtility.SetIconEnabled(typeof(Camera), false);
                GizmoUtility.SetGizmoEnabled(typeof(Camera), false);
            }
            else if (saved.Length > 0)
            {
                var parts = saved.Split(',');
                GizmoUtility.SetIconEnabled(typeof(Camera), parts[0] == bool.TrueString);
                GizmoUtility.SetGizmoEnabled(typeof(Camera), parts.Length > 1 && parts[1] == bool.TrueString);
                EditorPrefs.DeleteKey(CameraGizmoKey);
            }

            SceneView.RepaintAll();
        }

        // UnityEditor.WindowLayout.SaveWindowLayout is internal.
        private static bool SaveLayout(string file)
        {
            var save = typeof(EditorWindow).Assembly.GetType("UnityEditor.WindowLayout")
                ?.GetMethod("SaveWindowLayout", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(string) }, null);
            if (save == null)
            {
                Debug.LogError("Level editor: this Unity version has no WindowLayout.SaveWindowLayout; layouts are not switched.");
                return false;
            }

            save.Invoke(null, new object[] { Path.GetFullPath(file) });
            return true;
        }
    }

    // Docks an editor window as a new column on the right edge of another window's main window, as dragging its tab to
    // that edge would. Unity has no public API for this, so it drives the internal SplitView root drop through reflection;
    // false when that is not possible in this Unity version.
    internal static class WindowDocking
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                         BindingFlags.NonPublic;
        // Inside the root drop zone, which is a thin band along the main window's edge.
        private const float EdgeInset = 8f;

        // Both windows are in the same main window but in different panels (not tabs of one panel, not floating apart).
        public static bool IsBeside(EditorWindow a, EditorWindow b)
        {
            var parentField = typeof(EditorWindow).GetField("m_Parent", Any);
            var parentA = parentField?.GetValue(a);
            var parentB = parentField?.GetValue(b);
            if (parentA == null || parentB == null || ReferenceEquals(parentA, parentB)) return false;
            var windowA = parentA.GetType().GetProperty("window", Any)?.GetValue(parentA);
            var windowB = parentB.GetType().GetProperty("window", Any)?.GetValue(parentB);
            return windowA != null && ReferenceEquals(windowA, windowB);
        }

        // Gives every panel in the window's row (or column) the same size; Unity opens a new docked column narrow.
        public static void SplitEvenly(EditorWindow window)
        {
            try
            {
                var panel = typeof(EditorWindow).GetField("m_Parent", Any)?.GetValue(window);
                var split = panel?.GetType().GetProperty("parent", Any)?.GetValue(panel);
                if (!(split?.GetType().GetProperty("children", Any)?.GetValue(split) is Array children) || children.Length < 2) return;
                var position = split.GetType().GetProperty("position", Any);
                var childPosition = children.GetValue(0).GetType().GetProperty("position", Any);
                var vertical = (bool)(split.GetType().GetField("vertical", Any)?.GetValue(split) ?? false);
                var total = (Rect)position.GetValue(split);
                var step = (vertical ? total.height : total.width) / children.Length;
                for (var i = 0; i < children.Length; i++)
                {
                    var rect = vertical
                        ? new Rect(0f, Mathf.Round(i * step), total.width, Mathf.Round(step))
                        : new Rect(Mathf.Round(i * step), 0f, Mathf.Round(step), total.height);
                    childPosition.SetValue(children.GetValue(i), rect);
                }

                split.GetType().GetMethod("SetupSplitter", Any)?.Invoke(split, null);
                split.GetType().GetMethod("Reflow", Any, null, Type.EmptyTypes, null)?.Invoke(split, null);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Level editor: could not even out the panels: {e.InnerException?.Message ?? e.Message}");
            }
        }

        public static bool DockAtRight(EditorWindow anchor, EditorWindow window)
        {
            try
            {
                var assembly = typeof(EditorWindow).Assembly;
                var parentField = typeof(EditorWindow).GetField("m_Parent", Any);
                var parent = parentField?.GetValue(anchor);
                var container = parent?.GetType().GetProperty("window", Any)?.GetValue(parent);
                var splitView = container?.GetType().GetProperty("rootSplitView", Any)?.GetValue(container);
                var containerRect = container?.GetType().GetProperty("position", Any)?.GetValue(container);
                if (splitView == null || !(containerRect is Rect rect)) return false;

                var point = new Vector2(rect.xMax - EdgeInset, rect.center.y);
                var dropInfo = splitView.GetType().GetMethod("DragOverRootView", Any)?.Invoke(splitView, new object[] { point });
                if (dropInfo?.GetType().GetField("userData", Any)?.GetValue(dropInfo) == null) return false;

                assembly.GetType("UnityEditor.DockArea")?.GetField("s_OriginalDragSource", Any)
                    ?.SetValue(null, parentField.GetValue(window));
                var performDrop = splitView.GetType().GetMethod("PerformDrop", Any, null,
                    new[] { typeof(EditorWindow), dropInfo.GetType(), typeof(Vector2) }, null);
                return performDrop != null && (bool)performDrop.Invoke(splitView, new[] { window, dropInfo, (object)point });
            }
            catch (Exception e)
            {
                var reason = e.InnerException?.Message ?? e.Message;
                Debug.LogWarning($"Level editor: could not dock {window.titleContent.text}: {reason}");
                return false;
            }
        }
    }
}
