using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.DesignPatterns.Singleton;

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

    public abstract class ObjectPoolInstaller<T, P> : PrivateSingleton<ObjectPoolInstaller<T, P>>
        where T : MonoBehaviour where P : Pool<T>
    {
        [SerializeField] protected PoolObject[] poolObjects;
        [SerializeField] protected bool destroyIfNotInPool = true;
        [SerializeField] private PoolContext contextMode = PoolContext.Scene;
        [Tooltip("Hold requests raised while the pools are still loading and serve them once ready. Off by default: callers that expect the callback to run inside Raise would receive the object late.")]
        [SerializeField] private bool queueRequestsWhileLoading;

        protected Dictionary<int, P> mappings = new();
        protected Dictionary<object, int> keyToPrefabId = new();
        protected Dictionary<T, int> buffer = new();
        protected HashSet<T> spawned = new();
        protected Dictionary<int, int> maxCapacities = new();
        protected Dictionary<int, PoolCapacityMode> capacityModes = new();
        protected Dictionary<int, LinkedList<T>> activeOrder = new();

        private EventBinding<PoolRequest<T>> requestBinding;
        private EventBinding<ReleaseRequest<T>> releaseBinding;
        private EventBinding<PoolAllRequest<T>> releaseAllBinding;

        private const int MaxDeferredRequests = 256;

        protected DiContainer container;
        private DiContainer sceneContainer;
        private bool singletonReady;
        private bool initialized;
        private bool initializing;
        private bool failed;
        private bool registered;
        private readonly List<PoolRequest<T>> deferredRequests = new();

        /// <summary>True once every pool has been built and requests are served.</summary>
        public bool IsReady => initialized;

        /// <summary>True when initialization threw; requests are refused with an error instead of silently dropped.</summary>
        public bool HasFailed => failed;

        [Serializable]
        public class PoolObject
        {
            public T Prefab;
            [Range(0, 100000)] public int DefaultCapacity = 100;
            [Range(0, 100000)] public int MaxCapacity = 200;
            public PoolCapacityMode CapacityMode = PoolCapacityMode.SoftLock;
        }

        [Inject]
        private void Construct(DiContainer sceneContainer)
        {
            this.sceneContainer = sceneContainer;
            TryInitialize();
        }

        protected override void OnSingletonAwake()
        {
            singletonReady = true;
            TryInitialize();
        }

        private void TryInitialize()
        {
            if (initialized || initializing || failed || !singletonReady)
            {
                return;
            }

            if (contextMode == PoolContext.Project)
            {
                container = ProjectContext.Instance.Container;
            }
            else
            {
                if (sceneContainer == null)
                {
                    return;
                }

                container = sceneContainer;
            }

            initializing = true;

            // Listen straight away so requests raised while an async pool is still loading are
            // reported (or queued, when enabled) instead of vanishing silently.
            RegisterIfActive();
            _ = InitializeRoutine();
        }

        // Never async void: every exception is caught here and reported, so a failed load cannot
        // leave the installer "initializing" forever with requests quietly dropped.
        private async Task InitializeRoutine()
        {
            try
            {
                RegisterCapacityMetadata();
                await InitializePoolsAsync();

                if (this == null)
                {
                    return;
                }

                initialized = true;
            }
            catch (Exception e)
            {
                failed = true;
                ReportFailure($"Pool initialization threw: {e}");
            }
            finally
            {
                initializing = false;
            }

            if (this == null)
            {
                return;
            }

            RegisterIfActive();
            ResolveDeferredRequests();
        }

        /// <summary>
        /// Logs the failure and raises <see cref="PoolInitializationFailed"/> so the game can tell the
        /// player instead of freezing silently.
        /// </summary>
        protected void ReportFailure(string reason)
        {
            DebugUtility.LogError(this, $"{typeof(T).Name} pool: {reason}");
            EventBus<PoolInitializationFailed>.Raise(new PoolInitializationFailed
            {
                ItemType = typeof(T),
                Reason = reason
            });
        }

        private void ResolveDeferredRequests()
        {
            if (deferredRequests.Count == 0)
            {
                return;
            }

            var requests = deferredRequests.ToArray();
            deferredRequests.Clear();

            foreach (var request in requests)
            {
                if (initialized)
                {
                    Serve(request);
                }
                else
                {
                    DebugUtility.LogError(this, $"{typeof(T).Name} pool failed to initialize; a queued request was dropped.");
                }
            }
        }

        private void RegisterCapacityMetadata()
        {
            if (poolObjects == null)
            {
                return;
            }

            foreach (var poolObj in poolObjects)
            {
                if (poolObj.Prefab == null)
                {
                    continue;
                }

                int prefabId = poolObj.Prefab.GetInstanceID();
                maxCapacities[prefabId] = poolObj.MaxCapacity;
                capacityModes[prefabId] = poolObj.CapacityMode;
            }
        }

        private void OnEnable()
        {
            RegisterIfActive();
        }

        private void OnDisable()
        {
            if (registered)
            {
                DeregisterEventHandlers();
                registered = false;
            }
        }

        private void RegisterIfActive()
        {
            if ((!initialized && !initializing) || registered || !isActiveAndEnabled)
            {
                return;
            }

            RegisterEventHandlers();
            registered = true;
        }

        protected virtual void RegisterEventHandlers()
        {
            requestBinding = new EventBinding<PoolRequest<T>>(OnPoolRequestReceived);
            EventBus<PoolRequest<T>>.Register(requestBinding);

            releaseBinding = new EventBinding<ReleaseRequest<T>>(OnReleaseRequestReceived);
            EventBus<ReleaseRequest<T>>.Register(releaseBinding);

            releaseAllBinding = new EventBinding<PoolAllRequest<T>>(OnPoolAllRequestReceived);
            EventBus<PoolAllRequest<T>>.Register(releaseAllBinding);
        }

        protected virtual void DeregisterEventHandlers()
        {
            EventBus<PoolRequest<T>>.Deregister(requestBinding);
            EventBus<ReleaseRequest<T>>.Deregister(releaseBinding);
            EventBus<PoolAllRequest<T>>.Deregister(releaseAllBinding);
        }

        private void OnPoolRequestReceived(PoolRequest<T> request)
        {
            if (initialized)
            {
                Serve(request);
                return;
            }

            if (failed)
            {
                DebugUtility.LogError(this, $"{typeof(T).Name} pool failed to initialize; request dropped.");
                return;
            }

            if (queueRequestsWhileLoading && deferredRequests.Count < MaxDeferredRequests)
            {
                deferredRequests.Add(request);
                return;
            }

            DebugUtility.LogWarning(this, $"{typeof(T).Name} pool is still loading; request dropped.");
        }

        private void Serve(PoolRequest<T> request)
        {
            try
            {
                T obj = request.Key != null ? Get(request.Key) : Get(request.Prefab);
                if (obj == null)
                {
                    return;
                }

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

        private void OnPoolAllRequestReceived(PoolAllRequest<T> request)
        {
            try
            {
                ReleaseAll(request.Callback);
            }
            catch (Exception e)
            {
                DebugUtility.LogWarning(this, $"PoolAllRequest failed: {e}");
            }
        }

        public abstract void InitializePools();

        protected virtual Task InitializePoolsAsync()
        {
            InitializePools();
            return Task.CompletedTask;
        }

        public virtual void OnCreated(int prefabId, T obj)
        {
            buffer.TryAdd(obj, prefabId);
            obj.gameObject.SetActive(false);
        }

        public virtual void OnSpawned(T obj)
        {
            spawned.Add(obj);
            if (buffer.TryGetValue(obj, out int prefabId) && GetCapacityMode(prefabId) == PoolCapacityMode.QueueLock)
            {
                GetActiveOrder(prefabId).AddLast(obj);
            }
            obj.gameObject.SetActive(true);
            if (obj.TryGetComponent(out IPoolable poolable))
            {
                poolable.OnSpawned();
            }
        }

        public virtual void OnDespawned(T obj)
        {
            spawned.Remove(obj);
            if (buffer.TryGetValue(obj, out int prefabId) && activeOrder.TryGetValue(prefabId, out var order))
            {
                order.Remove(obj);
            }
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

            return GetByPrefabId(prefab.GetInstanceID(), prefab.name);
        }

        public virtual T Get(object key)
        {
            if (key == null)
            {
                DebugUtility.LogError(this, "Provided key is null.");
                return null;
            }

            if (!keyToPrefabId.TryGetValue(key, out int prefabId))
            {
                DebugUtility.LogWarning(this, $"No pool found for key: {key}");
                return null;
            }

            return GetByPrefabId(prefabId, key.ToString());
        }

        private T GetByPrefabId(int prefabId, string label)
        {
            if (!mappings.TryGetValue(prefabId, out var pool))
            {
                DebugUtility.LogWarning(this, $"No pool found for: {label}");
                return null;
            }

            if (pool.NumInactive > 0)
            {
                return SpawnFrom(pool, prefabId);
            }

            PoolCapacityMode mode = GetCapacityMode(prefabId);

            if (mode == PoolCapacityMode.SoftLock)
            {
                return SpawnFrom(pool, prefabId);
            }

            int maxCapacity = GetMaxCapacity(prefabId);

            if (pool.NumTotal < maxCapacity)
            {
                return SpawnFrom(pool, prefabId);
            }

            if (mode == PoolCapacityMode.HardLock)
            {
                DebugUtility.LogWarning(this, $"Pool for {label} reached its max capacity of {maxCapacity}. Request refused.");
                return null;
            }

            RecycleOldest(prefabId);
            return SpawnFrom(pool, prefabId);
        }

        private T SpawnFrom(P pool, int prefabId)
        {
            T instance = pool.Spawn();
            buffer.TryAdd(instance, prefabId);
            return instance;
        }

        private void RecycleOldest(int prefabId)
        {
            if (activeOrder.TryGetValue(prefabId, out var order) && order.First != null)
            {
                Release(order.First.Value);
            }
        }

        private LinkedList<T> GetActiveOrder(int prefabId)
        {
            if (!activeOrder.TryGetValue(prefabId, out var order))
            {
                order = new LinkedList<T>();
                activeOrder[prefabId] = order;
            }

            return order;
        }

        private PoolCapacityMode GetCapacityMode(int prefabId)
        {
            return capacityModes.TryGetValue(prefabId, out var mode) ? mode : PoolCapacityMode.SoftLock;
        }

        private int GetMaxCapacity(int prefabId)
        {
            return maxCapacities.TryGetValue(prefabId, out var max) ? max : int.MaxValue;
        }

        public virtual void Release(T obj)
        {
            if (obj == null)
            {
                DebugUtility.LogError(this, "Provided object is null.");
                return;
            }

            if (buffer.TryGetValue(obj, out int prefabId))
            {
                if (!spawned.Contains(obj))
                {
                    DebugUtility.LogWarning(this, $"Object already released: {obj.name}");
                    return;
                }

                if (mappings.TryGetValue(prefabId, out var pool))
                {
                    DebugUtility.Log(this, $"ReleaseRequest fullfilled!");
                    pool.Despawn(obj);
                }
                else
                {
                    DebugUtility.LogWarning(this, "No pool found for prefabId: " + prefabId);
                }
            }
            else
            {
                if (destroyIfNotInPool) Destroy(obj.gameObject);
                DebugUtility.LogWarning(this, "Failed to get prefabId for object: " + obj.name);
            }
        }

        public virtual void ReleaseAll(Action<T> callback = null)
        {
            if (spawned.Count == 0)
            {
                return;
            }
            
            var toRelease = new List<T>(spawned);

            foreach (var obj in toRelease)
            {
                if (obj == null)
                {
                    DebugUtility.LogWarning(this, "Null object encountered while releasing all pooled objects. Skipping.");
                    spawned.Remove(obj);
                    continue;
                }

                Release(obj);
                callback?.Invoke(obj);
            }
        }
    }
}
