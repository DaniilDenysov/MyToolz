#if MYTOOLZ_ADDRESSABLES
using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MyToolz.DesignPatterns.ObjectPool
{
    /// <summary>
    /// Pools Addressable prefabs (requested with <c>PoolRequest.Key = reference.RuntimeKey</c>) and,
    /// like <see cref="DefaultObjectPoolInstaller{T}"/>, any directly referenced prefabs in
    /// <c>poolObjects</c> (requested with <c>PoolRequest.Prefab</c>).
    /// </summary>
    public class AddressableObjectPoolInstaller<T> : DefaultObjectPoolInstaller<T>
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

        protected override async Task InitializePoolsAsync()
        {
            // Directly referenced prefabs first; they need no loading.
            InitializePools();

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
                        ReportFailure($"Failed to load addressable asset {poolObj.AssetReference.RuntimeKey}: {handle.OperationException?.Message ?? "unknown error"}");
                        continue;
                    }

                    var prefabComponent = handle.Result.GetComponent<T>();
                    if (prefabComponent == null)
                    {
                        ReportFailure($"Loaded asset does not contain component {typeof(T).Name}: {poolObj.AssetReference.RuntimeKey}");
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

                    BindPrefabPool(prefabComponent, poolObj.DefaultCapacity, poolObj.MaxCapacity);

                    loadedHandles[prefabId] = handle;
                    keepHandle = true;
                }
                catch (Exception e)
                {
                    ReportFailure($"Failed to create pool for {poolObj.AssetReference.RuntimeKey}: {e}");
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
