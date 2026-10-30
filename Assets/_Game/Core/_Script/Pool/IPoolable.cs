namespace ColorBlockJam.Pool
{
    public interface IDespawnable
    {
        void OnDespawned();
    }

    public interface IPoolable : IDespawnable
    {
        void OnSpawned();
    }

    public interface IPoolable<in TParam> : IDespawnable
    {
        void OnSpawned(TParam param);
    }
}
