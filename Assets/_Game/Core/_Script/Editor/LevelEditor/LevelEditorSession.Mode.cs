using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Add (+) or erase (−) for the Block, Gate and Cell tools, picked next to the tools; Shift flips it while held.
    // Block: + paints / grows, − erases cells. Gate: + adds / grows, − deletes. Cell: + puts cells back, − removes them.
    public sealed partial class LevelEditorSession
    {
        private static readonly Color EraseHover = new Color(1f, 0.25f, 0.25f, 0.45f);
        private static readonly Color RestoreHover = new Color(0.25f, 0.85f, 0.35f, 0.40f);
        private static readonly Color PlainHover = new Color(1f, 1f, 1f, 0.12f);

        // Indexed by LevelEditorTool; the Cell tool starts on removing, the others on adding.
        [SerializeField] private bool[] eraseModes = { false, false, false, true };

        internal bool HasModes => tool != LevelEditorTool.Select;

        internal bool EraseMode
        {
            get => HasModes && EraseModes[(int)tool];
            set
            {
                if (!HasModes || EraseModes[(int)tool] == value) return;
                EraseModes[(int)tool] = value;
                RaiseStateChanged();
            }
        }

        // An older session kept fewer entries; it gets the defaults back.
        private bool[] EraseModes => eraseModes != null && eraseModes.Length == 4
            ? eraseModes
            : eraseModes = new[] { false, false, false, true };

        private bool ErasesNow(bool shift) => HasModes && EraseMode != shift;

        // What the cell under the mouse is tinted: red where a press erases, green where the Cell tool puts cells back.
        public static Color HoverColor(bool shift)
        {
            var session = instance;
            if (!session.HasModes) return PlainHover;
            if (session.ErasesNow(shift)) return EraseHover;
            return session.tool == LevelEditorTool.Cell ? RestoreHover : PlainHover;
        }

        public static bool ErasesFromScene(bool shift) => instance.ErasesNow(shift);
    }
}
