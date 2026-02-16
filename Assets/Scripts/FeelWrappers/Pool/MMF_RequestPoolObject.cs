#if FEEL_PRESENT
using MoreMountains.Feedbacks;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.FeelWrappers.Pool
{
    public abstract class MMF_RequestPoolObject<T> : MMF_Feedback
    {
        [MMFInspectorGroup("Pool", true, 14, true)]
        [SerializeField] protected T prefab;
        [SerializeField] protected Transform position;

        protected override void CustomPlayFeedback(Vector3 pos, float feedbacksIntensity = 1f)
        {
            if (!Active)
                return;

            if (prefab == null)
            {
                DebugUtility.LogWarning(this," MMF_RequestPoolObject: Prefab is not assigned.");
                return;
            }

            Transform t = position;
            Vector3 spawnPos = t != null ? t.position : pos;

            EventBus<PoolRequest<T>>.Raise(new PoolRequest<T>()
            {
                Prefab = prefab,
                Position = spawnPos,
                Callback = (obj) => OnSpawned(obj)
            });
        }

        protected virtual void OnSpawned(T obj)
        {

        }
    }
}
#endif