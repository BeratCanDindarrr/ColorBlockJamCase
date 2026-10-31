using UnityEditor;
using UnityEditor.SceneManagement;

namespace ColorBlockJam.Editor
{
    // Play always starts the game from the main scene, whichever scene is open; the open scene comes back on Stop.
    [InitializeOnLoad]
    internal static class PlayModeStartScene
    {
        private const string MainScenePath = "Assets/Scenes/SampleScene.unity";

        static PlayModeStartScene()
        {
            // During the very first import the scene asset may not load yet; try again once the editor is idle.
            if (!Apply()) EditorApplication.delayCall += () => Apply();
        }

        private static bool Apply()
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
            return EditorSceneManager.playModeStartScene != null;
        }
    }
}
