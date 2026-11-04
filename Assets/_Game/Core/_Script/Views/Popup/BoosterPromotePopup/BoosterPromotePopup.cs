using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Views
{
    // yarm_stack's BoosterPromotePopup: the new booster's picture and what it does; closing it starts its tutorial.
    public class BoosterPromotePopup : BasePopup<BoosterPromotePopupParameters>
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI infoText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button closeButton;

        public override void ShowPopup()
        {
            iconImage.sprite = Parameters.Icon;
            titleText.text = Parameters.Title;
            infoText.text = Parameters.Info;
            continueButton.onClick.AddListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        protected override void Dispose()
        {
            continueButton.onClick.RemoveListener(Close);
            closeButton.onClick.RemoveListener(Close);
        }

        private void Close()
        {
            continueButton.interactable = false;
            closeButton.interactable = false;
            ClosePopup();
            Parameters.OnCloseAction?.Invoke();
        }
    }
}
