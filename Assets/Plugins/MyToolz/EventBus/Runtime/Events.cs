using MyToolz.DesignPatterns.EventBus;
using System;
using UnityEngine;

namespace MyToolz.DesignPatterns.EventBus
{
    public interface IEvent { }
}

namespace MyToolz.Events
{
    public struct PoolRequest<T> : IEvent
    {
        public T Prefab;
        public Vector3 Position;
        public Quaternion Rotation;
        public Transform Parent;
        public Action<T> Callback;
    }

    public struct ReleaseRequest<T> : IEvent
    {
        public T PoolObject;
        public Action<T> Callback;
    }
}