using ColorBlockJam.Core.Level;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Editor drawing colours. Block colours mirror BoardSettings' defaults; BoardSettings lives in SandBox, which
    // Core code cannot see.
    internal static class LevelEditorStyle
    {
        public static readonly Color Background = new Color(0.17f, 0.17f, 0.19f);
        public static readonly Color Wall = new Color(0.26f, 0.27f, 0.30f);
        public static readonly Color Cell = new Color(0.36f, 0.38f, 0.42f);
        public static readonly Color WallSlotHover = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color Selection = Color.white;
        public static readonly Color Error = new Color(1f, 0.25f, 0.25f);
        // "+" places where the picked block or gate can grow.
        public static readonly Color Grow = new Color(0.20f, 0.72f, 0.30f, 0.9f);

        public static Color Of(BlockColor color)
        {
            switch (color)
            {
                case BlockColor.Red: return new Color(0.90f, 0.22f, 0.21f);
                case BlockColor.Blue: return new Color(0.20f, 0.45f, 0.90f);
                case BlockColor.Green: return new Color(0.30f, 0.75f, 0.30f);
                case BlockColor.Yellow: return new Color(0.98f, 0.82f, 0.20f);
                case BlockColor.Purple: return new Color(0.58f, 0.32f, 0.80f);
                case BlockColor.Orange: return new Color(0.98f, 0.55f, 0.15f);
                case BlockColor.Pink: return new Color(0.96f, 0.45f, 0.70f);
                case BlockColor.Cyan: return new Color(0.25f, 0.82f, 0.88f);
                default: return Color.magenta;
            }
        }
    }
}
