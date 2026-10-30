namespace ColorBlockJam.Pool
{
    public interface IMemoryPool
    {
        int CountAll { get; }
        int CountActive { get; }
        int CountInactive { get; }
        void Clear();
    }

    public interface IDespawnPool<in T> : IMemoryPool
    {
        void Despawn(T item);
    }

    public interface IMemoryPool<T> : IDespawnPool<T>
    {
        T Spawn();
    }

    public interface IMemoryPool<in TParam, T> : IDespawnPool<T>
    {
        T Spawn(TParam param);
    }
}
