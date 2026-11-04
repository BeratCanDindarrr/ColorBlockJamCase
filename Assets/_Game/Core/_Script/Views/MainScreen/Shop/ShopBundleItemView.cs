using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Views
{
    // A non-coin reward inside a bundle: its icon with the amount under it.
    public class ShopBundleItemView : BaseView
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI amountText;

        public void Set(Sprite sprite, string amount)
        {
            icon.sprite = sprite;
            amountText.SetText(amount);
        }
    }
}
