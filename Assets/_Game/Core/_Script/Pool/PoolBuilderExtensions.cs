using UnityEngine;
using VContainer;

namespace ColorBlockJam.Pool
{
    public static class PoolBuilderExtensions
    {
        public static RegistrationBuilder RegisterPool<T>(this IContainerBuilder builder,
            int initialSize = 0, int maxSize = MemoryPoolBase<T>.DefaultMaxSize) where T : class, new()
        {
            return builder.Register(r => new MemoryPool<T>(r, initialSize, maxSize), Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
        }

        public static RegistrationBuilder RegisterPool<TParam, T>(this IContainerBuilder builder,
            int initialSize = 0, int maxSize = MemoryPoolBase<T>.DefaultMaxSize) where T : class, new()
        {
            return builder.Register(r => new MemoryPool<TParam, T>(r, initialSize, maxSize), Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
        }

        public static RegistrationBuilder RegisterMonoPool<T>(this IContainerBuilder builder, T prefab,
            int initialSize = 0, int maxSize = MemoryPoolBase<T>.DefaultMaxSize, Transform parent = null)
            where T : Component
        {
            return builder.Register(r => new MonoMemoryPool<T>(r, prefab, initialSize, maxSize, parent), Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
        }

        public static RegistrationBuilder RegisterMonoPool<TParam, T>(this IContainerBuilder builder, T prefab,
            int initialSize = 0, int maxSize = MemoryPoolBase<T>.DefaultMaxSize, Transform parent = null)
            where T : Component
        {
            return builder.Register(r => new MonoMemoryPool<TParam, T>(r, prefab, initialSize, maxSize, parent), Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
        }
    }
}
