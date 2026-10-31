using ColorBlockJam.Const;
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
