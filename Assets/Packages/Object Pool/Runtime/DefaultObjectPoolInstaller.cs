using MyToolz.Utilities.Debug;
using System;
using UnityEngine;

namespace MyToolz.DesignPatterns.ObjectPool
{
    public class DefaultObjectPoolInstaller<T> : ObjectPoolInstaller<T, Pool<T>>
        where T : MonoBehaviour
    {
        public override void InitializePools()
        {
            if (poolObjects == null)
            {
                return;
            }

            foreach (var poolObj in poolObjects)
            {
                if (poolObj.Prefab == null)
                {
                    DebugUtility.LogWarning(this,"Prefab is null in PoolObject configuration.");
                    continue;
                }

                BindPrefabPool(poolObj.Prefab, poolObj.DefaultCapacity, poolObj.MaxCapacity);
            }
        }

        /// <summary>Binds (or reuses) the Zenject memory pool for one prefab.</summary>
        protected void BindPrefabPool(T prefab, int defaultCapacity, int maxCapacity)
        {
            int prefabId = prefab.GetInstanceID();

            if (container.HasBindingId<Pool<T>>(prefabId))
            {
                mappings[prefabId] = container.ResolveId<Pool<T>>(prefabId);
                return;
            }

            container.BindMemoryPool<T, Pool<T>>()
                .WithId(prefabId)
                .WithInitialSize(defaultCapacity)
                .WithMaxSize(maxCapacity)
                .WithFactoryArguments<Action<Pool<T>>, int, Action<T>, Action<int, T>, Action<T>>((pool) => mappings[prefabId] = pool, prefabId, OnSpawned, OnCreated, OnDespawned)
                .FromComponentInNewPrefab(prefab)
                .UnderTransformGroup($"{typeof(T).Name} Pool");

            container.ResolveId<Pool<T>>(prefabId);
        }
    }
}
