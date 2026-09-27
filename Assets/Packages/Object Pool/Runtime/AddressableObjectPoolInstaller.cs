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
            InitializePools();

            if (addressablePoolObjects == null)
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            foreach (var poolObj in addressablePoolObjects)
            {
                if (token.IsCancellationRequested) return;

                if (poolObj.AssetReference == null || !poolObj.AssetReference.RuntimeKeyIsValid())
                {
                    DebugUtility.LogWarning(this, "Invalid AssetReference in AddressablePoolObject configuration.");
                    continue;
                }

                var handle = Addressables.LoadAssetAsync<GameObject>(poolObj.AssetReference);
                await handle.Task;

                if (token.IsCancellationRequested)
                {
                    if (handle.IsValid()) Addressables.Release(handle);
                    return;
                }

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    ReportFailure($"Failed to load addressable asset {poolObj.AssetReference.RuntimeKey}: {handle.OperationException?.Message ?? "unknown error"}");
                    if (handle.IsValid()) Addressables.Release(handle);
                    continue;
                }

                var loadedPrefab = handle.Result;
                var prefabComponent = loadedPrefab.GetComponent<T>();

                if (prefabComponent == null)
                {
                    ReportFailure($"Loaded asset does not contain component {typeof(T).Name}: {poolObj.AssetReference.RuntimeKey}");
                    Addressables.Release(handle);
                    continue;
                }

                int prefabId = prefabComponent.GetInstanceID();

                RegisterMetadata(poolObj, prefabId);

                if (container.HasBindingId<Pool<T>>(prefabId))
                {
                    mappings[prefabId] = container.ResolveId<Pool<T>>(prefabId);
                    Addressables.Release(handle);
                    continue;
                }

                loadedHandles[prefabId] = handle;
                BindPrefabPool(prefabComponent, poolObj.DefaultCapacity, poolObj.MaxCapacity);
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
            cancellationTokenSource = null;

            foreach (var handle in loadedHandles.Values)
            {
                if (handle.IsValid()) Addressables.Release(handle);
            }
            loadedHandles.Clear();
        }
    }
}
