using ColorBlockJam.Const;
using UnityEngine;

namespace ColorBlockJam.Core.Settings
{
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(CurrencySettings), fileName = nameof(CurrencySettings))]
    public class CurrencySettings : ScriptableObject
    {
        // What a new player starts with.
        public long StartCoin = 1000;
        public long StartBoosterCount = 3;
    }
}
