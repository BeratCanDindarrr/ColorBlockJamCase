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

        public static LevelSettings FindLevelSettings()
        {
            var guid = AssetDatabase.FindAssets("t:" + nameof(LevelSettings)).FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<LevelSettings>(AssetDatabase.GUIDToAssetPath(guid));
        }

        public static List<LevelData> FindLevels()
        {
            var settings = FindLevelSettings();
            var levels = settings != null && settings.Levels != null
                ? settings.Levels.Where(l => l != null).Distinct().ToList()
                : new List<LevelData>();

            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(LevelData)))
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
                if (level != null && !levels.Contains(level)) levels.Add(level);
            }

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
                foreach (var issue in LevelValidator.Validate(level.Cols, level.Rows, level.TimeLimitSec, level.Blocks, level.Gates))
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
