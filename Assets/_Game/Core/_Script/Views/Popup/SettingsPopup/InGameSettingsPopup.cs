using ColorBlockJam.Core.Controller;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    // Gameplay settings: the same switches plus quitting the level (straight to Level Failed).
    public class InGameSettingsPopup : BasePopup<InGameSettingsPopupParameters>
    {
        [SerializeField] private SettingsToggleButtonView musicToggle;
        [SerializeField] private SettingsToggleButtonView soundToggle;
        [SerializeField] private SettingsToggleButtonView hapticToggle;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button closeButton;

        private SettingsController _settingsController;
        private ApplicationController _applicationController;

        [Inject]
        public void Construct(SettingsController settingsController, ApplicationController applicationController)
        {
            _settingsController = settingsController;
            _applicationController = applicationController;
        }

        public override void ShowPopup()
        {
            musicToggle.Init(_settingsController.Model.music, _settingsController.SetMusic);
            soundToggle.Init(_settingsController.Model.sound, _settingsController.SetSound);
            hapticToggle.Init(_settingsController.Model.haptic, _settingsController.SetHaptic);
            quitButton.onClick.AddListener(QuitButtonClicked);
            closeButton.onClick.AddListener(ClosePopup);
        }

        protected override void Dispose()
        {
            musicToggle.Dispose();
            soundToggle.Dispose();
            hapticToggle.Dispose();
            quitButton.onClick.RemoveListener(QuitButtonClicked);
            closeButton.onClick.RemoveListener(ClosePopup);
        }

        private void QuitButtonClicked()
        {
            ClosePopup();
            _applicationController.QuitLevel();
        }
    }
}
