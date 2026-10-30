using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace ColorBlockJam.Pool
{
    public interface IPoolItemHandler<T> : IDisposable where T : class
    {
        T Create();
        void OnGet(T item);
        void OnRelease(T item);
        void Destroy(T item);
    }

    public sealed class ClassItemHandler<T> : IPoolItemHandler<T> where T : class, new()
    {
        private readonly IObjectResolver _resolver;

        public ClassItemHandler(IObjectResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public T Create()
        {
            var item = new T();
            _resolver.Inject(item);
            return item;
        }

        public void OnGet(T item) { }
        public void OnRelease(T item) { }
        public void Destroy(T item) => (item as IDisposable)?.Dispose();
        public void Dispose() { }
    }

    public sealed class ComponentItemHandler<T> : IPoolItemHandler<T> where T : Component
    {
        private readonly IObjectResolver _resolver;
        private readonly T _prefab;
        private readonly Transform _root;
        private readonly bool _ownsRoot;

        public ComponentItemHandler(IObjectResolver resolver, T prefab, Transform parent)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab), $"Pool of {typeof(T).Name} has no prefab");
            _ownsRoot = parent == null;
            _root = _ownsRoot ? new GameObject($"[Pool] {typeof(T).Name}").transform : parent;
        }

        public T Create() => _resolver.Instantiate(_prefab, _root);

        public void OnGet(T item) => item.gameObject.SetActive(true);

        public void OnRelease(T item)
        {
            item.gameObject.SetActive(false);
            item.transform.SetParent(_root, false);
        }

        public void Destroy(T item)
        {
            if (item != null) Object.Destroy(item.gameObject);
        }

        public void Dispose()
        {
            if (_ownsRoot && _root != null) Object.Destroy(_root.gameObject);
        }
    }
}
