using ColorBlockJam.Core.Controller;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    // HUD shown on top of the gameplay scene.
    public class GamePlayScreenView : BaseView
    {
        [SerializeField] private Button homeButton;

        private SceneLoaderController _sceneLoaderController;

        [Inject]
        public void Construct(SceneLoaderController sceneLoaderController)
        {
            _sceneLoaderController = sceneLoaderController;
        }

        public void Init()
        {
            homeButton.onClick.AddListener(OnHomeClicked);
        }

        public override void Dispose()
        {
            if (homeButton != null) homeButton.onClick.RemoveListener(OnHomeClicked);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        private void OnHomeClicked()
        {
            _sceneLoaderController.CloseGamePlayScene().Forget();
        }
    }
}
