using System;
using System.Collections.Generic;
using UnityEngine;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.MVP.View;
using MyToolz.EditorToolz;
using MyToolz.UI.Notifications.Model;
using MyToolz.Utilities.Debug;
using MyToolz.Events;

namespace MyToolz.UI.Notifications.View
{
    public class PlayerNotificationView : ViewBase<NotificationQueueModel>, INotificationView
    {
        [FoldoutGroup("Config"), SerializeField] private Transform container;
        [FoldoutGroup("Config"), SerializeField] private NotificationBase[] notificationPrefabs;

        private readonly Dictionary<Type, NotificationBase> prefabMap = new();
        private readonly Dictionary<int, NotificationBase> instanceMap = new();

        private void Awake()
        {
            InitializePrefabMap();
        }

        public override void UpdateView(NotificationQueueModel model) { }

        private void InitializePrefabMap()
        {
            prefabMap.Clear();

            foreach (var prefab in notificationPrefabs)
            {
                var keyType = prefab.GetType();

                if (!prefabMap.TryAdd(keyType, prefab))
                {
                    DebugUtility.LogWarning(this, $"Duplicate notification prefab mapping for type: {keyType}");
                }
            }
        }

        public bool HasPrefabForType(Type messageType)
        {
            return prefabMap.ContainsKey(messageType);
        }

        public void HandleAdded(AddOutcome outcome, NotificationData data, Action<int> onHidden)
        {
            switch (outcome.Result)
            {
                case AddResult.Spawned:
                    SpawnNotification(outcome.SpawnedId, data.MessageType, data.Text, onHidden);
                    break;

                case AddResult.ReplacedActive:
                    ReplaceNotification(outcome.ReplacedId, outcome.SpawnedId, data.MessageType, data.Text, onHidden);
                    break;
            }
        }

        public void HandleCleared(int id)
        {
            if (instanceMap.TryGetValue(id, out var instance))
                instance.Stop();
        }

        public void HandleEvicted(int id)
        {
            ForceReleaseNotification(id);
        }

        public void Reorder(NotificationQueueModel model)
        {
            var sortedIds = model.GetSortedActiveIds();

            for (int i = 0; i < sortedIds.Count; i++)
            {
                if (!instanceMap.TryGetValue(sortedIds[i], out var instance))
                    continue;

                var tr = instance.transform;
                if (tr.parent == container)
                    tr.SetSiblingIndex(i);
            }
        }

        public void ReleaseAll()
        {
            foreach (var kvp in instanceMap)
            {
                var instance = kvp.Value;
                if (instance == null)
                    continue;

                instance.OnHidden = null;

                EventBus<ReleaseRequest<NotificationBase>>.Raise(new ReleaseRequest<NotificationBase>
                {
                    PoolObject = instance
                });
            }
            instanceMap.Clear();
        }

        public void ForceReleaseNotification(int id)
        {
            if (!instanceMap.TryGetValue(id, out var instance))
                return;

            instance.OnHidden = null;
            instanceMap.Remove(id);

            EventBus<ReleaseRequest<NotificationBase>>.Raise(new ReleaseRequest<NotificationBase>
            {
                PoolObject = instance
            });
        }

        private void SpawnNotification(int id, Type messageType, string text, Action<int> onHidden)
        {
            if (!prefabMap.TryGetValue(messageType, out var prefab))
            {
                DebugUtility.LogWarning(this, $"No notification prefab mapped for type: {messageType}");
                return;
            }

            EventBus<PoolRequest<NotificationBase>>.Raise(new PoolRequest<NotificationBase>
            {
                Prefab = prefab,
                Parent = container,
                Callback = instance =>
                {
                    instanceMap[id] = instance;
                    instance.OnHidden = () => onHidden?.Invoke(id);
                    instance.SetMessage(text);
                }
            });
        }

        private void ReplaceNotification(int oldId, int newId, Type messageType, string text, Action<int> onHidden)
        {
            if (!prefabMap.TryGetValue(messageType, out var prefab))
            {
                DebugUtility.LogWarning(this, $"No notification prefab mapped for type: {messageType}");
                return;
            }

            int siblingIndex = -1;
            if (instanceMap.TryGetValue(oldId, out var oldInstance))
            {
                siblingIndex = oldInstance.transform.GetSiblingIndex();
                oldInstance.OnHidden = null;
                instanceMap.Remove(oldId);

                EventBus<ReleaseRequest<NotificationBase>>.Raise(new ReleaseRequest<NotificationBase>
                {
                    PoolObject = oldInstance
                });
            }

            EventBus<PoolRequest<NotificationBase>>.Raise(new PoolRequest<NotificationBase>
            {
                Prefab = prefab,
                Parent = container,
                Callback = newInstance =>
                {
                    instanceMap[newId] = newInstance;
                    newInstance.OnHidden = () => onHidden?.Invoke(newId);
                    newInstance.SetMessage(text);

                    if (siblingIndex >= 0)
                        newInstance.transform.SetSiblingIndex(siblingIndex);
                }
            });
        }
    }
}
