using UnityEngine;
using VContainer;

namespace ColorBlockJam.Pool
{
    public class MonoMemoryPool<T> : MemoryPoolBase<T>, IMemoryPool<T> where T : Component
    {
        public MonoMemoryPool(IObjectResolver resolver, T prefab, int initialSize = 0,
            int maxSize = DefaultMaxSize, Transform parent = null)
            : base(new ComponentItemHandler<T>(resolver, prefab, parent), initialSize, maxSize) { }

        public T Spawn() => SpawnItem();
    }

    public class MonoMemoryPool<TParam, T> : MemoryPoolBase<T>, IMemoryPool<TParam, T> where T : Component
    {
        public MonoMemoryPool(IObjectResolver resolver, T prefab, int initialSize = 0,
            int maxSize = DefaultMaxSize, Transform parent = null)
            : base(new ComponentItemHandler<T>(resolver, prefab, parent), initialSize, maxSize) { }

        public T Spawn(TParam param) => SpawnItem(param);
    }
}
