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
        // Upper bound on dead (externally destroyed) instances skipped in a single spawn.
        private const int MaxDestroyedInstancesSkipped = 16;

        [SerializeField] protected PoolObject[] poolObjects;
        [SerializeField] protected bool destroyIfNotInPool = true;
        [SerializeField] private PoolContext contextMode = PoolContext.Scene;

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

        protected DiContainer container;
        private DiContainer sceneContainer;
        private bool singletonReady;
        private bool initialized;
        private bool initializing;
        private bool registered;

        // Requests that arrive while pools are still being created (Addressables load asynchronously)
        // are queued and replayed in order once the pools exist, instead of being dropped.
        private readonly Queue<Action> pendingRequests = new();
        private readonly TaskCompletionSource<bool> readySource = new();

        // Placement for the instance currently being spawned by a PoolRequest. Applied before the
        // object is activated, so OnEnable/IPoolable.OnSpawned observe the requested parent and pose.
        private bool hasPendingPlacement;
        private Transform pendingParent;
        private Vector3 pendingPosition;
        private Quaternion pendingRotation;

        /// <summary>True once every configured pool has been created.</summary>
        public bool IsReady => initialized;

        /// <summary>
        /// Completes with true when the pools are ready, or false if initialization failed.
        /// </summary>
        public Task<bool> Ready => readySource.Task;

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
            RegisterIfActive();
            TryInitialize();
        }

        private async void TryInitialize()
        {
            if (initialized || initializing || !singletonReady)
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
            try
            {
                RegisterCapacityMetadata();
                await InitializePoolsAsync();
                initialized = true;
            }
            catch (Exception e)
            {
                DebugUtility.LogError(this, $"Pool initialization failed: {e}");
            }
            finally
            {
                initializing = false;
            }

            if (this == null)
            {
                // Destroyed while pools were loading.
                pendingRequests.Clear();
                readySource.TrySetResult(false);
                return;
            }

            if (!initialized)
            {
                if (pendingRequests.Count > 0)
                {
                    DebugUtility.LogWarning(this, $"Dropping {pendingRequests.Count} pool request(s) because initialization failed.");
                    pendingRequests.Clear();
                }
                readySource.TrySetResult(false);
                return;
            }

            readySource.TrySetResult(true);
            RegisterIfActive();
            DrainPendingRequests();
        }

        private void RegisterCapacityMetadata()
        {
            if (poolObjects == null)
            {
                return;
            }

            foreach (var poolObj in poolObjects)
            {
                if (poolObj?.Prefab == null)
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
            // Registration only requires being the surviving singleton; requests received before the
            // pools exist are queued. A duplicate instance never registers.
            if (!singletonReady || registered || !isActiveAndEnabled)
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

        private void DrainPendingRequests()
        {
            while (pendingRequests.Count > 0 && initialized)
            {
                pendingRequests.Dequeue()?.Invoke();
            }
        }

        private void OnPoolRequestReceived(PoolRequest<T> request)
        {
            if (!initialized)
            {
                pendingRequests.Enqueue(() => HandlePoolRequest(request));
                return;
            }

            HandlePoolRequest(request);
        }

        private void HandlePoolRequest(PoolRequest<T> request)
        {
            T obj = null;
            try
            {
                hasPendingPlacement = true;
                pendingParent = request.Parent;
                pendingPosition = request.Position;
                pendingRotation = request.Rotation;

                obj = request.Key != null ? Get(request.Key) : Get(request.Prefab);
            }
            catch (Exception e)
            {
                DebugUtility.LogWarning(this, $"PoolRequest failed: {e}");
            }
            finally
            {
                hasPendingPlacement = false;
                pendingParent = null;
            }

            if (obj == null)
            {
                return;
            }

            try
            {
                request.Callback?.Invoke(obj);
            }
            catch (Exception e)
            {
                DebugUtility.LogWarning(this, $"PoolRequest callback failed: {e}");
            }
        }

        private void OnReleaseRequestReceived(ReleaseRequest<T> request)
        {
            if (!initialized)
            {
                pendingRequests.Enqueue(() => HandleReleaseRequest(request));
                return;
            }

            HandleReleaseRequest(request);
        }

        private void HandleReleaseRequest(ReleaseRequest<T> request)
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
            if (!initialized)
            {
                pendingRequests.Enqueue(() => HandlePoolAllRequest(request));
                return;
            }

            HandlePoolAllRequest(request);
        }

        private void HandlePoolAllRequest(PoolAllRequest<T> request)
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
            if (obj == null)
            {
                // Destroyed while inactive in the pool; SpawnFrom discards it.
                return;
            }

            spawned.Add(obj);
            if (buffer.TryGetValue(obj, out int prefabId) && GetCapacityMode(prefabId) == PoolCapacityMode.QueueLock)
            {
                GetActiveOrder(prefabId).AddLast(obj);
            }

            if (hasPendingPlacement)
            {
                ApplyPendingPlacement(obj.transform);
            }

            obj.gameObject.SetActive(true);
            if (obj.TryGetComponent(out IPoolable poolable))
            {
                poolable.OnSpawned();
            }
        }

        private void ApplyPendingPlacement(Transform target)
        {
            target.SetParent(pendingParent);
            // default(Quaternion) is (0,0,0,0), which is not a valid rotation.
            Quaternion rotation = pendingRotation.x == 0f && pendingRotation.y == 0f && pendingRotation.z == 0f && pendingRotation.w == 0f
                ? Quaternion.identity
                : pendingRotation;
            target.SetPositionAndRotation(pendingPosition, rotation);
        }

        public virtual void OnDespawned(T obj)
        {
            if (obj == null)
            {
                return;
            }

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
            // An inactive instance can be destroyed from outside (e.g. its parent was unloaded). Skip
            // such dead entries and forget them instead of handing out a destroyed object.
            for (int attempt = 0; attempt <= MaxDestroyedInstancesSkipped; attempt++)
            {
                T instance = pool.Spawn();
                if (instance != null)
                {
                    buffer.TryAdd(instance, prefabId);
                    return instance;
                }

                Forget(instance);
            }

            DebugUtility.LogError(this, "Pool keeps returning destroyed instances; they were destroyed outside the pool.");
            return null;
        }

        private void RecycleOldest(int prefabId)
        {
            if (!activeOrder.TryGetValue(prefabId, out var order))
            {
                return;
            }

            while (order.First != null)
            {
                T oldest = order.First.Value;
                if (oldest != null)
                {
                    Release(oldest);
                    return;
                }

                order.RemoveFirst();
                Forget(oldest);
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
            if (ReferenceEquals(obj, null))
            {
                DebugUtility.LogError(this, "Provided object is null.");
                return;
            }

            if (obj == null)
            {
                // Destroyed while spawned: nothing to return, but drop the ownership records.
                DebugUtility.LogWarning(this, "Released object was already destroyed; forgetting it.");
                Forget(obj);
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
            PruneDestroyed();

            if (spawned.Count == 0)
            {
                return;
            }

            var toRelease = new List<T>(spawned);

            foreach (var obj in toRelease)
            {
                Release(obj);
                callback?.Invoke(obj);
            }
        }

        /// <summary>Removes ownership records of pooled objects that were destroyed outside the pool.</summary>
        public void PruneDestroyed()
        {
            buffer.RemoveWhere(obj => obj == null);
            spawned.RemoveWhere(obj => obj == null);
            foreach (var order in activeOrder.Values)
            {
                for (var node = order.First; node != null;)
                {
                    var next = node.Next;
                    if (node.Value == null) order.Remove(node);
                    node = next;
                }
            }
        }

        private void Forget(T obj)
        {
            buffer.Remove(obj);
            spawned.Remove(obj);
            foreach (var order in activeOrder.Values)
            {
                order.Remove(obj);
            }
        }
    }

    internal static class PoolDictionaryExtensions
    {
        public static void RemoveWhere<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Predicate<TKey> match)
        {
            List<TKey> dead = null;
            foreach (var key in dictionary.Keys)
            {
                if (match(key)) (dead ??= new List<TKey>()).Add(key);
            }

            if (dead == null) return;
            foreach (var key in dead) dictionary.Remove(key);
        }
    }
}
