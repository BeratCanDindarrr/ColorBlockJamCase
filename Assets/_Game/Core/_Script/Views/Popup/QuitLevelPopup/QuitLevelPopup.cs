using ColorBlockJam.Core.Controller;
using ColorBlockJam.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    // "Are you sure?" before giving up the level; backing out returns to the out-of-time popup.
    public class QuitLevelPopup : BasePopup<QuitLevelPopupParameters>
    {
        [SerializeField] private Button quitButton;
        [SerializeField] private Button closeButton;

        private ApplicationController _applicationController;
        private IPopupService _popupService;

        [Inject]
        public void Construct(ApplicationController applicationController, IPopupService popupService)
        {
            _applicationController = applicationController;
            _popupService = popupService;
        }

        public override void ShowPopup()
        {
            quitButton.onClick.AddListener(QuitButtonClicked);
            closeButton.onClick.AddListener(CloseButtonClicked);
        }

        protected override void Dispose()
        {
            quitButton.onClick.RemoveListener(QuitButtonClicked);
            closeButton.onClick.RemoveListener(CloseButtonClicked);
        }

        // TODO: show what the player loses (streak items) before the final quit.
        private void QuitButtonClicked()
        {
            _applicationController.LoseLevel();
            _popupService.AddPopup(new LevelFailedPopupParameters());
            ClosePopup();
        }

        private void CloseButtonClicked()
        {
            Parameters.OnCloseAction?.Invoke();
            ClosePopup();
        }
    }
}
