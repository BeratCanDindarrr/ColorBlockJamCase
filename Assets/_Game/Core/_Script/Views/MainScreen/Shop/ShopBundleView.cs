using System;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using ColorBlockJam.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Views
{
    // One bundle on the shop page: its coins on the left, everything else (limitless life, boosters) in the box on the
    // right, the name and the price button along the bottom.
    public class ShopBundleView : BaseView
    {
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private Transform itemRoot;
        [SerializeField] private ShopBundleItemView itemTemplate;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button buyButton;

        public void Set(ShopSettings.ShopOffer offer, CurrencySettings currencySettings,
            Action<ShopSettings.ShopOffer> bought)
        {
            itemTemplate.gameObject.SetActive(false);
            long coins = 0;
            foreach (var reward in offer.Rewards)
            {
                if (reward.currencyType == CurrencyType.Coin)
                {
                    coins += reward.currencyValue;
                    continue;
                }

                var item = Instantiate(itemTemplate, itemRoot);
                item.gameObject.SetActive(true);
                item.Set(currencySettings.IconOf(reward.currencyType), RewardText(reward.currencyType, reward.currencyValue));
            }

            coinText.SetText(CurrencyText.Grouped(coins));
            titleText.SetText(offer.Title);
            priceText.SetText(offer.Price);
            buyButton.onClick.AddListener(() => bought(offer));
        }

        public override void Dispose() => buyButton.onClick.RemoveAllListeners();

        // Limitless life reads as its time ("1h"), boosters as a count ("x2").
        private static string RewardText(CurrencyType type, long value) =>
            type == CurrencyType.Time ? CurrencyText.Amount(type, value) : $"x{value}";
    }
}
