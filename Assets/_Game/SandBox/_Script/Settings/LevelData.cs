using ColorBlockJam.Const;
using ColorBlockJam.SandBox.Board;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(LevelData), fileName = nameof(LevelData))]
    public class LevelData : ScriptableObject
    {
        public int Cols = 6;
        public int Rows = 6;
        public float TimeLimitSec = 120f;
        public BlockData[] Blocks;
        public GateData[] Gates;

        public BoardLevelDefinition CreateBoardDefinition() =>
            new BoardLevelDefinition(Cols, Rows, TimeLimitSec, Blocks, Gates);

        private void OnValidate()
        {
            Cols = Mathf.Max(1, Cols);
            Rows = Mathf.Max(1, Rows);
            TimeLimitSec = Mathf.Max(0f, TimeLimitSec);
        }
    }
}
