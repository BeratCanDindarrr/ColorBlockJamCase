using ColorBlockJam.Core.Controller;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    // Shown on the main menu after giving up a level: try it again (costs a life) or close.
    public class LevelFailedPopup : BasePopup<LevelFailedPopupParameters>
    {
        [SerializeField] private Button tryAgainButton;
        [SerializeField] private Button closeButton;

        private ApplicationController _applicationController;

        [Inject]
        public void Construct(ApplicationController applicationController)
        {
            _applicationController = applicationController;
        }

        public override void ShowPopup()
        {
            tryAgainButton.onClick.AddListener(TryAgainButtonClicked);
            closeButton.onClick.AddListener(ClosePopup);
        }

        protected override void Dispose()
        {
            tryAgainButton.onClick.RemoveListener(TryAgainButtonClicked);
            closeButton.onClick.RemoveListener(ClosePopup);
        }

        private void TryAgainButtonClicked()
        {
            _applicationController.PlayLevel();
            ClosePopup();
        }
    }
}
