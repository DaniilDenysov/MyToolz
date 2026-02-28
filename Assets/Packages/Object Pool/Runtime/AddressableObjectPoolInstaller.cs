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
        }

        [SerializeField] private AddressablePoolObject[] addressablePoolObjects;

        private readonly Dictionary<int, AsyncOperationHandle<GameObject>> loadedHandles = new();
        private CancellationTokenSource cancellationTokenSource;

        public override async void InitializePools()
        {
            await InitializePoolsAsync();
        }

        public async Task InitializePoolsAsync()
        {
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
                    DebugUtility.LogError(this, $"Failed to load addressable asset: {poolObj.AssetReference.RuntimeKey}");
                    continue;
                }

                var loadedPrefab = handle.Result;
                var prefabComponent = loadedPrefab.GetComponent<T>();

                if (prefabComponent == null)
                {
                    DebugUtility.LogError(this, $"Loaded asset does not contain component {typeof(T).Name}: {poolObj.AssetReference.RuntimeKey}");
                    Addressables.Release(handle);
                    continue;
                }

                int prefabId = prefabComponent.GetInstanceID();
                loadedHandles[prefabId] = handle;

                Container.BindMemoryPool<T, Pool<T>>()
                    .WithId(prefabId)
                    .WithInitialSize(poolObj.DefaultCapacity)
                    .WithFactoryArguments<Action<Pool<T>>, int, Action<T>, Action<int, T>, Action<T>>(
                        (pool) => mappings.Add(prefabId, pool),
                        prefabId,
                        OnSpawned,
                        OnCreated,
                        OnDespawned)
                    .FromComponentInNewPrefab(prefabComponent)
                    .UnderTransformGroup($"{typeof(T).Name} Pool")
                    .NonLazy();
            }
        }

        private void OnDestroy()
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
