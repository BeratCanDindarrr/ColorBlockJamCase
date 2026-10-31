using System.Collections.Generic;
using ColorBlockJam.Core.Level;
using ColorBlockJam.Core.Settings;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Copy/Paste of blocks, also between levels. Kept as text so later edits to the level do not reach the copy.
    // Gates are not copied.
    internal static class LevelEditorClipboard
    {
        private static readonly List<string> Copied = new List<string>();

        public static void Copy(LevelData level, List<int> blocks)
        {
            if (blocks.Count == 0) return;
            Copied.Clear();
            foreach (var i in blocks) Copied.Add(JsonUtility.ToJson(level.Blocks[i]));
        }

        public static List<BlockData> Paste()
        {
            var blocks = new List<BlockData>();
            foreach (var json in Copied) blocks.Add(JsonUtility.FromJson<BlockData>(json));
            return blocks;
        }
    }
}
