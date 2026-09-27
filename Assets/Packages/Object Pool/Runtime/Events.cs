using MyToolz.DesignPatterns.EventBus;
using System;
using UnityEngine;

namespace MyToolz.Events
{
    public struct PoolRequest<T> : IEvent
    {
        public T Prefab;

        /// <summary>Addressable runtime key; when set it is used instead of <see cref="Prefab"/>.</summary>
        public object Key;
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

    public struct PoolAllRequest<T> : IEvent
    {
        public Action<T> Callback;
    }

    /// <summary>
    /// Raised when a pool installer could not build its pools (for example an Addressable prefab
    /// failed to load). Requests for that pool are refused with an error, so a game can listen for
    /// this and tell the player rather than leave a board silently empty.
    /// </summary>
    public struct PoolInitializationFailed : IEvent
    {
        public Type ItemType;
        public string Reason;
    }
}