using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ColorBlockJam.Editor
{
    // Adds scene shortcut buttons to the left of the Play/Pause/Step controls in the main toolbar.
    // shortcut: reaches into internal UnityEditor.Toolbar via reflection; re-check after Unity upgrades.
    [InitializeOnLoad]
    public static class ToolbarSceneSwitcher
    {
        private const string ContainerName = "SceneSwitcherContainer";
        private const string MainScenePath = "Assets/Scenes/SampleScene.unity";
        private const string GamePlayScenePath = "Assets/Scenes/GamePlayScene.unity";
        private const string EditorScenePath = "Assets/Scenes/EditorScene.unity";

        private static VisualElement _container;

        static ToolbarSceneSwitcher()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        // The toolbar is rebuilt on layout changes, so keep checking that our buttons are still attached.
        private static void OnUpdate()
        {
            if (_container != null && _container.panel != null) return;

            var visualTree = GetToolbarRoot();
            if (visualTree == null) return;

            _container = visualTree.Q(ContainerName) ?? CreateContainer();
            if (_container.parent != null) return;

            var leftZone = visualTree.Q("ToolbarZoneLeftAlign") ?? visualTree.Q(className: "unity-toolbar__left");
            (leftZone ?? visualTree).Add(_container);
        }

        private static VisualElement GetToolbarRoot()
        {
            var editorAssembly = typeof(EditorWindow).Assembly;
            var toolbarType = editorAssembly.GetType("UnityEditor.Toolbar");
            var guiViewType = editorAssembly.GetType("UnityEditor.GUIView");
            var backendType = editorAssembly.GetType("UnityEditor.IWindowBackend");
            if (toolbarType == null || guiViewType == null || backendType == null) return null;

            var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
            if (toolbars.Length == 0) return null;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var backend = guiViewType.GetProperty("windowBackend", flags)?.GetValue(toolbars[0]);
            if (backend == null) return null;

            return backendType.GetProperty("visualTree", flags)?.GetValue(backend) as VisualElement;
        }

        private static VisualElement CreateContainer()
        {
            var container = new VisualElement { name = ContainerName };
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.marginLeft = 10;
            container.style.marginRight = 10;

            var label = new Label("Scenes:");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginRight = 5;
            label.style.color = new StyleColor(new Color(0.85f, 0.85f, 0.85f));
            container.Add(label);

            AddSceneButton(container, "Main", MainScenePath, "#3498db", "d_SceneAsset Icon");
            AddSceneButton(container, "Gameplay", GamePlayScenePath, "#f1c40f", "d_UnityEditor.GameView");
            AddSceneButton(container, "Editor", EditorScenePath, "#2ecc71", "d_Settings");
            return container;
        }

        private static void AddSceneButton(VisualElement parent, string title, string scenePath, string colorHex, string iconName)
        {
            var button = new ToolbarButton(() => OpenScene(scenePath)) { tooltip = $"Switch to {title} Scene" };
            button.style.flexDirection = FlexDirection.Row;
            button.style.alignItems = Align.Center;
            button.style.height = 18;
            button.style.marginLeft = 3;
            button.style.marginRight = 3;
            button.style.paddingLeft = 4;
            button.style.paddingRight = 6;
            button.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
            button.style.borderTopLeftRadius = 3;
            button.style.borderTopRightRadius = 3;
            button.style.borderBottomLeftRadius = 3;
            button.style.borderBottomRightRadius = 3;

            var icon = EditorGUIUtility.IconContent(iconName)?.image as Texture2D;
            if (icon != null)
            {
                var iconElement = new VisualElement();
                iconElement.style.backgroundImage = new StyleBackground(icon);
                iconElement.style.width = 13;
                iconElement.style.height = 13;
                iconElement.style.marginRight = 4;
                iconElement.style.flexShrink = 0;
                button.Add(iconElement);
            }

            var label = new Label(title);
            label.style.fontSize = 11;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            if (ColorUtility.TryParseHtmlString(colorHex, out var color)) label.style.color = new StyleColor(color);
            button.Add(label);

            parent.Add(button);
        }

        private static void OpenScene(string scenePath)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Exit Play Mode before switching scenes.");
                return;
            }
            if (EditorSceneManager.GetActiveScene().path == scenePath) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                Debug.LogError($"Scene not found: {scenePath}");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }
    }
}
