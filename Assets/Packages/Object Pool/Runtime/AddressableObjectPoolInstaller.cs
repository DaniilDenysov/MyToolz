#if MYTOOLZ_ADDRESSABLES
using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Zenject;

namespace MyToolz.DesignPatterns.ObjectPool
{
    public class AddressableObjectPoolInstaller<T> : ObjectPoolInstaller<T, Pool<T>>
        where T : MonoBehaviour
    {
        [Serializable]
        public class AddressablePoolObject
        {
            public AssetReferenceGameObject AssetReference;
            [Range(0, 100000)] public int DefaultCapacity = 100;
            [Range(0, 100000)] public int MaxCapacity = 200;
            public PoolCapacityMode CapacityMode = PoolCapacityMode.SoftLock;
        }

        [SerializeField] private AddressablePoolObject[] addressablePoolObjects;

        private readonly Dictionary<int, AsyncOperationHandle<GameObject>> loadedHandles = new();
        private CancellationTokenSource cancellationTokenSource;

        public override void InitializePools()
        {
        }

        protected override async Task InitializePoolsAsync()
        {
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            if (addressablePoolObjects == null)
            {
                return;
            }

            foreach (var poolObj in addressablePoolObjects)
            {
                if (token.IsCancellationRequested) return;

                if (poolObj?.AssetReference == null || !poolObj.AssetReference.RuntimeKeyIsValid())
                {
                    DebugUtility.LogWarning(this, "Invalid AssetReference in AddressablePoolObject configuration.");
                    continue;
                }

                AsyncOperationHandle<GameObject> handle = default;
                bool keepHandle = false;
                try
                {
                    handle = Addressables.LoadAssetAsync<GameObject>(poolObj.AssetReference);
                    await handle.Task;

                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                    {
                        DebugUtility.LogError(this, $"Failed to load addressable asset: {poolObj.AssetReference.RuntimeKey} {handle.OperationException}");
                        continue;
                    }

                    var prefabComponent = handle.Result.GetComponent<T>();
                    if (prefabComponent == null)
                    {
                        DebugUtility.LogError(this, $"Loaded asset does not contain component {typeof(T).Name}: {poolObj.AssetReference.RuntimeKey}");
                        continue;
                    }

                    int prefabId = prefabComponent.GetInstanceID();

                    RegisterMetadata(poolObj, prefabId);

                    if (container.HasBindingId<Pool<T>>(prefabId))
                    {
                        // Another installer already owns a pool (and a handle) for this asset.
                        mappings[prefabId] = container.ResolveId<Pool<T>>(prefabId);
                        continue;
                    }

                    container.BindMemoryPool<T, Pool<T>>()
                        .WithId(prefabId)
                        .WithInitialSize(poolObj.DefaultCapacity)
                        .WithMaxSize(poolObj.MaxCapacity)
                        .WithFactoryArguments<Action<Pool<T>>, int, Action<T>, Action<int, T>, Action<T>>(
                            (pool) => mappings.Add(prefabId, pool),
                            prefabId,
                            OnSpawned,
                            OnCreated,
                            OnDespawned)
                        .FromComponentInNewPrefab(prefabComponent)
                        .UnderTransformGroup($"{typeof(T).Name} Pool");

                    container.ResolveId<Pool<T>>(prefabId);

                    loadedHandles[prefabId] = handle;
                    keepHandle = true;
                }
                catch (Exception e)
                {
                    DebugUtility.LogError(this, $"Failed to create pool for {poolObj.AssetReference.RuntimeKey}: {e}");
                }
                finally
                {
                    // Every exit that does not hand the handle to loadedHandles releases it.
                    if (!keepHandle && handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                }
            }
        }

        private void RegisterMetadata(AddressablePoolObject poolObj, int prefabId)
        {
            keyToPrefabId[poolObj.AssetReference.RuntimeKey] = prefabId;
            maxCapacities[prefabId] = poolObj.MaxCapacity;
            capacityModes[prefabId] = poolObj.CapacityMode;
        }

        protected override void OnSingletonDestroy()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();

            foreach (var handle in loadedHandles.Values)
            {
                if (handle.IsValid()) Addressables.Release(handle);
            }
            loadedHandles.Clear();
        }
    }
}
#endif
