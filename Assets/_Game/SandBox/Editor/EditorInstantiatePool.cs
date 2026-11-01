using System;
using ColorBlockJam.Pool;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ColorBlockJam.SandBox.Editor
{
    // Stands in for the game's pools in edit mode: every Spawn instantiates the prefab and Despawn destroys it, so
    // the preview can run the game's own view-building code without a VContainer scope.
    internal sealed class EditorInstantiatePool<T> : IMemoryPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Action<T> _onCreated;

        public EditorInstantiatePool(T prefab, Transform parent, Action<T> onCreated = null)
        {
            _prefab = prefab;
            _parent = parent;
            _onCreated = onCreated;
        }

        public int CountAll => 0;
        public int CountActive => 0;
        public int CountInactive => 0;

        public T Spawn()
        {
            var item = Object.Instantiate(_prefab, _parent, false);
            item.gameObject.hideFlags = HideFlags.DontSave;
            _onCreated?.Invoke(item);
            if (item is IPoolable poolable) poolable.OnSpawned();
            return item;
        }

        public void Despawn(T item)
        {
            if (item != null) Object.DestroyImmediate(item.gameObject);
        }

        public void Clear()
        {
        }
    }
}
