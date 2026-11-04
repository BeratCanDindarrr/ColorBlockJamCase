using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ColorBlockJam.Core.Level;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Block colours as the game shows them: read from BoardSettings.Colors (SandBox; reached through SerializedObject,
    // which needs no reference to it). Adding a colour writes a new BlockColor enum member and a BoardSettings entry.
    internal static class LevelEditorColors
    {
        private const string EnumFile = "Assets/_Game/Core/_Script/Level/Data/LevelTypes.cs";
        private const string GameFolder = "Assets/_Game";
        private const string SettingsType = "BoardSettings";
        private const string ColorsField = "Colors";
        private static readonly Regex Identifier = new Regex("^[A-Z][A-Za-z0-9]*$");

        // Favourite colours are what the palette shows (all when there are none); kept per user and project.
        private static string FavoritesKey => "ColorBlockJam.LevelEditor.FavoriteColors." + Application.dataPath;

        public static bool IsFavorite(BlockColor color) =>
            EditorPrefs.GetString(FavoritesKey, "").Split(',').Contains(((int)color).ToString());

        public static void SetFavorite(BlockColor color, bool favorite)
        {
            var saved = EditorPrefs.GetString(FavoritesKey, "").Split(',').Where(v => v.Length > 0).ToList();
            saved.Remove(((int)color).ToString());
            if (favorite) saved.Add(((int)color).ToString());
            EditorPrefs.SetString(FavoritesKey, string.Join(",", saved));
        }

        public static Color Of(BlockColor color)
        {
            var colors = CachedColors();
            if (colors == null) return Color.magenta;
            for (var i = 0; i < colors.arraySize; i++)
            {
                var entry = colors.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("Key").intValue == (int)color)
                    return entry.FindPropertyRelative("Value").colorValue;
            }

            return Color.magenta;
        }

        // Null when the name can be used, otherwise why not.
        public static string CheckName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Give the colour a name.";
            if (!Identifier.IsMatch(name)) return "Use letters and digits, starting with a capital (e.g. Teal).";
            if (Enum.GetNames(typeof(BlockColor)).Contains(name)) return $"{name} already exists.";
            return null;
        }

        // Appends `name` to the BlockColor enum and its colour to BoardSettings; Unity recompiles afterwards.
        public static bool Add(string name, Color color)
        {
            if (CheckName(name) != null) return false;

            var settings = FindSettings();
            if (settings == null)
            {
                EditorUtility.DisplayDialog("Level Editor", "No BoardSettings asset found for the colour.", "OK");
                return false;
            }

            var value = Enum.GetValues(typeof(BlockColor)).Cast<int>().Max() + 1;
            if (!AddEnumMember(name, value)) return false;

            var serialized = new SerializedObject(settings);
            var colors = serialized.FindProperty(ColorsField);
            colors.arraySize++;
            var entry = colors.GetArrayElementAtIndex(colors.arraySize - 1);
            entry.FindPropertyRelative("Key").intValue = value;
            entry.FindPropertyRelative("Value").colorValue = color;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(settings);

            AssetDatabase.ImportAsset(EnumFile);
            return true;
        }

        // Sets the colour BoardSettings shows for `color` (adds its entry if missing).
        public static void SetColor(BlockColor color, Color value)
        {
            var settings = FindSettings();
            if (settings == null)
            {
                EditorUtility.DisplayDialog("Level Editor", "No BoardSettings asset found for the colour.", "OK");
                return;
            }

            var serialized = new SerializedObject(settings);
            var colors = serialized.FindProperty(ColorsField);
            SerializedProperty entry = null;
            for (var i = 0; i < colors.arraySize && entry == null; i++)
            {
                var candidate = colors.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("Key").intValue == (int)color) entry = candidate;
            }

            if (entry == null)
            {
                colors.arraySize++;
                entry = colors.GetArrayElementAtIndex(colors.arraySize - 1);
                entry.FindPropertyRelative("Key").intValue = (int)color;
            }

            entry.FindPropertyRelative("Value").colorValue = value;
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(settings);
        }

        // Scripts under Assets/_Game that name `color` in code (tests, BoardSettings defaults); a rename updates them too.
        public static List<string> UsersOf(BlockColor color)
        {
            var reference = Reference(color);
            return AssetDatabase.FindAssets("t:Script", new[] { GameFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".cs") && path != EnumFile && reference.IsMatch(File.ReadAllText(path)))
                .ToList();
        }

        // Renames the BlockColor member and every `BlockColor.<old>` in the scripts that use it; levels keep the colour,
        // as they store its number. The caller refreshes the AssetDatabase once after all renames (Unity recompiles).
        public static bool Rename(BlockColor color, string name)
        {
            if (CheckName(name) != null) return false;

            var text = File.ReadAllText(EnumFile);
            var member = new Regex($@"\b{color}(\s*=\s*{(int)color}\b)");
            if (!member.IsMatch(text))
            {
                Debug.LogError($"Level editor: BlockColor.{color} not found in {EnumFile}.");
                return false;
            }

            var users = UsersOf(color);
            File.WriteAllText(EnumFile, member.Replace(text, name + "$1", 1));
            foreach (var path in users)
                File.WriteAllText(path, Reference(color).Replace(File.ReadAllText(path), "BlockColor." + name));
            return true;
        }

        private static Regex Reference(BlockColor color) => new Regex($@"\bBlockColor\.{color}\b");

        private static bool AddEnumMember(string name, int value)
        {
            var text = File.ReadAllText(EnumFile);
            var match = Regex.Match(text, @"public enum BlockColor\s*\{(?<body>[^}]*)\}");
            if (!match.Success)
            {
                Debug.LogError($"Level editor: BlockColor enum not found in {EnumFile}.");
                return false;
            }

            var body = match.Groups["body"];
            var members = body.Value.TrimEnd();
            var updated = members + $",\n        {name} = {value}\n    ";
            File.WriteAllText(EnumFile, text.Substring(0, body.Index) + updated + text.Substring(body.Index + body.Length));
            return true;
        }

        // Of runs for every block/gate/swatch on every GUI event, so the asset and its SerializedObject are kept.
        private static ScriptableObject _settings;
        private static SerializedObject _serialized;

        private static SerializedProperty CachedColors()
        {
            if (_settings == null || _serialized == null || _serialized.targetObject != _settings)
            {
                _settings = FindSettings();
                _serialized = _settings == null ? null : new SerializedObject(_settings);
            }

            if (_serialized == null) return null;
            _serialized.UpdateIfRequiredOrScript();
            return _serialized.FindProperty(ColorsField);
        }

        private static ScriptableObject FindSettings()
        {
            var guid = AssetDatabase.FindAssets("t:" + SettingsType).FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
        }

    }

    // The "+" of the colour palette: a name and a colour for a new block colour.
    internal sealed class AddColorPopup : PopupWindowContent
    {
        private string _name = "";
        private Color _color = Color.white;

        public override Vector2 GetWindowSize() => new Vector2(240f, 110f);

        public override void OnGUI(Rect rect)
        {
            GUILayout.Label("New colour", EditorStyles.boldLabel);
            _name = EditorGUILayout.TextField("Name", _name);
            _color = EditorGUILayout.ColorField("Colour", _color);

            var problem = LevelEditorColors.CheckName(_name);
            if (problem != null) GUILayout.Label(problem, EditorStyles.miniLabel);

            using (new EditorGUI.DisabledScope(problem != null))
            {
                if (GUILayout.Button("Add") && LevelEditorColors.Add(_name.Trim(), _color)) editorWindow.Close();
            }
        }
    }
}
