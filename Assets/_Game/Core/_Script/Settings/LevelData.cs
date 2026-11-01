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
        // Board cells taken out of the grid: nothing stands or slides there, and the walls follow the board's shape.
        public CellCoord[] Holes;

        private void OnValidate()
        {
            Cols = Mathf.Max(1, Cols);
            Rows = Mathf.Max(1, Rows);
            TimeLimitSec = Mathf.Max(0f, TimeLimitSec);
        }
    }
}
