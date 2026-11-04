using System;
using ColorBlockJam.Const;
using ColorBlockJam.Core.Model;
using UnityEngine;

namespace ColorBlockJam.Core.Settings
{
    // The shop page's offers. No store yet: the price is only a label, and buying an offer hands its rewards to the
    // Collect popup.
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(ShopSettings), fileName = nameof(ShopSettings))]
    public class ShopSettings : ScriptableObject
    {
        [Serializable]
        public class ShopOffer
        {
            public string Title;
            public string Price;
            // How big the coin picture of a coin pack is drawn (1 = as in the template).
            public float IconScale = 1f;
            public CurrencyData[] Rewards;
        }

        public ShopOffer[] CoinPacks;
        public ShopOffer[] Bundles;
    }
}
