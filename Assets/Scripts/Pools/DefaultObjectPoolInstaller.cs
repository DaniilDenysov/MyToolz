using MyToolz.Utilities.Debug;
using System;
using UnityEngine;

namespace MyToolz.Pools
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
                    DebugUtility.LogWarning("Prefab is null in PoolObject configuration.");
                    continue;
                }

                int prefabId = poolObj.Prefab.GetInstanceID();

                Container.BindMemoryPool<T, Pool<T>>()
                    .WithId(prefabId)
                    .WithInitialSize(poolObj.DefaultCapacity)
                    .WithFactoryArguments<Action<Pool<T>>, int, Action<T>, Action<int, T>, Action<T>>((pool) => mappings.Add(prefabId, pool), prefabId, OnSpawned, OnCreated, OnDespawned)
                    .FromComponentInNewPrefab(poolObj.Prefab)
                    .UnderTransformGroup($"{typeof(T).Name} Pool")
                    .NonLazy();
            }
        }
    }


}
