using ColorBlockJam.Core.Controller;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    public class MainScreenPage2View : MainScreenBasePageView
    {
        [SerializeField] private Button playButton;
        [SerializeField] private MainScreenHeaderView header;

        private SceneLoaderController _sceneLoaderController;

        [Inject]
        public void Construct(SceneLoaderController sceneLoaderController)
        {
            _sceneLoaderController = sceneLoaderController;
        }

        public override void Initialize()
        {
            playButton.onClick.AddListener(OnPlayClicked);
            header.Initialize();
        }

        public override void Dispose()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
            if (header != null) header.Dispose();
        }

        private void OnPlayClicked()
        {
            _sceneLoaderController.OpenGamePlayScene().Forget();
        }
    }
}
