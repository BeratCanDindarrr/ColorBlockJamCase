using ColorBlockJam.Core.Settings;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace ColorBlockJam.LevelEditor
{
    // The order the game plays the levels in (LevelSettings.Levels), as in its inspector: drag a row by its handle, use
    // ▲ ▼, or the ⇥ button to type the position to send it to. Every change is saved to the asset at once.
    // A second tab edits the boosters (LevelEditorBoosterTab). Opened from the level bar's Panels switches; closed by
    // default.
    [Overlay(typeof(SceneView), LevelEditorSceneOverlays.LevelOrderId, "Level Order", defaultDisplay = false,
        defaultDockZone = DockZone.Floating)]
    internal sealed class LevelEditorLevelOrderOverlay : Overlay
    {
        private const string LevelsField = "Levels";
        private const float Width = 300f;
        private const float MaxHeight = 420f;
        private const float PositionWidth = 34f;
        private const float ArrowWidth = 22f;
        private const float Gap = 4f;
        private const string TabKey = "ColorBlockJam.LevelEditor.LevelOrderTab";
        private static readonly string[] Tabs = { "Levels", "Boosters", "Test" };
        private static readonly GUIContent PreviewContent =
            new GUIContent(EditorGUIUtility.IconContent("d_scenevis_visible_hover").image, "Preview: open this level in the editor");

        private IMGUIContainer _content;
        private EditingVisibility _visibility;
        private SerializedObject _serialized;
        private ReorderableList _list;
        private Vector2 _scroll;
        private readonly LevelEditorBoosterTab _boosterTab = new LevelEditorBoosterTab();
        private readonly LevelEditorTestTab _testTab = new LevelEditorTestTab();
        private int _moveFrom = -1;
        private int _moveTo = -1;

        public override VisualElement CreatePanelContent()
        {
            _content = new IMGUIContainer(Draw);
            _content.style.width = Width;
            return _content;
        }

        public override void OnCreated() =>
            _visibility = new EditingVisibility(this, () => _content?.MarkDirtyRepaint(), opensItself: false);

        public override void OnWillBeDestroyed() => _visibility?.Dispose();

        // Three tabs: the level order; the boosters' unlock levels, pictures and texts; whether each level can be finished
        // (LevelEditorTestTab). The open tab is kept per user.
        private void Draw()
        {
            LevelEditorSceneOverlays.DrawCloseButton(LevelEditorSceneOverlays.LevelOrderId);
            var tab = EditorPrefs.GetInt(TabKey, 0);
            var picked = GUILayout.Toolbar(tab, Tabs);
            if (picked != tab) EditorPrefs.SetInt(TabKey, picked);
            GUILayout.Space(4f);

            if (picked == 1) _boosterTab.Draw(MaxHeight);
            else if (picked == 2) _testTab.Draw(MaxHeight);
            else DrawLevels();
        }

        private void DrawLevels()
        {
            var settings = LevelEditorAssets.FindLevelSettings();
            if (settings == null)
            {
                EditorGUILayout.HelpBox("No LevelSettings asset found.", MessageType.Warning);
                return;
            }

            if (_serialized == null || _serialized.targetObject != settings) Build(settings);
            _serialized.UpdateIfRequiredOrScript();

            GUILayout.Label("Drag ≡, use ▲ ▼, or ⇥ to send a level to a position", EditorStyles.miniLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(MaxHeight));
            _list.DoLayoutList();
            EditorGUILayout.EndScrollView();

            // Moves asked for while drawing the rows are done after, so the list is not changed under its own loop.
            if (_moveFrom >= 0)
            {
                _list.serializedProperty.MoveArrayElement(_moveFrom, _moveTo);
                _list.index = _moveTo;
                _moveFrom = _moveTo = -1;
            }

            if (!_serialized.ApplyModifiedProperties()) return;
            AssetDatabase.SaveAssetIfDirty(settings);
            LevelEditorSession.Current.RaiseStateChanged();
        }

        private void Build(LevelSettings settings)
        {
            _serialized = new SerializedObject(settings);
            _list = new ReorderableList(_serialized, _serialized.FindProperty(LevelsField), true, false, false, false)
            {
                drawElementCallback = DrawRow,
                elementHeight = EditorGUIUtility.singleLineHeight + 4f
            };
        }

        // Position, name (bold for the level being edited), preview (opens it), ▲ ▼, and ⇥ to send it to a typed position.
        private void DrawRow(Rect rect, int index, bool active, bool focused)
        {
            var levels = _list.serializedProperty;
            var level = levels.GetArrayElementAtIndex(index).objectReferenceValue as LevelData;
            var line = new Rect(rect.x, rect.y + 2f, rect.width, EditorGUIUtility.singleLineHeight);

            GUI.Label(new Rect(line.x, line.y, PositionWidth, line.height), $"{index + 1}.", EditorStyles.miniBoldLabel);

            var nameRect = new Rect(line.x + PositionWidth + Gap, line.y,
                line.width - PositionWidth - ArrowWidth * 4f - Gap * 4f, line.height);
            var isOpen = level != null && level == LevelEditorSession.Current.Source;
            GUI.Label(nameRect, level != null ? level.name : "(missing)", isOpen ? EditorStyles.boldLabel : EditorStyles.label);

            var preview = new Rect(line.xMax - ArrowWidth * 4f - Gap * 2f, line.y, ArrowWidth, line.height);
            using (new EditorGUI.DisabledScope(level == null || isOpen))
            {
                if (GUI.Button(preview, PreviewContent, EditorStyles.miniButton))
                    // Opening may ask about unsaved changes; a dialog in the middle of drawing the list breaks its layout.
                    EditorApplication.delayCall += () => LevelEditorSession.Current.OpenLevel(level);
            }

            var up = new Rect(line.xMax - ArrowWidth * 3f - Gap, line.y, ArrowWidth, line.height);
            var down = new Rect(line.xMax - ArrowWidth * 2f - Gap, line.y, ArrowWidth, line.height);
            var send = new Rect(line.xMax - ArrowWidth, line.y, ArrowWidth, line.height);
            using (new EditorGUI.DisabledScope(index == 0))
            {
                if (GUI.Button(up, new GUIContent("▲", "Move up"), EditorStyles.miniButtonLeft)) AskMove(index, index - 1);
            }

            using (new EditorGUI.DisabledScope(index == levels.arraySize - 1))
            {
                if (GUI.Button(down, new GUIContent("▼", "Move down"), EditorStyles.miniButtonRight)) AskMove(index, index + 1);
            }

            if (GUI.Button(send, new GUIContent("⇥", "Send to a position"), EditorStyles.miniButton))
            {
                var from = index;
                UnityEditor.PopupWindow.Show(send, new MoveToPopup(index + 1, levels.arraySize, to =>
                {
                    AskMove(from, to - 1);
                    _content?.MarkDirtyRepaint();
                }));
            }
        }

        private void AskMove(int from, int to)
        {
            if (from == to) return;
            _moveFrom = from;
            _moveTo = to;
        }
    }

    // "Send to position": type the new place (1 = first) and press Enter or Move.
    internal sealed class MoveToPopup : PopupWindowContent
    {
        private const string FieldName = "LevelOrderMoveTo";
        private static readonly Vector2 Size = new Vector2(170f, 64f);

        private readonly int _count;
        private readonly System.Action<int> _move;
        private int _position;
        private bool _focused;

        public MoveToPopup(int current, int count, System.Action<int> move)
        {
            _position = current;
            _count = count;
            _move = move;
        }

        public override Vector2 GetWindowSize() => Size;

        public override void OnGUI(Rect rect)
        {
            var e = Event.current;
            var enter = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);

            GUILayout.Label($"Send to position (1–{_count})", EditorStyles.miniBoldLabel);
            GUI.SetNextControlName(FieldName);
            _position = EditorGUILayout.IntField(_position);
            if (!_focused)
            {
                EditorGUI.FocusTextInControl(FieldName);
                _focused = true;
            }

            if (GUILayout.Button("Move") || enter)
            {
                _move(Mathf.Clamp(_position, 1, _count));
                editorWindow.Close();
                if (enter) e.Use();
            }
        }
    }
}
