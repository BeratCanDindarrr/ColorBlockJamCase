using System.Linq;
using ColorBlockJam.Core.Level;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // The level's problems as a notification card in the Scene view's bottom-right corner while any exist: red for
    // errors (Save is blocked), yellow for warnings. "–" folds it to its title line; Select picks what a problem is about.
    public sealed partial class LevelEditorSession
    {
        private const float ToastWidth = 340f;
        private const float ToastMargin = 12f;
        private const float ToastPadding = 8f;
        private const float ToastAccent = 4f;
        private const float ToastLine = 18f;
        private const float ToastSelectWidth = 52f;
        private const int ToastMaxRows = 4;
        private static readonly Color ToastBackground = new Color(0.13f, 0.13f, 0.13f, 0.96f);
        private static readonly Color ErrorColor = new Color(0.92f, 0.26f, 0.24f);
        private static readonly Color WarningColor = new Color(0.98f, 0.76f, 0.18f);

        private bool _toastFolded;
        private Rect _toastRect;

        // Scene input skips presses on the card, so its buttons work.
        public static bool IsOverProblemToast(Vector2 guiPoint) =>
            instance._toastRect.width > 0f && instance._toastRect.Contains(guiPoint);

        internal void DrawProblemToast(SceneView sceneView)
        {
            _toastRect = Rect.zero;
            if (working == null || _issues.Count == 0) return;

            var errors = _issues.Count(i => i.IsError);
            var accent = errors > 0 ? ErrorColor : WarningColor;
            var rows = _issues.OrderByDescending(i => i.IsError).Take(ToastMaxRows).ToList();
            var textWidth = ToastWidth - ToastAccent - ToastPadding * 2f - ToastSelectWidth - 4f;
            var message = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { normal = { textColor = Color.white } };

            var height = ToastPadding * 2f + ToastLine;
            if (!_toastFolded)
            {
                if (errors > 0) height += ToastLine;
                height += rows.Sum(i => Mathf.Max(ToastLine, message.CalcHeight(new GUIContent(i.Message), textWidth)) + 2f);
                if (_issues.Count > rows.Count) height += ToastLine;
            }

            var view = sceneView.camera.pixelRect;
            var scale = EditorGUIUtility.pixelsPerPoint;
            _toastRect = new Rect(view.width / scale - ToastWidth - ToastMargin, view.height / scale - height - ToastMargin,
                ToastWidth, height);

            Handles.BeginGUI();
            EditorGUI.DrawRect(_toastRect, ToastBackground);
            EditorGUI.DrawRect(new Rect(_toastRect.x, _toastRect.y, ToastAccent, _toastRect.height), accent);

            var x = _toastRect.x + ToastAccent + ToastPadding;
            var y = _toastRect.y + ToastPadding;
            var width = ToastWidth - ToastAccent - ToastPadding * 2f;
            DrawToastTitle(new Rect(x, y, width, ToastLine), errors, _issues.Count - errors, accent);
            y += ToastLine;

            if (!_toastFolded)
            {
                if (errors > 0)
                {
                    GUI.Label(new Rect(x, y, width, ToastLine), "Save is blocked until the errors are fixed.", EditorStyles.miniLabel);
                    y += ToastLine;
                }

                foreach (var issue in rows) y = DrawToastRow(issue, x, y, textWidth, message);

                if (_issues.Count > rows.Count)
                    GUI.Label(new Rect(x, y, width, ToastLine), $"+{_issues.Count - rows.Count} more (Level Editor window lists all)",
                        EditorStyles.miniLabel);
            }

            Handles.EndGUI();
        }

        private void DrawToastTitle(Rect rect, int errors, int warnings, Color accent)
        {
            var icon = EditorGUIUtility.IconContent(errors > 0 ? "console.erroricon.sml" : "console.warnicon.sml").image;
            var title = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = accent } };
            var text = errors > 0 ? $"Level has {errors} error(s)" : "Level has warnings";
            if (errors > 0 && warnings > 0) text += $", {warnings} warning(s)";
            else if (errors == 0) text += $" ({warnings})";
            GUI.Label(new Rect(rect.x, rect.y, rect.width - 22f, rect.height), new GUIContent(" " + text, icon), title);

            if (GUI.Button(new Rect(rect.xMax - 20f, rect.y, 20f, rect.height), _toastFolded ? "+" : "–", EditorStyles.miniButton))
                _toastFolded = !_toastFolded;
        }

        private float DrawToastRow(LevelIssue issue, float x, float y, float textWidth, GUIStyle message)
        {
            var height = Mathf.Max(ToastLine, message.CalcHeight(new GUIContent(issue.Message), textWidth));
            EditorGUI.DrawRect(new Rect(x, y + 5f, 6f, 6f), issue.IsError ? ErrorColor : WarningColor);
            GUI.Label(new Rect(x + 10f, y, textWidth - 10f, height), issue.Message, message);
            if ((issue.BlockIndex >= 0 || issue.GateIndex >= 0) &&
                GUI.Button(new Rect(x + textWidth + 4f, y, ToastSelectWidth, ToastLine - 2f), "Select", EditorStyles.miniButton))
            {
                SwitchTool(LevelEditorTool.Select);
                SelectOnly(issue.BlockIndex, issue.GateIndex);
                RaiseStateChanged();
            }

            return y + height + 2f;
        }
    }

    // Draws the problem card on every Scene view while the level editor is editing in the editor scene.
    [InitializeOnLoad]
    internal static class LevelEditorProblemToast
    {
        static LevelEditorProblemToast()
        {
            SceneView.duringSceneGui += sceneView =>
            {
                if (LevelEditorSceneOverlays.IsEditing) LevelEditorSession.Current.DrawProblemToast(sceneView);
            };
        }
    }
}
