using System;
using ColorBlockJam.Const;
using ColorBlockJam.Enums;
using ColorBlockJam.Views.Currency;
using UnityEngine;

namespace ColorBlockJam.Core.Settings
{
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(CurrencySettings), fileName = nameof(CurrencySettings))]
    public class CurrencySettings : ScriptableObject
    {
        // What a new player starts with.
        public long StartCoin = 1000;
        public long StartBoosterCount = 3;

        [Serializable]
        public struct CurrencyIcon
        {
            public CurrencyType Type;
            public Sprite Icon;
        }

        // The picture of each currency in reward lists (CollectPopup).
        public CurrencyIcon[] Icons;

        public Sprite IconOf(CurrencyType currencyType)
        {
            foreach (var entry in Icons)
            {
                if (entry.Type == currencyType) return entry.Icon;
            }

            Debug.LogError($"{nameof(CurrencySettings)}: no icon for {currencyType}", this);
            return null;
        }

        [Header("Reward fly animation")]
        public CoinCurrencyView CoinViewPrefab;
        public LifeTimeCurrencyView LifeTimeViewPrefab;
        public BoosterCurrencyView BoosterViewPrefab;
        // A coin reward shows one coin per coin, up to this many.
        public int MaxCoinViews = 10;
        public float SpawnInterval = 0.08f;
        // Canvas units around the screen centre where the icons appear.
        public float SpawnSpread = 80f;
        public float AppearDuration = 0.35f;
        // Wait after appearing before flying.
        public float FlyDelay = 0.4f;
        public float FlyDuration = 0.5f;
        // Canvas units the flight curves up and sideways.
        public float CurveHeight = 200f;
        public float CurveSide = 50f;
        public float LandScale = 0.9f;
        // The target icon's bump on each landing.
        public float TargetPunch = 0.1f;
        public float TargetPunchDuration = 0.15f;
        // The header coin label counting up to the new amount.
        public float CoinCountDuration = 0.8f;
    }
}
