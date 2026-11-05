using System;
using ColorBlockJam.Core.Controller.Mock;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.MockMessenger
{
    // The mock messenger's buttons (plain IMGUI, no Odin); each section shows its inputs, the
    // current values and the actions. Buttons work in Play Mode; a failing action logs instead of breaking the Inspector.
    [CustomEditor(typeof(MockMessageController))]
    public sealed class MockMessageControllerEditor : UnityEditor.Editor
    {
        private static bool _showCurrency = true;
        private static bool _showRewards = true;
        private static bool _showLife = true;
        private static bool _showPopups = true;
        private static bool _showLevel = true;
        private static bool _showTasks = true;

        private MockMessageController Mock => (MockMessageController)target;

        // Values on screen follow the game while it runs.
        public override bool RequiresConstantRepaint() => EditorApplication.isPlaying;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var ready = EditorApplication.isPlaying && Mock.IsReady;
            if (!ready) EditorGUILayout.HelpBox("Enter Play Mode to use the buttons.", MessageType.Info);

            Section(ref _showCurrency, "Currency", ready, DrawCurrency);
            Section(ref _showRewards, "Win / Rewards", ready, DrawRewards);
            Section(ref _showLife, "Life", ready, DrawLife);
            Section(ref _showPopups, "Popups", ready, DrawPopups);
            Section(ref _showLevel, "Level", ready, DrawLevel);
            Section(ref _showTasks, "UI Tasks / Storage", ready, DrawTasks);
            serializedObject.ApplyModifiedProperties();
        }

        private static void Section(ref bool shown, string title, bool ready, Action<bool> draw)
        {
            shown = EditorGUILayout.BeginFoldoutHeaderGroup(shown, title);
            if (shown)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                draw(ready);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void Field(string name) => EditorGUILayout.PropertyField(serializedObject.FindProperty(name));

        private static void Status(bool ready, Func<string> text)
        {
            if (ready) EditorGUILayout.LabelField(text(), EditorStyles.wordWrappedMiniLabel);
        }

        private static void Button(string label, Action action, bool enabled)
        {
            using (new EditorGUI.DisabledScope(!enabled))
            {
                if (!GUILayout.Button(label)) return;
            }

            try
            {
                action();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void DrawCurrency(bool ready)
        {
            Status(ready, () => Mock.CurrencySummary);
            Field(nameof(MockMessageController.currencyType));
            Field(nameof(MockMessageController.currencyAmount));
            using (new GUILayout.HorizontalScope())
            {
                Button("Add", Mock.AddCurrency, ready);
                Button("Consume", Mock.ConsumeCurrency, ready);
                Button("Reset", Mock.ResetCurrency, ready);
            }
        }

        private void DrawRewards(bool ready)
        {
            Field(nameof(MockMessageController.rewardCoins));
            Field(nameof(MockMessageController.rewardTimeSeconds));
            Field(nameof(MockMessageController.rewardBooster1));
            var inLevel = ready && Mock.InLevel;
            using (new GUILayout.HorizontalScope())
            {
                Button("Add rewards + fly", Mock.AddRewardsAndFly, ready && !inLevel);
                Button("Collect popup", Mock.OpenCollectPopup, ready);
            }

            using (new GUILayout.HorizontalScope())
            {
                Button("Win level", Mock.WinLevel, inLevel);
                Button("Lose level", Mock.LoseLevel, inLevel);
            }
        }

        private void DrawLife(bool ready)
        {
            Status(ready, () => Mock.LifeSummary);
            using (new GUILayout.HorizontalScope())
            {
                Field(nameof(MockMessageController.limitlessSeconds));
                Button("Add limitless", Mock.AddLimitlessLife, ready);
            }

            using (new GUILayout.HorizontalScope())
            {
                Button("Give life", Mock.GiveLife, ready);
                Button("Remove life", Mock.RemoveLife, ready);
                Button("Refill", Mock.RefillLives, ready);
                Button("Reset", Mock.ResetLives, ready);
            }
        }

        private void DrawPopups(bool ready)
        {
            var inLevel = ready && Mock.InLevel;
            using (new GUILayout.HorizontalScope())
            {
                Button("More Lives", Mock.OpenMoreLivesPopup, ready);
                Button("Out Of Time", Mock.OpenOutOfTimePopup, inLevel);
                Button("Level Failed", Mock.OpenLevelFailedPopup, ready);
            }

            using (new GUILayout.HorizontalScope())
            {
                Button("Menu Settings", Mock.OpenMenuSettingsPopup, ready);
                Button("In-Game Settings", Mock.OpenInGameSettingsPopup, inLevel);
            }
        }

        private void DrawLevel(bool ready)
        {
            Status(ready, () => Mock.LevelSummary);
            var inLevel = ready && Mock.InLevel;
            using (new GUILayout.HorizontalScope())
            {
                Field(nameof(MockMessageController.level));
                Button("Set", Mock.SetLevel, ready && !inLevel);
            }

            using (new GUILayout.HorizontalScope())
            {
                Button("Play (spends a life)", Mock.PlayLevel, ready && !inLevel);
                Button("Open level", Mock.OpenLevel, ready && !inLevel);
                Button("Close level", Mock.CloseLevel, inLevel);
            }
        }

        private void DrawTasks(bool ready)
        {
            using (new GUILayout.HorizontalScope())
            {
                Button("Lock input", Mock.LockInput, ready);
                Button("Unlock input", Mock.UnlockInput, ready);
                Button("Save storage", Mock.SaveStorage, ready);
            }
        }
    }
}
