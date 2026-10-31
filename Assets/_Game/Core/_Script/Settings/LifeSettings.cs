using ColorBlockJam.Const;
using UnityEngine;

namespace ColorBlockJam.Core.Settings
{
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(LifeSettings), fileName = nameof(LifeSettings))]
    public class LifeSettings : ScriptableObject
    {
        public int MaxLife = 5;
        // Seconds to refill one life.
        public int LifeRefillTime = 1800;
        // Coin price of a full refill in MoreLivesPopup.
        public int RefillCoinCost = 900;

        private void OnValidate()
        {
            MaxLife = Mathf.Max(1, MaxLife);
            // LifeController divides by it.
            LifeRefillTime = Mathf.Max(1, LifeRefillTime);
            RefillCoinCost = Mathf.Max(0, RefillCoinCost);
        }
    }
}
