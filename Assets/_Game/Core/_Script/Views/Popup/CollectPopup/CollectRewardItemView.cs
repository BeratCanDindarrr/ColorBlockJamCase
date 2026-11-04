using ColorBlockJam.Core.Model;
using ColorBlockJam.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Views
{
    // One reward in the collect popup: the currency's picture and how much of it.
    public class CollectRewardItemView : BaseView
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI countLabel;

        // Where the reward's fly animation starts.
        public Vector3 IconPosition => icon.transform.position;

        public void Set(CurrencyData reward, Sprite sprite)
        {
            icon.sprite = sprite;
            countLabel.SetText("x" + CurrencyText.Amount(reward.currencyType, reward.currencyValue));
        }
    }
}
