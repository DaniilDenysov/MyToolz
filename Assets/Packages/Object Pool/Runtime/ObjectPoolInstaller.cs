using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;

namespace MyToolz.DesignPatterns.ObjectPool
{
    public class Pool<T> : MemoryPool<T>
        where T : MonoBehaviour
    {
        protected int prefabId;
        protected Action<T> onSpawned;
        protected Action<int, T> onCreated;
        protected Action<T> onDespawned;

        public Pool(Action<Pool<T>> onInitialized, int prefabId, Action<T> onSpawned, Action<int, T> onCreated, Action<T> onDespawned)
        {
            this.prefabId = prefabId;
            this.onSpawned = onSpawned;
            this.onCreated = onCreated;
            this.onDespawned = onDespawned;
            onInitialized?.Invoke(this);
        }

        protected override void OnSpawned(T item)
        {
            onSpawned?.Invoke(item);
        }

        protected override void OnCreated(T item)
        {
            base.OnCreated(item);
            onCreated?.Invoke(prefabId, item);
        }

        protected override void OnDespawned(T item)
        {
            onDespawned?.Invoke(item);
        }
    }

    public abstract class ObjectPoolInstaller<T, P> : MonoInstaller
        where T : MonoBehaviour where P : Pool<T>
    {
        [SerializeField] protected PoolObject[] poolObjects;

        protected Dictionary<int, P> mappings = new();
        protected Dictionary<T, int> buffer = new();

        private EventBinding<PoolRequest<T>> requestBinding;
        private EventBinding<ReleaseRequest<T>> releaseBinding;

        [Serializable]
        public class PoolObject
        {
            public T Prefab;
            [Range(0, 100000)] public int DefaultCapacity = 100;
            [Range(0, 100000)] public int MaxCapacity = 200;
        }

        public override void InstallBindings()
        {
            InitializePools();

            Container.BindInstance(this).AsSingle().NonLazy();
        }

        private void OnEnable()
        {
            RegisterEventHandlers();
        }

        private void OnDisable()
        {
            DeregisterEventHandlers();
        }

        private void OnDestroy()
        {
            OnDisable();
        }

        protected virtual void RegisterEventHandlers()
        {
            requestBinding = new EventBinding<PoolRequest<T>>(OnPoolRequestReceived);
            EventBus<PoolRequest<T>>.Register(requestBinding);

            releaseBinding = new EventBinding<ReleaseRequest<T>>(OnReleaseRequestReceived);
            EventBus<ReleaseRequest<T>>.Register(releaseBinding);
        }

        protected virtual void DeregisterEventHandlers()
        {
            EventBus<PoolRequest<T>>.Deregister(requestBinding);
            EventBus<ReleaseRequest<T>>.Deregister(releaseBinding);
        }

        private void OnPoolRequestReceived(PoolRequest<T> request)
        {
            T obj;
            try
            {
                obj = Get(request.Prefab);
                obj.transform.SetParent(request.Parent);
                obj.transform.position = request.Position;
                obj.transform.rotation = request.Rotation;
                request.Callback?.Invoke(obj);
            }
            catch (Exception e)
            {
                DebugUtility.LogWarning(this, $"PoolRequest failed: {e}");
            }
        }

        private void OnReleaseRequestReceived(ReleaseRequest<T> request)
        {
            try
            {
                var obj = request.PoolObject;
                Release(obj);
                request.Callback?.Invoke(obj);
            }
            catch (Exception e)
            {
                DebugUtility.LogWarning(this, $"ReleaseRequest failed: {e}");
            }
        }

        public abstract void InitializePools();

        public virtual void OnCreated(int prefabId, T obj)
        {
            buffer.TryAdd(obj, prefabId);
            obj.gameObject.SetActive(false);
        }

        public virtual void OnSpawned(T obj)
        {
            obj.gameObject.SetActive(true);
            if (obj.TryGetComponent(out IPoolable poolable))
            {
                poolable.OnSpawned();
            }
        }

        public virtual void OnDespawned(T obj)
        {
            obj.gameObject.SetActive(false);
            if (obj.TryGetComponent(out IPoolable poolable))
            {
                poolable.OnDespawned();
            }
        }

        public virtual T Get(T prefab)
        {
            if (prefab == null)
            {
                DebugUtility.LogError(this, "Provided prefab is null.");
                return null;
            }

            int prefabId = prefab.GetInstanceID();

            if (mappings.TryGetValue(prefabId, out var pool))
            {
                T instance = pool.Spawn();
                buffer.TryAdd(instance, prefabId);
                return instance;
            }
            else
            {
                DebugUtility.LogWarning(this, $"No pool found for prefab: {prefab.name}");
            }

            return null;
        }

        public virtual void Release(T obj)
        {
            if (buffer.TryGetValue(obj, out int prefabId))
            {
                if (mappings.TryGetValue(prefabId, out var pool))
                {
                    DebugUtility.Log(this, $"ReleaseRequest fullfilled!");
                    buffer.Remove(obj);
                    pool.Despawn(obj);
                }
                else
                {
                    DebugUtility.LogWarning(this, "No pool found for prefabId: " + prefabId);
                }
            }
            else
            {
                DebugUtility.LogWarning(this, "Failed to get prefabId for object: " + obj.name);
            }
        }
    }
}