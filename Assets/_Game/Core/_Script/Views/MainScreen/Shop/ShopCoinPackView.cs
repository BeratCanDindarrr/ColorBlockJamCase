using System;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using ColorBlockJam.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Views
{
    // One coin pack on the shop page: the amount, the coin picture and the price button.
    public class ShopCoinPackView : BaseView
    {
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private Transform icon;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button buyButton;

        public void Set(ShopSettings.ShopOffer offer, Action<ShopSettings.ShopOffer> bought)
        {
            long coins = 0;
            foreach (var reward in offer.Rewards)
            {
                if (reward.currencyType == CurrencyType.Coin) coins += reward.currencyValue;
            }

            amountText.SetText(CurrencyText.Grouped(coins));
            icon.localScale = Vector3.one * offer.IconScale;
            priceText.SetText(offer.Price);
            buyButton.onClick.AddListener(() => bought(offer));
        }

        public override void Dispose() => buyButton.onClick.RemoveAllListeners();
    }
}
