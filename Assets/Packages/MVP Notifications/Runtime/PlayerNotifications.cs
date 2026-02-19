using MyToolz.DesignPatterns.EventBus;
using MyToolz.EditorToolz;
using MyToolz.Events;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.UI.Events;
using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.UI.Events
{
    public enum NotificationPriority
    {
        /// <summary>
        /// Non-critical informational messages.
        /// Example: ammo pickups, interaction hints, movement feedback.
        /// </summary>
        Low = 0,

        /// <summary>
        /// Default importance level
        /// Example: neutral system messages or standard gameplay feedback.
        /// </summary>
        Normal = 10,

        /// <summary>
        /// Important gameplay events that should usually be shown.
        /// Example: kills
        /// </summary>
        High = 20,

        /// <summary>
        /// Must-be-seen messages.
        /// Example: critical alerts
        /// </summary>
        Critical = 30
    }

    public enum OverflowPolicy
    {
        None,
        /// <summary>
        /// Discards the incoming notification if the queue is full.
        /// Existing notifications remain untouched.
        /// </summary>
        DropNew,

        /// <summary>
        /// Removes the oldest active notification to make room
        /// for the incoming one, regardless of priority.
        /// </summary>
        DropOldest,

        /// <summary>
        /// Removes the currently active notification with the lowest priority.
        /// If multiple have the same priority, the oldest one is removed.
        /// </summary>
        DropLowestPriority,

        /// <summary>
        /// If an active or pending notification with the same key exists,
        /// it is removed and replaced by the incoming notification.
        /// If no matching key exists, the incoming notification is dropped.
        /// </summary>
        ReplaceSameKeyOrDropNew
    }

    public enum DedupePolicy
    {
        /// <summary>
        /// No deduplication.
        /// Multiple notifications with the same key may coexist.
        /// </summary>
        None,

        /// <summary>
        /// If a notification with the same key already exists
        /// (active or pending), the incoming notification is ignored.
        /// </summary>
        IgnoreIfSameKeyExists,

        /// <summary>
        /// If a notification with the same key already exists
        /// (active or pending), it is removed and replaced
        /// by the incoming notification.
        /// </summary>
        ReplaceIfSameKeyExists
    }

    public struct NotificationClearRequest : IEvent
    {
        public string Key;
        public Type MessageType;
    }


    public struct NotificationRequest : IEvent
    {
        public string Key;
        public Type MessageType;
        public string Text;
        public NotificationPriority Priority;
        public OverflowPolicy Overflow;
        public DedupePolicy Dedupe;
    }
}

namespace MyToolz.UI.Notifications.View
{
    public class PlayerNotifications : MonoBehaviour, IEventListener
    {
        [FoldoutGroup("Config"), SerializeField] private Transform container;
        [FoldoutGroup("Config"), SerializeField] private Notification[] notificationPrefabs;
        [FoldoutGroup("Config"), SerializeField, Range(1, 10)] private int maxActive = 2;

        private Dictionary<Type, Notification> prefabMap = new();
        private List<ActiveEntry> active = new();
        private List<PendingEntry> pending = new();
        private Dictionary<string, KeyPresence> keyPresence = new();

        private EventBinding<NotificationRequest> notificationBinding;
        private EventBinding<NotificationClearRequest> notificationClearBinding;

        private void Awake()
        {
            InitializePrefabMap();
        }

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        public void RegisterEvents()
        {
            notificationBinding = new EventBinding<NotificationRequest>(OnNotificationRequested);
            EventBus<NotificationRequest>.Register(notificationBinding);
            notificationClearBinding = new EventBinding<NotificationClearRequest>(OnNotificationClearRequest);
            EventBus<NotificationClearRequest>.Register(notificationClearBinding);
        }

        public void UnregisterEvents()
        {
            EventBus<NotificationRequest>.Deregister(notificationBinding);
            EventBus<NotificationClearRequest>.Deregister(notificationClearBinding);
        }

        private void InitializePrefabMap()
        {
            prefabMap.Clear();

            foreach (var prefab in notificationPrefabs)
            {
                var keyType = prefab.GetMessageType();
                if (keyType == null)
                {
                    DebugUtility.LogWarning(this, "Notification prefab has null message type (strategy missing?)");
                    continue;
                }

                if (!prefabMap.TryAdd(keyType, prefab))
                {
                    DebugUtility.LogWarning(this, $"Duplicate notification prefab mapping for type: {keyType}");
                }
            }
        }

        private void OnNotificationClearRequest(NotificationClearRequest e)
        {
            if (string.IsNullOrEmpty(e.Key))
            {
                DebugUtility.LogWarning(this, "NotificationClear.Key is null/empty");
                return;
            }
            RemoveByKey(e.Key, e.MessageType);
        }

        private void OnNotificationRequested(NotificationRequest e)
        {
            if (e.MessageType == null)
            {
                DebugUtility.LogError(this, "NotificationRequestEvent.MessageType is null");
                return;
            }

            if (!prefabMap.TryGetValue(e.MessageType, out var prefab))
            {
                DebugUtility.LogWarning(this, $"No notification prefab mapped for type: {e.MessageType}");
                return;
            }

            var key = string.IsNullOrEmpty(e.Key) ? e.MessageType.FullName : e.Key;

            if (e.Dedupe != DedupePolicy.None && keyPresence.ContainsKey(key))
            {
                if (e.Dedupe == DedupePolicy.IgnoreIfSameKeyExists)
                    return;

                if (e.Dedupe == DedupePolicy.ReplaceIfSameKeyExists)
                {
                    if (InsertOrReplaceActive(prefab, e, key))
                        return;
                }

            }

            if (active.Count >= maxActive)
            {
                switch (e.Overflow)
                {
                    case OverflowPolicy.DropNew:
                        return;

                    case OverflowPolicy.DropOldest:
                        RemoveOldestActive();
                        break;

                    case OverflowPolicy.DropLowestPriority:
                        RemoveLowestPriorityActive();
                        break;

                    case OverflowPolicy.ReplaceSameKeyOrDropNew:
                        RemoveByKey(key);
                        break;

                    default:
                        return;
                }
            }

            if (active.Count < maxActive)
            {
                Spawn(prefab, e, key);
            }
            else
            {
                EnqueuePending(prefab, e, key);
            }
        }

        private bool InsertOrReplaceActive(Notification prefab, NotificationRequest e, string key)
        {
            for (int i = 0; i < active.Count; i++)
            {
                var entry = active[i];

                if (entry.Key != key)
                    continue;

                if (e.MessageType != null && entry.MessageType != e.MessageType)
                    continue;

                var oldInstance = entry.Instance;
                var siblingIndex = oldInstance.transform.GetSiblingIndex();

                oldInstance.OnHidden = null;

                EventBus<ReleaseRequest<Notification>>.Raise(new ReleaseRequest<Notification>
                {
                    PoolObject = oldInstance
                });

                EventBus<PoolRequest<Notification>>.Raise(new PoolRequest<Notification>
                {
                    Prefab = prefab,
                    Parent = container,
                    Callback = newInstance =>
                    {
                        entry.Instance = newInstance;
                        entry.Priority = e.Priority;
                        entry.MessageType = e.MessageType;

                        newInstance.OnHidden = () =>
                        {
                            if (!RemoveActiveInstance(newInstance, out var removed))
                                return;

                            keyPresence.Remove(removed.Key);

                            EventBus<ReleaseRequest<Notification>>.Raise(new ReleaseRequest<Notification>
                            {
                                PoolObject = newInstance
                            });

                            TryPromotePending();
                        };

                        newInstance.SetMessage(e.Text);

                        active[i] = entry;
                        newInstance.transform.SetSiblingIndex(siblingIndex);
                    }
                });

                return true;
            }

            return false;
        }


        private void Spawn(Notification prefab, NotificationRequest e, string key)
        {
            keyPresence[key] = KeyPresence.Active;

            EventBus<PoolRequest<Notification>>.Raise(new PoolRequest<Notification>
            {
                Prefab = prefab,
                Parent = container,
                Callback = instance =>
                {
                    var entry = new ActiveEntry()
                    {
                        Instance = instance, 
                        Priority = e.Priority,
                        Key = key,
                        MessageType = e.MessageType
                    };

                    instance.OnHidden = () =>
                    {
                        if (!RemoveActiveInstance(instance, out var removedEntry))
                            return;

                        keyPresence.Remove(removedEntry.Key);

                        EventBus<ReleaseRequest<Notification>>.Raise(new ReleaseRequest<Notification>
                        {
                            PoolObject = instance
                        });

                        TryPromotePending();
                    };

                    instance.SetMessage(e.Text);
                    active.Add(entry);
                    ReorderContainerHierarchy();
                }
            });
        }

        private void EnqueuePending(Notification prefab, NotificationRequest request, string key)
        {
            keyPresence[key] = KeyPresence.Pending;
            var pendingEntry = new PendingEntry()
            {
                Prefab = prefab,
                Request = request,
                Key = key
            };
            int index = pending.FindIndex(p => (int)p.Priority < (int)request.Priority);
            if (index < 0) pending.Add(pendingEntry);
            else pending.Insert(index, pendingEntry);
        }

        private void TryPromotePending()
        {
            while (active.Count < maxActive && pending.Count > 0)
            {
                var next = pending[0];
                pending.RemoveAt(0);
                keyPresence[next.Key] = KeyPresence.Active;
                Spawn(next.Prefab, next.Request, next.Key);
            }
        }

        private void RemoveOldestActive()
        {
            if (active.Count == 0) return;
            var oldest = active[0];
            ForceRelease(oldest.Instance, oldest.Key);
        }

        private void RemoveLowestPriorityActive()
        {
            if (active.Count == 0) return;

            int lowestIndex = 0;
            int lowestPriority = (int)active[0].Priority;

            for (int i = 1; i < active.Count; i++)
            {
                int pr = (int)active[i].Priority;
                if (pr < lowestPriority)
                {
                    lowestPriority = pr;
                    lowestIndex = i;
                }
            }

            var obj = active[lowestIndex];
            ForceRelease(obj.Instance, obj.Key);
        }

        private void RemoveByKey(string key, Type messageType = null)
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (pending[i].Key != key) continue;
                if (messageType != null && pending[i].Request.MessageType != messageType) continue;

                pending.RemoveAt(i);
                keyPresence.Remove(key);
                return;
            }

            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i].Key != key) continue;
                if (messageType != null && active[i].MessageType != messageType) continue;

                active[i].Instance.Stop();
                return;
            }
        }


        private void ForceRelease(Notification instance, string key)
        {
            instance.OnHidden = null;
            RemoveActiveInstance(instance, out _);
            keyPresence.Remove(key);
            EventBus<ReleaseRequest<Notification>>.Raise(new ReleaseRequest<Notification>
            {
                PoolObject = instance
            });

            TryPromotePending();
        }

        private bool RemoveActiveInstance(Notification instance, out ActiveEntry removed)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Instance == instance)
                {
                    removed = active[i];
                    active.RemoveAt(i);
                    return true;
                }
            }
            removed = default;
            return false;
        }

        private void OnDestroy()
        {
            OnDisable();
            for (int i = 0; i < active.Count; i++)
            {
                var instance = active[i].Instance;
                if (instance == null)
                    continue;

                instance.OnHidden = null;

                EventBus<ReleaseRequest<Notification>>.Raise(new ReleaseRequest<Notification>
                {
                    PoolObject = instance
                });
            }
        }

        private void ReorderContainerHierarchy()
        {
            active.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            for (int i = 0; i < active.Count; i++)
            {
                var instance = active[i].Instance;
                if (instance == null)
                    continue;

                var tr = instance.transform;
                if (tr.parent == container)
                    tr.SetSiblingIndex(i);
            }
        }

        private enum KeyPresence : byte { Pending, Active }

        private struct ActiveEntry
        {
            public Notification Instance;
            public NotificationPriority Priority;
            public string Key;
            public Type MessageType;
        }


        private struct PendingEntry
        {
            public Notification Prefab;
            public NotificationRequest Request;
            public NotificationPriority Priority;
            public string Key;
        }
    }
}
