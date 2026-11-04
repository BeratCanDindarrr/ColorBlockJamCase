using System.Collections.Generic;
using System.Linq;
using ColorBlockJam.Core.Settings;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Test tab of the Level Order panel: every level in play order with whether it can be finished (LevelEditorTests),
    // "Test changed" for the levels edited since their last test (or never tested), and a Test button per level. The time
    // a solution takes is its drags times a pace per drag, against the level's time limit.
    internal sealed class LevelEditorTestTab
    {
        private const string SecondsPerDragKey = "ColorBlockJam.LevelEditor.SecondsPerDrag";
        private const float DefaultSecondsPerDrag = 2.5f;
        private const float NameWidth = 72f;
        private const float ButtonWidth = 22f;
        private const float PaceLabelWidth = 62f;
        private const float PaceFieldWidth = 40f;

        private static readonly Color Pass = new Color(0.35f, 0.8f, 0.4f);
        private static readonly Color Slow = new Color(1f, 0.65f, 0.2f);
        private static readonly Color Fail = new Color(1f, 0.35f, 0.35f);
        private static readonly Color Stale = new Color(0.95f, 0.85f, 0.3f);
        private static readonly Color None = new Color(0.6f, 0.6f, 0.6f);
        private static readonly GUIContent PreviewContent =
            new GUIContent(EditorGUIUtility.IconContent("d_scenevis_visible_hover").image, "Open this level in the editor");

        private readonly Dictionary<Color, GUIStyle> _styles = new Dictionary<Color, GUIStyle>();
        private Vector2 _scroll;

        public void Draw(float maxHeight)
        {
            if (!LevelEditorTests.CanTest)
            {
                EditorGUILayout.HelpBox("The level solver is not loaded (SandBox editor code).", MessageType.Warning);
                return;
            }

            var levels = LevelEditorAssets.FindLevelSettings()?.Levels?.Where(l => l != null).ToList() ?? new List<LevelData>();
            var pace = Mathf.Max(0.1f, EditorPrefs.GetFloat(SecondsPerDragKey, DefaultSecondsPerDrag));
            var untested = LevelEditorTests.Untested(levels);

            DrawPlayback();
            DrawSummary(levels, untested.Count);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(untested.Count == 0))
            {
                if (GUILayout.Button(new GUIContent($"Test changed ({untested.Count})",
                        "Plays the levels edited since their last test, or never tested")))
                    EditorApplication.delayCall += () => TestAll(untested);
            }

            GUILayout.Label(new GUIContent("Sec / drag", "How long a player takes per drag, for the time estimate"),
                EditorStyles.miniLabel, GUILayout.Width(PaceLabelWidth));
            var newPace = EditorGUILayout.FloatField(pace, GUILayout.Width(PaceFieldWidth));
            if (!Mathf.Approximately(newPace, pace)) EditorPrefs.SetFloat(SecondsPerDragKey, Mathf.Max(0.1f, newPace));
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(maxHeight));
            for (var i = 0; i < levels.Count; i++) DrawRow(i, levels[i], pace);
            EditorGUILayout.EndScrollView();
        }

        // Watch the open level's solution in the scene preview: start it, then step through it.
        private static void DrawPlayback()
        {
            var session = LevelEditorSession.Current;
            if (session.Source == null) return;
            if (!session.IsPlayingBack)
            {
                if (GUILayout.Button(new GUIContent($"▶ Watch solution · {session.Source.name}",
                        "Plays the open level's solution step by step in the scene preview (unsaved edits included)")))
                    EditorApplication.delayCall += session.StartPlayback;
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(session.PlaybackCaption(), EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.BeginHorizontal();
            var index = session.PlaybackIndex;
            var count = session.PlaybackCount;
            var busy = session.IsAnimating || session.IsAutoPlaying;
            using (new EditorGUI.DisabledScope(index == 0 || busy))
            {
                if (GUILayout.Button(new GUIContent("⏮", "Start"), EditorStyles.miniButtonLeft)) session.ShowPlaybackStep(0);
                if (GUILayout.Button(new GUIContent("‹", "Previous drag"), EditorStyles.miniButtonMid)) session.ShowPlaybackStep(index - 1);
            }

            using (new EditorGUI.DisabledScope(index >= count))
            {
                var play = session.IsAutoPlaying ? new GUIContent("⏸", "Pause") : new GUIContent("▶", "Play the drags one after another");
                if (GUILayout.Button(play, EditorStyles.miniButtonMid)) session.ToggleAutoPlay();
            }

            using (new EditorGUI.DisabledScope(index >= count || busy))
            {
                if (GUILayout.Button(new GUIContent("›", "Next drag (animated)"), EditorStyles.miniButtonMid)) session.PlayNextStep();
                if (GUILayout.Button(new GUIContent("⏭", "End"), EditorStyles.miniButtonRight)) session.ShowPlaybackStep(count);
            }

            if (GUILayout.Button(new GUIContent("Stop", "Back to editing the level"), EditorStyles.miniButton)) session.StopPlayback();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private static void DrawSummary(List<LevelData> levels, int untested)
        {
            var tested = levels.Where(LevelEditorTests.IsUpToDate).Select(LevelEditorTests.Find).ToList();
            var pass = tested.Count(e => e.Verdict == LevelTestVerdict.Solvable);
            var fail = tested.Count(e => e.Verdict != LevelTestVerdict.Solvable);
            GUILayout.Label($"{pass} pass · {fail} fail · {untested} to test", EditorStyles.miniBoldLabel);
        }

        private void DrawRow(int index, LevelData level, float pace)
        {
            var session = LevelEditorSession.Current;
            var isOpen = level == session.Source;
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label($"{index + 1}. {level.name}", isOpen ? EditorStyles.miniBoldLabel : EditorStyles.miniLabel,
                GUILayout.Width(NameWidth));
            var (text, color, tip) = Status(level, pace);
            if (isOpen && session.HasUnsavedEdits) tip += "\nUnsaved edits are not tested; Save first.";
            GUILayout.Label(new GUIContent(text, tip), StyleOf(color), GUILayout.MinWidth(0f));

            using (new EditorGUI.DisabledScope(isOpen))
            {
                if (GUILayout.Button(PreviewContent, EditorStyles.miniButton, GUILayout.Width(ButtonWidth)))
                    EditorApplication.delayCall += () => session.OpenLevel(level);
            }

            if (GUILayout.Button(new GUIContent("▶", "Test this level"), EditorStyles.miniButton, GUILayout.Width(ButtonWidth)))
                EditorApplication.delayCall += () => TestAll(new List<LevelData> { level });
            EditorGUILayout.EndHorizontal();
        }

        private static (string Text, Color Color, string Tip) Status(LevelData level, float pace)
        {
            var entry = LevelEditorTests.Find(level);
            if (entry == null) return ("Not tested", None, "Press ▶ or Test changed");

            string text;
            Color color;
            string tip;
            switch (entry.Verdict)
            {
                case LevelTestVerdict.Solvable:
                    var seconds = entry.Drags * pace;
                    var slow = seconds > level.TimeLimitSec;
                    text = $"✓ {entry.Drags} drags · ~{seconds:0}s / {level.TimeLimitSec:0}s";
                    color = slow ? Slow : Pass;
                    tip = $"Can be finished: {entry.Drags} drags ({entry.AsideMoves} moving blocks aside)." +
                          (slow ? "\nAt this pace it takes longer than the time limit." : "");
                    break;
                case LevelTestVerdict.Unsolvable:
                    text = "✗ Stuck: " + string.Join(", ", entry.StuckBlocks);
                    color = Fail;
                    tip = "These blocks can never leave, however the others are moved.";
                    break;
                default:
                    text = "? Too big to decide";
                    color = Slow;
                    tip = "The search stopped at its limit before deciding.";
                    break;
            }

            if (LevelEditorTests.IsUpToDate(level)) return (text, color, tip);
            return ("Changed · was " + text, Stale, "Edited since this test; test it again.\n" + tip);
        }

        private GUIStyle StyleOf(Color color)
        {
            if (_styles.TryGetValue(color, out var style)) return style;
            style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = color }, clipping = TextClipping.Clip };
            _styles[color] = style;
            return style;
        }

        // Runs outside the panel's drawing (from delayCall), with a progress bar that can stop the batch between levels.
        private static void TestAll(List<LevelData> levels)
        {
            try
            {
                for (var i = 0; i < levels.Count; i++)
                {
                    if (levels[i] == null) continue;
                    if (EditorUtility.DisplayCancelableProgressBar("Testing levels", $"{levels[i].name} ({i + 1}/{levels.Count})",
                            (float)i / levels.Count))
                        break;
                    LevelEditorTests.Test(levels[i]);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                LevelEditorTests.SaveResults();
                SceneView.RepaintAll();
            }
        }
    }
}
