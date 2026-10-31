using ColorBlockJam.Core.Controller;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    // Main menu settings: music, sound and haptic switches.
    // TODO: knit_Out also has Support (mail), Terms and Privacy links here.
    public class MenuSettingsPopup : BasePopup<MenuSettingsPopupParameters>
    {
        [SerializeField] private SettingsToggleButtonView musicToggle;
        [SerializeField] private SettingsToggleButtonView soundToggle;
        [SerializeField] private SettingsToggleButtonView hapticToggle;
        [SerializeField] private Button closeButton;

        private SettingsController _settingsController;

        [Inject]
        public void Construct(SettingsController settingsController)
        {
            _settingsController = settingsController;
        }

        public override void ShowPopup()
        {
            musicToggle.Init(_settingsController.Model.music, _settingsController.SetMusic);
            soundToggle.Init(_settingsController.Model.sound, _settingsController.SetSound);
            hapticToggle.Init(_settingsController.Model.haptic, _settingsController.SetHaptic);
            closeButton.onClick.AddListener(ClosePopup);
        }

        protected override void Dispose()
        {
            musicToggle.Dispose();
            soundToggle.Dispose();
            hapticToggle.Dispose();
            closeButton.onClick.RemoveListener(ClosePopup);
        }
    }
}
