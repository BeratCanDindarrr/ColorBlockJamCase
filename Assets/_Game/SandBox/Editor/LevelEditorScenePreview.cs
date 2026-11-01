using System;
using System.Linq;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.LevelEditor;
using ColorBlockJam.SandBox.Board;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ColorBlockJam.SandBox.Editor
{
    // Shows the level being edited in the editor scene, built by the game's own BoardViewController from the game's
    // prefabs, with a camera framed like the game's. Rebuilt on every edit and never saved with the scene.
    [InitializeOnLoad]
    internal static class LevelEditorScenePreview
    {
        private const string EditorScenePath = "Assets/Scenes/EditorScene.unity";
        private const string RootName = "_BoardPreview";
        // Same as the gameplay scene's camera and light.
        private const float CameraFieldOfView = 60f;
        private static readonly Vector3 LightEuler = new Vector3(50f, 330f, 0f);

        private static GameObject _root;
        private static LevelData _alignedFor;

        // The board the preview built, for SceneView editing; null when no preview is shown.
        public static BoardViewController Board { get; private set; }
        public static BoardSettings Settings { get; private set; }

        static LevelEditorScenePreview()
        {
            LevelEditorWindow.WorkingLevelChanged += Rebuild;
            EditorSceneManager.activeSceneChangedInEditMode += (_, __) => Rebuild(LevelEditorWindow.CurrentWorking);
            EditorApplication.playModeStateChanged += change =>
            {
                if (change == PlayModeStateChange.ExitingEditMode) Clear();
                if (change == PlayModeStateChange.EnteredEditMode) Rebuild(LevelEditorWindow.CurrentWorking);
            };
            // References die with the domain; a preview left behind would be found only by name.
            AssemblyReloadEvents.beforeAssemblyReload += Clear;
            EditorApplication.delayCall += () => Rebuild(LevelEditorWindow.CurrentWorking);
        }

        [MenuItem("Tools/Level Editor/Frame Scene Preview")]
        private static void FrameSceneViewMenu() => FrameSceneView();

        // Looks at the board through the game camera's eyes. False when there is no scene view or preview yet.
        private static bool FrameSceneView()
        {
            var camera = _root != null ? _root.GetComponentInChildren<Camera>() : null;
            var sceneView = SceneView.lastActiveSceneView;
            if (camera == null || sceneView == null) return false;

            sceneView.in2DMode = false;
            sceneView.AlignViewToObject(camera.transform);
            sceneView.Repaint();
            return true;
        }

        private static void Rebuild(LevelData level)
        {
            Clear();
            if (level == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SceneManager.GetActiveScene().path != EditorScenePath) return;

            var settings = LoadSettings<BoardSettings>();
            if (settings == null) return;
            Settings = settings;

            try
            {
                Build(level, settings);
            }
            catch (Exception e)
            {
                // A half-edited level must never break the editor; the window lists what is wrong with it.
                Debug.LogWarning($"Level preview could not be built: {e.Message}");
                Clear();
                return;
            }

            // Framed once per opened level, so orbiting the view is kept while editing.
            if (_alignedFor != level && FrameSceneView()) _alignedFor = level;

            SceneView.RepaintAll();
        }

        private static void Build(LevelData level, BoardSettings settings)
        {
            _root = NewChild(RootName, null);

            var light = NewChild("Directional Light", _root.transform).AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(LightEuler);

            var cameraObject = NewChild("BoardPreviewCamera", _root.transform);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = CameraFieldOfView;
            var cameraView = cameraObject.AddComponent<BoardCameraView>();
            cameraView.Construct(LoadSettings<LevelCameraSettings>());
            var cameraFields = new SerializedObject(cameraView);
            cameraFields.FindProperty("gamePlayCamera").objectReferenceValue = camera;
            cameraFields.ApplyModifiedPropertiesWithoutUndo();

            var boardObject = NewChild("BoardView", _root.transform);
            var board = boardObject.AddComponent<BoardViewController>();
            var parent = boardObject.transform;
            var piecePool = new EditorInstantiatePool<BlockPieceView>(settings.BlockPiecePrefab, parent);
            board.Construct(settings, cameraView,
                new EditorInstantiatePool<FloorTileView>(settings.FloorTilePrefab, parent),
                new EditorInstantiatePool<WallPieceView>(settings.WallPiecePrefab, parent),
                new EditorInstantiatePool<GateView>(settings.GatePrefab, parent),
                new EditorInstantiatePool<BlockView>(settings.BlockPrefab, parent, view => view.Construct(piecePool, settings)));

            board.Build(new BoardSimulation(LevelDefinitionFactory.Create(level), settings.ToRules()));
            Board = board;

            foreach (var child in _root.GetComponentsInChildren<Transform>(true))
                child.gameObject.hideFlags = HideFlags.DontSave;
        }

        private static void Clear()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            _root = null;
            Board = null;

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects().Where(r => r.name == RootName))
                Object.DestroyImmediate(root);
        }

        private static GameObject NewChild(string name, Transform parent)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static T LoadSettings<T>() where T : ScriptableObject
        {
            var guid = AssetDatabase.FindAssets("t:" + typeof(T).Name).FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}
