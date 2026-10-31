using UnityEditor;
using UnityEditor.SceneManagement;

namespace ColorBlockJam.LevelEditor
{
    // Opening the editor scene opens the level editor with it.
    [InitializeOnLoad]
    internal static class LevelEditorSceneHook
    {
        private const string EditorScenePath = "Assets/Scenes/EditorScene.unity";

        static LevelEditorSceneHook()
        {
            EditorSceneManager.sceneOpened += (scene, _) =>
            {
                if (scene.path == EditorScenePath) LevelEditorWindow.Open();
            };
        }
    }
}
