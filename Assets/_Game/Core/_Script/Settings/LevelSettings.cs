using ColorBlockJam.Const;
using DG.Tweening;
using UnityEngine;

namespace ColorBlockJam.Core.Settings
{
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(LevelSettings), fileName = nameof(LevelSettings))]
    public class LevelSettings : ScriptableObject
    {
        public LevelData[] Levels;
        // After the last level, play continues this many levels back from the end.
        public int EndlessLoopCount = 5;
        // Coins given for winning a level.
        public int LevelReward = 20;
        // Pause on the won board before the win flow takes over.
        public float WinLevelDelaySeconds = 1.5f;
        // "Play On" in the out-of-time popup: its coin price and the seconds it adds.
        public int PlayOnCoinCost = 900;
        public float PlayOnSeconds = 30f;
        // The main screen's level button after a win: shrinks to this scale, takes the new number, springs back with a
        // jolt of this many degrees while a white shine flashes up to this alpha.
        public float LevelButtonShrinkScale = 0.8f;
        public float LevelButtonShrinkDuration = 0.15f;
        public float LevelButtonGrowDuration = 0.6f;
        public float LevelButtonShakeAngle = 8f;
        public float LevelButtonShineAlpha = 0.6f;
        public float LevelButtonShineDuration = 0.4f;

        // The level path above the Play button: this many round nodes, the current level's on the bottom, this far
        // apart (canvas units). After a win it slides down one node, then the new level's lock shrinks away.
        [Header("Level Path")]
        public int LevelPathVisibleNodes = 3;
        public float LevelPathSpacing = 600f;
        public float LevelPathSlideDuration = 0.5f;
        public Ease LevelPathSlideEase = Ease.InOutQuad;
        public float LevelPathLockHideDuration = 0.35f;

        public int LevelCount => Levels?.Length ?? 0;

        public LevelData GetLevel(int index)
        {
            if (Levels == null || Levels.Length == 0)
            {
                Debug.LogError($"{nameof(LevelSettings)}: no levels assigned", this);
                return null;
            }

            return Levels[Mathf.Clamp(index, 0, Levels.Length - 1)];
        }
    }
}
