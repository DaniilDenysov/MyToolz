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
            foreach (var poolObj in poolObjects)
            {
                if (poolObj.Prefab == null)
                {
                    DebugUtility.LogWarning(this,"Prefab is null in PoolObject configuration.");
                    continue;
                }

                int prefabId = poolObj.Prefab.GetInstanceID();

                if (container.HasBindingId<Pool<T>>(prefabId))
                {
                    mappings[prefabId] = container.ResolveId<Pool<T>>(prefabId);
                    continue;
                }

                container.BindMemoryPool<T, Pool<T>>()
                    .WithId(prefabId)
                    .WithInitialSize(poolObj.DefaultCapacity)
                    .WithFactoryArguments<Action<Pool<T>>, int, Action<T>, Action<int, T>, Action<T>>((pool) => mappings.Add(prefabId, pool), prefabId, OnSpawned, OnCreated, OnDespawned)
                    .FromComponentInNewPrefab(poolObj.Prefab)
                    .UnderTransformGroup($"{typeof(T).Name} Pool");

                container.ResolveId<Pool<T>>(prefabId);
            }
        }
    }
}
