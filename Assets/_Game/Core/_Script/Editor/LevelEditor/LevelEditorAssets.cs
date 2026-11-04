using System.Collections.Generic;
using System.Linq;
using ColorBlockJam.Core.Level;
using ColorBlockJam.Core.Settings;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Finds and creates level assets. Levels come in LevelSettings play order, then any LevelData asset it misses.
    internal static class LevelEditorAssets
    {
        private const string LevelFolder = "Assets/_Game/Core/Settings/Levels";

        // The level bar asks for these on every GUI event; AssetDatabase searches are kept until the project changes.
        private static LevelSettings _levelSettings;
        private static List<LevelData> _allLevels;

        static LevelEditorAssets() => EditorApplication.projectChanged += ClearCache;

        private static void ClearCache()
        {
            _levelSettings = null;
            _allLevels = null;
        }

        public static LevelSettings FindLevelSettings()
        {
            if (_levelSettings != null) return _levelSettings;
            var guid = AssetDatabase.FindAssets("t:" + nameof(LevelSettings)).FirstOrDefault();
            _levelSettings = guid == null ? null : AssetDatabase.LoadAssetAtPath<LevelSettings>(AssetDatabase.GUIDToAssetPath(guid));
            return _levelSettings;
        }

        public static List<LevelData> FindLevels()
        {
            var settings = FindLevelSettings();
            var levels = settings != null && settings.Levels != null
                ? settings.Levels.Where(l => l != null).Distinct().ToList()
                : new List<LevelData>();

            if (_allLevels == null || _allLevels.Any(l => l == null))
                _allLevels = AssetDatabase.FindAssets("t:" + nameof(LevelData))
                    .Select(guid => AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid)))
                    .Where(l => l != null).ToList();

            levels.AddRange(_allLevels.Where(l => !levels.Contains(l)));
            return levels;
        }

        // Index the game plays this level at, or -1 when LevelSettings does not list it.
        public static int PlayIndexOf(LevelData level)
        {
            var settings = FindLevelSettings();
            return settings == null || settings.Levels == null ? -1 : System.Array.IndexOf(settings.Levels, level);
        }

        // A new empty level at the end of LevelSettings.
        public static LevelData CreateLevel()
        {
            var settings = FindLevelSettings();
            var number = (settings?.LevelCount ?? 0) + 1;
            var path = AssetDatabase.GenerateUniqueAssetPath($"{LevelFolder}/Level_{number}.asset");

            var level = ScriptableObject.CreateInstance<LevelData>();
            level.Blocks = new BlockData[0];
            level.Gates = new GateData[0];
            AssetDatabase.CreateAsset(level, path);
            ClearCache();

            if (settings != null)
            {
                Undo.RecordObject(settings, "Add Level");
                settings.Levels = (settings.Levels ?? new LevelData[0]).Append(level).ToArray();
                EditorUtility.SetDirty(settings);
            }

            AssetDatabase.SaveAssets();
            return level;
        }

        [MenuItem("Tools/Level Editor/Check All Levels")]
        public static void CheckAllLevels()
        {
            var errors = 0;
            foreach (var level in FindLevels())
            {
                foreach (var issue in LevelValidator.Validate(level.Cols, level.Rows, level.TimeLimitSec, level.Blocks, level.Gates,
                             level.Holes))
                {
                    if (issue.IsError)
                    {
                        errors++;
                        Debug.LogError($"{level.name}: {issue.Message}", level);
                    }
                    else
                    {
                        Debug.LogWarning($"{level.name}: {issue.Message}", level);
                    }
                }
            }

            Debug.Log($"Level check done: {errors} error(s) in {FindLevels().Count} level(s)");
        }
    }
}
