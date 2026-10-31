using System;

namespace ColorBlockJam.Core.Model
{
    [Serializable]
    public class LevelModel
    {
        // Level number shown to the player; keeps growing past the last level.
        public int visualLevel;
        // 1-based level that is actually played; loops back near the end once the levels run out.
        public int currentLevel;
        // Level count when last synced, so newly added levels are picked up.
        public int lastMaxLevel;
        public int loopResetCount;
    }
}
