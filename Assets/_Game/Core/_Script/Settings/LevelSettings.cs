using ColorBlockJam.Const;
using UnityEngine;

namespace ColorBlockJam.Core.Settings
{
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(LevelSettings), fileName = nameof(LevelSettings))]
    public class LevelSettings : ScriptableObject
    {
        public LevelData[] Levels;

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
