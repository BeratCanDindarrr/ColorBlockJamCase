using System;
using ColorBlockJam.Const;
using ColorBlockJam.Enums;
using UnityEngine;

namespace ColorBlockJam.Core.Settings
{
    // Coin price and unlock level of each in-game booster.
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(BoosterSettings), fileName = nameof(BoosterSettings))]
    public class BoosterSettings : ScriptableObject
    {
        [Serializable]
        public struct BoosterData
        {
            public CurrencyType Type;
            // Coins for one more when none is left.
            public long Price;
            // First visual level the booster can be used on; locked ("Lvl N") before.
            public int UnlockLevel;
            // The promote popup shown before the booster's tutorial (which runs on its unlock level).
            public string TutorialTitle;
            [TextArea] public string TutorialInfo;
        }

        public BoosterData[] Boosters;
        // How long the Timer booster (Booster1) stops the level clock.
        public float TimerFreezeSeconds = 15f;

        // The tutorial hand and the dim around the booster button.
        [Header("Tutorial")]
        [Tooltip("A player without the booster is given one when its tutorial starts.")]
        public bool GrantBoosterIfNone = true;
        public Color TutorialDimColor = new Color(0f, 0f, 0f, 0.7f);
        [Tooltip("How far the clear hole around the booster button reaches past its edges.")]
        [Min(0f)] public float ButtonHolePadding = 12f;
        [Min(0f)] public float TapPressDuration = 0.3f;
        public float TapPressedScale = 0.85f;
        [Min(0f)] public float TapRepeatDelay = 0.35f;

        // The first level's tutorial: the hand drags one block along its way out to its gate.
        [Header("Drag Tutorial")]
        [Tooltip("Visual level the drag tutorial runs on, once.")]
        public int DragTutorialLevel = 1;
        [Min(0f)] public float DragHintDuration = 1.4f;
        [Min(0f)] public float DragHintFadeDuration = 0.2f;
        [Min(0f)] public float DragHintRepeatDelay = 0.4f;

        public BoosterData Get(CurrencyType type)
        {
            foreach (var booster in Boosters)
            {
                if (booster.Type == type) return booster;
            }

            Debug.LogError($"{nameof(BoosterSettings)}: no entry for {type}", this);
            return new BoosterData { Type = type, Price = long.MaxValue, UnlockLevel = int.MaxValue };
        }
    }
}
