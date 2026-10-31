using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Views
{
    // Top bar of the home page: profile, coin, life and settings.
    public class MainScreenHeaderView : BaseView
    {
        [SerializeField] private Button profileButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI lifeText;

        public event Action ProfileClicked;
        public event Action SettingsClicked;

        public override void Initialize()
        {
            profileButton.onClick.AddListener(OnProfileClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        public override void Dispose()
        {
            if (profileButton != null) profileButton.onClick.RemoveListener(OnProfileClicked);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
        }

        public void SetCoin(long amount) => coinText.SetText(amount.ToString());

        // Text, so limitless life can show "∞".
        public void SetLife(string life) => lifeText.SetText(life);

        private void OnProfileClicked() => ProfileClicked?.Invoke();

        private void OnSettingsClicked() => SettingsClicked?.Invoke();
    }
}
