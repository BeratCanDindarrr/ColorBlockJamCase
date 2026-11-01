using System;
using System.Collections.Generic;
using System.Linq;
using ColorBlockJam.Core.Level;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Often used block shapes for the level editor, dragged from the Shapes panel onto the board like prefabs. Kept in
    // the project as an asset, made with a starter set the first time it is needed.
    internal sealed class LevelEditorShapeLibrary : ScriptableObject
    {
        private const string AssetPath = "Assets/_Game/Core/Settings/LevelEditorShapes.asset";

        [Serializable]
        public class Shape
        {
            public string Name;
            // Offsets from the top-left corner, as BlockData.Cells.
            public CellCoord[] Cells;
        }

        public List<Shape> Shapes = new List<Shape>();

        private static LevelEditorShapeLibrary _loaded;

        public static LevelEditorShapeLibrary Get()
        {
            if (_loaded != null) return _loaded;
            _loaded = AssetDatabase.LoadAssetAtPath<LevelEditorShapeLibrary>(AssetPath);
            if (_loaded != null) return _loaded;

            _loaded = CreateInstance<LevelEditorShapeLibrary>();
            _loaded.Shapes = StarterShapes();
            AssetDatabase.CreateAsset(_loaded, AssetPath);
            AssetDatabase.SaveAssets();
            return _loaded;
        }

        public void Add(string name, IEnumerable<CellCoord> cells)
        {
            var list = cells.ToList();
            var corner = new CellCoord(list.Min(c => c.Col), list.Min(c => c.Row));
            Undo.RecordObject(this, "Add Shape");
            Shapes.Add(new Shape
            {
                Name = name,
                Cells = list.Select(c => new CellCoord(c.Col - corner.Col, c.Row - corner.Row)).ToArray()
            });
            Save();
        }

        public void Remove(int index)
        {
            Undo.RecordObject(this, "Remove Shape");
            Shapes.RemoveAt(index);
            Save();
        }

        public void Rename(int index, string name)
        {
            Undo.RecordObject(this, "Rename Shape");
            Shapes[index].Name = name;
            Save();
        }

        private void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        private static List<Shape> StarterShapes()
        {
            Shape Make(string name, params int[] pairs) => new Shape
            {
                Name = name,
                Cells = Enumerable.Range(0, pairs.Length / 2).Select(i => new CellCoord(pairs[i * 2], pairs[i * 2 + 1])).ToArray()
            };

            return new List<Shape>
            {
                Make("1x1", 0, 0),
                Make("1x2", 0, 0, 1, 0),
                Make("1x3", 0, 0, 1, 0, 2, 0),
                Make("2x2", 0, 0, 1, 0, 0, 1, 1, 1),
                Make("L", 0, 0, 0, 1, 0, 2, 1, 2),
                Make("J", 1, 0, 1, 1, 1, 2, 0, 2),
                Make("T", 0, 0, 1, 0, 2, 0, 1, 1),
                Make("S", 1, 0, 2, 0, 0, 1, 1, 1)
            };
        }
    }

    // Right-click on a shape in the Shapes panel: rename or delete it.
    internal sealed class EditShapePopup : PopupWindowContent
    {
        private readonly int _index;
        private string _name;

        public EditShapePopup(int index)
        {
            _index = index;
            _name = LevelEditorShapeLibrary.Get().Shapes[index].Name;
        }

        public override Vector2 GetWindowSize() => new Vector2(220f, 80f);

        public override void OnGUI(Rect rect)
        {
            var library = LevelEditorShapeLibrary.Get();
            if (_index >= library.Shapes.Count)
            {
                editorWindow.Close();
                return;
            }

            GUILayout.Label("Shape", EditorStyles.boldLabel);
            _name = EditorGUILayout.TextField("Name", _name);
            using (new GUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_name)))
                {
                    if (GUILayout.Button("Save"))
                    {
                        library.Rename(_index, _name.Trim());
                        editorWindow.Close();
                    }
                }

                if (GUILayout.Button("Delete"))
                {
                    library.Remove(_index);
                    editorWindow.Close();
                }
            }
        }
    }
}
