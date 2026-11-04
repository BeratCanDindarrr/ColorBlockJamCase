using System.Linq;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Boosters tab of the Level Order panel: per booster the level it unlocks on (its tutorial runs there), its picture
    // and the promote popup's title and description. Written straight to BoosterSettings, and the picture to the
    // booster's CurrencySettings icon (what the promote popup and reward lists show); every change is saved at once.
    internal sealed class LevelEditorBoosterTab
    {
        private const string BoostersField = "Boosters";
        private const string IconsField = "Icons";
        private const float IconSize = 56f;
        private const float DescriptionHeight = 44f;

        private SerializedObject _boosters;
        private SerializedObject _currencies;
        private Vector2 _scroll;

        public void Draw(float maxHeight)
        {
            var boosterSettings = FindAsset<BoosterSettings>();
            if (boosterSettings == null)
            {
                EditorGUILayout.HelpBox("No BoosterSettings asset found.", MessageType.Warning);
                return;
            }

            var currencySettings = FindAsset<CurrencySettings>();
            if (_boosters == null || _boosters.targetObject != boosterSettings) _boosters = new SerializedObject(boosterSettings);
            if (currencySettings != null && (_currencies == null || _currencies.targetObject != currencySettings))
                _currencies = new SerializedObject(currencySettings);
            _boosters.UpdateIfRequiredOrScript();
            _currencies?.UpdateIfRequiredOrScript();

            var levels = LevelEditorAssets.FindLevelSettings()?.Levels;
            var boosters = _boosters.FindProperty(BoostersField);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(maxHeight));
            for (var i = 0; i < boosters.arraySize; i++) DrawBooster(boosters.GetArrayElementAtIndex(i), levels);
            EditorGUILayout.EndScrollView();

            if (_boosters.ApplyModifiedProperties()) AssetDatabase.SaveAssetIfDirty(boosterSettings);
            if (_currencies != null && _currencies.ApplyModifiedProperties()) AssetDatabase.SaveAssetIfDirty(currencySettings);
        }

        private void DrawBooster(SerializedProperty booster, LevelData[] levels)
        {
            var type = (CurrencyType)booster.FindPropertyRelative("Type").intValue;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            DrawIcon(type);
            EditorGUILayout.BeginVertical();
            GUILayout.Label(type.ToString(), EditorStyles.boldLabel);
            var unlock = booster.FindPropertyRelative("UnlockLevel");
            unlock.intValue = Mathf.Max(1, EditorGUILayout.IntField(
                new GUIContent("Unlock level", "The level number it opens on; its tutorial runs there"), unlock.intValue));
            GUILayout.Label(LevelHint(unlock.intValue, levels), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(booster.FindPropertyRelative("TutorialTitle"), new GUIContent("Title"));
            var info = booster.FindPropertyRelative("TutorialInfo");
            GUILayout.Label("Description", EditorStyles.miniBoldLabel);
            info.stringValue = EditorGUILayout.TextArea(info.stringValue, EditorStyles.textArea,
                GUILayout.MinHeight(DescriptionHeight));

            EditorGUILayout.EndVertical();
        }

        // The booster's picture; a booster CurrencySettings has no icon entry for gets one.
        private void DrawIcon(CurrencyType type)
        {
            var size = new[] { GUILayout.Width(IconSize), GUILayout.Height(IconSize) };
            if (_currencies == null)
            {
                GUILayout.Label("No\nCurrency\nSettings", EditorStyles.centeredGreyMiniLabel, size);
                return;
            }

            var icon = IconOf(type);
            if (icon == null)
            {
                if (GUILayout.Button(new GUIContent("+ Icon", "Give this booster a picture"), size)) icon = AddIcon(type);
                if (icon == null) return;
            }

            icon.objectReferenceValue = EditorGUILayout.ObjectField(icon.objectReferenceValue, typeof(Sprite), false, size);
        }

        private SerializedProperty IconOf(CurrencyType type)
        {
            var icons = _currencies.FindProperty(IconsField);
            for (var i = 0; i < icons.arraySize; i++)
            {
                var entry = icons.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("Type").intValue == (int)type) return entry.FindPropertyRelative("Icon");
            }

            return null;
        }

        private SerializedProperty AddIcon(CurrencyType type)
        {
            var icons = _currencies.FindProperty(IconsField);
            icons.arraySize++;
            var entry = icons.GetArrayElementAtIndex(icons.arraySize - 1);
            entry.FindPropertyRelative("Type").intValue = (int)type;
            entry.FindPropertyRelative("Icon").objectReferenceValue = null;
            return entry.FindPropertyRelative("Icon");
        }

        // Which level asset that number is today (the game plays LevelSettings in order).
        private static string LevelHint(int number, LevelData[] levels)
        {
            if (levels == null || levels.Length == 0) return "No levels in LevelSettings";
            if (number > levels.Length) return $"Past the last level ({levels.Length})";
            var level = levels[number - 1];
            return level != null ? $"= {level.name}" : "(missing level)";
        }

        private static T FindAsset<T>() where T : Object
        {
            var guid = AssetDatabase.FindAssets("t:" + typeof(T).Name).FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}
