using ColorBlockJam.Const;
using ColorBlockJam.Enums;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorBlockJam.Core.Controller
{
    // Loads the gameplay scene additively on top of the main scene and swaps the screens around it.
    public class SceneLoaderController
    {
        private readonly ScreenController _screenController;
        private bool _isBusy;

        public SceneLoaderController(ScreenController screenController)
        {
            _screenController = screenController;
        }

        public bool IsGamePlaySceneLoaded() => SceneManager.GetSceneByName(GlobalConst.SceneName.GamePlay).isLoaded;

        public async UniTask OpenGamePlayScene()
        {
            if (_isBusy || IsGamePlaySceneLoaded()) return;

            _isBusy = true;
            try
            {
                var operation = SceneManager.LoadSceneAsync(GlobalConst.SceneName.GamePlay, LoadSceneMode.Additive);
                if (operation == null)
                {
                    Debug.LogError($"{nameof(SceneLoaderController)}: '{GlobalConst.SceneName.GamePlay}' is not in Build Settings");
                    return;
                }
                await operation;
                _screenController.SetScreen(ScreenType.GamePlayScreen);
            }
            finally
            {
                _isBusy = false;
            }
        }

        public async UniTask CloseGamePlayScene()
        {
            if (_isBusy || !IsGamePlaySceneLoaded()) return;

            _isBusy = true;
            try
            {
                // Switch the screen before the unload finishes: the Main Camera is Depth Only, so if the
                // gameplay scene unloads first there are a few frames where nothing clears the screen.
                _screenController.SetScreen(ScreenType.MainScreen);
                await SceneManager.UnloadSceneAsync(GlobalConst.SceneName.GamePlay, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
            }
            finally
            {
                _isBusy = false;
            }
        }
    }
}
