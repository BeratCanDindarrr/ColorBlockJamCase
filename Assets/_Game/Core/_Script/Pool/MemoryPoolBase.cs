using System;
using UnityEngine;
using UnityEngine.Pool;

namespace ColorBlockJam.Pool
{
    public abstract class MemoryPoolBase<T> : IDespawnPool<T>, IDisposable where T : class
    {
        public const int DefaultMaxSize = 100;

        private readonly IPoolItemHandler<T> _handler;
        private readonly ObjectPool<T> _pool;

        protected MemoryPoolBase(IPoolItemHandler<T> handler, int initialSize, int maxSize)
        {
            if (maxSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxSize), maxSize, "must be > 0");
            if (initialSize < 0 || initialSize > maxSize)
                throw new ArgumentOutOfRangeException(nameof(initialSize), initialSize, $"must be in 0..{maxSize}");

            _handler = handler;
            // Double-release check is O(n), so only in the editor.
            _pool = new ObjectPool<T>(handler.Create, handler.OnGet, handler.OnRelease, handler.Destroy,
                Application.isEditor, Math.Max(initialSize, 1), maxSize);
            Prewarm(initialSize);
        }

        public int CountAll => _pool.CountAll;
        public int CountActive => _pool.CountActive;
        public int CountInactive => _pool.CountInactive;

        public void Despawn(T item)
        {
            if (item == null)
            {
                Debug.LogError($"{GetType().Name}: Despawn called with null");
                return;
            }

            OnDespawned(item);
            (item as IDespawnable)?.OnDespawned();
            _pool.Release(item);
        }

        public void Clear() => _pool.Clear();

        public void Dispose()
        {
            _pool.Dispose();
            _handler.Dispose();
        }

        protected T SpawnItem()
        {
            var item = _pool.Get();
            (item as IPoolable)?.OnSpawned();
            OnSpawned(item);
            return item;
        }

        protected T SpawnItem<TParam>(TParam param)
        {
            var item = _pool.Get();
            (item as IPoolable<TParam>)?.OnSpawned(param);
            OnSpawned(item);
            return item;
        }

        protected virtual void OnSpawned(T item) { }
        protected virtual void OnDespawned(T item) { }

        // Bypasses Spawn/Despawn so prewarmed items never get IPoolable callbacks.
        private void Prewarm(int count)
        {
            var items = new T[count];
            for (var i = 0; i < count; i++) items[i] = _pool.Get();
            foreach (var item in items) _pool.Release(item);
        }
    }
}
