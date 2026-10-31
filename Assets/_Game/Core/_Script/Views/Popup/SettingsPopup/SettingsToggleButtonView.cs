using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Views
{
    // On/off switch of the settings popups: each click flips it; while off its graphics draw in grayscale.
    public class SettingsToggleButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Graphic[] graphics;
        [SerializeField] private Material grayscaleMaterial;

        private bool _isOn;
        private Action<bool> _onChanged;

        public void Init(bool isOn, Action<bool> onChanged)
        {
            _isOn = isOn;
            _onChanged = onChanged;
            button.onClick.AddListener(OnClicked);
            UpdateLook();
        }

        public void Dispose()
        {
            button.onClick.RemoveListener(OnClicked);
            _onChanged = null;
        }

        private void OnClicked()
        {
            _isOn = !_isOn;
            _onChanged?.Invoke(_isOn);
            UpdateLook();
        }

        private void UpdateLook()
        {
            // null puts the graphic back on its default UI material.
            foreach (var graphic in graphics) graphic.material = _isOn ? null : grayscaleMaterial;
        }
    }
}
