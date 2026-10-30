using VContainer;

namespace ColorBlockJam.Pool
{
    // Plain C# objects: created with new T(), then [Inject] members are filled by VContainer.
    public class MemoryPool<T> : MemoryPoolBase<T>, IMemoryPool<T> where T : class, new()
    {
        public MemoryPool(IObjectResolver resolver, int initialSize = 0, int maxSize = DefaultMaxSize)
            : base(new ClassItemHandler<T>(resolver), initialSize, maxSize) { }

        public T Spawn() => SpawnItem();
    }

    public class MemoryPool<TParam, T> : MemoryPoolBase<T>, IMemoryPool<TParam, T> where T : class, new()
    {
        public MemoryPool(IObjectResolver resolver, int initialSize = 0, int maxSize = DefaultMaxSize)
            : base(new ClassItemHandler<T>(resolver), initialSize, maxSize) { }

        public T Spawn(TParam param) => SpawnItem(param);
    }
}
