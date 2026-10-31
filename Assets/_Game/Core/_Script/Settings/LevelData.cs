using ColorBlockJam.Const;
using ColorBlockJam.Core.Level;
using UnityEngine;

namespace ColorBlockJam.Core.Settings
{
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(LevelData), fileName = nameof(LevelData))]
    public class LevelData : ScriptableObject
    {
        public int Cols = 6;
        public int Rows = 6;
        public float TimeLimitSec = 120f;
        public BlockData[] Blocks;
        public GateData[] Gates;

        private void OnValidate()
        {
            Cols = Mathf.Max(1, Cols);
            Rows = Mathf.Max(1, Rows);
            TimeLimitSec = Mathf.Max(0f, TimeLimitSec);
        }
    }
}
