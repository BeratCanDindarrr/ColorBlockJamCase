using System;
using System.Collections.Generic;
using System.Linq;
using ColorBlockJam.Core.Level;
using ColorBlockJam.Core.Settings;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    public enum LevelTestVerdict
    {
        Solvable,
        Unsolvable,
        TooBig
    }

    // One drag of a solution: the block walks cell by cell along Path to (Col, Row) and, when Exits, leaves from there
    // through a gate on ExitEdge.
    public struct LevelTestStep
    {
        public string BlockId;
        public int Col;
        public int Row;
        public bool Exits;
        public CellCoord[] Path;
        public BoardEdge ExitEdge;
    }

    // What playing a level through found (filled in by the SandBox solver, see LevelSolverBridge).
    public sealed class LevelTestOutcome
    {
        public LevelTestVerdict Verdict;
        public int Drags;
        public int AsideMoves;
        public string[] StuckBlocks;
        public LevelTestStep[] Steps;
    }

    // Level test results, kept in ProjectSettings with a fingerprint of each level's content, so "Test changed" only
    // plays the levels edited since (or never tested). The solver lives in SandBox, which Core cannot see: SandBox editor
    // code sets Solver when it loads.
    [FilePath("ProjectSettings/ColorBlockJamLevelTests.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class LevelEditorTests : ScriptableSingleton<LevelEditorTests>
    {
        [Serializable]
        internal sealed class Entry
        {
            public string Guid;
            public string Hash;
            public LevelTestVerdict Verdict;
            public int Drags;
            public int AsideMoves;
            public string[] StuckBlocks;
        }

        public static Func<LevelData, LevelTestOutcome> Solver;

        [SerializeField] private List<Entry> entries = new List<Entry>();

        internal static bool CanTest => Solver != null;

        internal static Entry Find(LevelData level)
        {
            var guid = GuidOf(level);
            return instance.entries.Find(e => e.Guid == guid);
        }

        // Tested, and not changed since.
        internal static bool IsUpToDate(LevelData level)
        {
            var entry = Find(level);
            return entry != null && entry.Hash == HashOf(level);
        }

        // Plays the saved level asset through and keeps what it found (call SaveResults after a batch).
        internal static void Test(LevelData level)
        {
            if (!CanTest || level == null) return;

            var outcome = Solver(level);
            var entry = Find(level);
            if (entry == null)
            {
                entry = new Entry { Guid = GuidOf(level) };
                instance.entries.Add(entry);
            }

            entry.Hash = HashOf(level);
            entry.Verdict = outcome.Verdict;
            entry.Drags = outcome.Drags;
            entry.AsideMoves = outcome.AsideMoves;
            entry.StuckBlocks = outcome.StuckBlocks ?? Array.Empty<string>();
        }

        internal static void SaveResults()
        {
            // Results of levels that no longer exist are dropped.
            instance.entries.RemoveAll(e => string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(e.Guid)));
            instance.Save(true);
        }

        private static string GuidOf(LevelData level) => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(level));

        // Changes with anything the level holds (size, time, blocks, gates, removed cells).
        private static string HashOf(LevelData level) => Hash128.Compute(JsonUtility.ToJson(level)).ToString();

        internal static List<LevelData> Untested(IEnumerable<LevelData> levels) => levels.Where(l => !IsUpToDate(l)).ToList();
    }
}
