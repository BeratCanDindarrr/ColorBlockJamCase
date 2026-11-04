using ColorBlockJam.Core.Level;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Editor drawing colours.
    internal static class LevelEditorStyle
    {
        public static readonly Color Background = new Color(0.17f, 0.17f, 0.19f);
        public static readonly Color Wall = new Color(0.26f, 0.27f, 0.30f);
        public static readonly Color Cell = new Color(0.36f, 0.38f, 0.42f);
        public static readonly Color Selection = Color.white;
        public static readonly Color Error = new Color(1f, 0.25f, 0.25f);
        // "+" places where the picked block or gate can grow.
        public static readonly Color Grow = new Color(0.20f, 0.72f, 0.30f, 0.9f);

        // The game's colour for a block colour (BoardSettings), so the editor and the game agree.
        public static Color Of(BlockColor color) => LevelEditorColors.Of(color);
    }
}
