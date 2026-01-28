using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.Core
{
    public abstract class MonoBehaviourPlus : MonoBehaviour
    {
        [SerializeField] private bool enableLogging = true;
        protected Transform CachedTransform => _cachedTransform ? _cachedTransform : (_cachedTransform = transform);
        protected GameObject CachedGameObject => _cachedGameObject ? _cachedGameObject : (_cachedGameObject = gameObject);

        private Transform _cachedTransform;
        private GameObject _cachedGameObject;

        private Dictionary<Type, Component> _componentCache;

        protected T GetComponent<T>(bool cacheMiss = true) where T : Component
        {
            _componentCache ??= new Dictionary<Type, Component>(8);
            var type = typeof(T);

            if (_componentCache.TryGetValue(type, out var cached))
                return cached as T;

            var found = GetComponent<T>();
            if (found != null || cacheMiss)
                _componentCache[type] = found;

            return found;
        }

        protected bool TryGetComponentCached<T>(out T component, bool cacheMiss = true) where T : Component
        {
            component = GetComponent<T>(cacheMiss);
            return component != null;
        }

        protected T GetComponentInChildren<T>(bool includeInactive = false, bool cacheMiss = true) where T : Component
        {
            _componentCache ??= new Dictionary<Type, Component>(8);
            var type = typeof(T);
            var key = TypeKey.Child(type);

            if (_componentCache.TryGetValue(key, out var cached))
                return cached as T;

            var found = GetComponentInChildren<T>(includeInactive);
            if (found != null || cacheMiss)
                _componentCache[key] = found;

            return found;
        }

        protected T GetComponentInParent<T>(bool includeInactive = false, bool cacheMiss = true) where T : Component
        {
            _componentCache ??= new Dictionary<Type, Component>(8);
            var type = typeof(T);
            var key = TypeKey.Parent(type);

            if (_componentCache.TryGetValue(key, out var cached))
                return cached as T;

#if UNITY_2021_2_OR_NEWER
            var found = GetComponentInParent<T>(includeInactive);
#else
        var found = GetComponentInParent<T>();
#endif

            if (found != null || cacheMiss)
                _componentCache[key] = found;

            return found;
        }

        protected void ClearComponentCache()
        {
            _componentCache?.Clear();
        }

        protected void Log(string message)
        {
            if (!enableLogging) return;
            DebugUtility.Log(this, message);
        }

        protected void LogError(string message)
        {
            if (!enableLogging) return;
            DebugUtility.LogError(this, message);
        }

        protected void LogWarning(string message)
        {
            if (!enableLogging) return;
            DebugUtility.LogWarning(this, message);
        }

        private static class TypeKey
        {
            public static Type Child(Type t) => typeof(ChildKey<>).MakeGenericType(t);
            public static Type Parent(Type t) => typeof(ParentKey<>).MakeGenericType(t);

            private sealed class ChildKey<T> { }
            private sealed class ParentKey<T> { }
        }
    }
}
