using System;
using System.Collections.Generic;
using MyToolz.DesignPatterns.MVP.Model;

namespace MyToolz.UI.Notifications.Model
{
    public struct ActiveEntry
    {
        public int Id;
        public NotificationPriority Priority;
        public string Key;
        public Type MessageType;
    }

    public struct PendingEntry
    {
        public NotificationData Request;
        public string Key;
    }

    public enum AddResult
    {
        Spawned,
        Enqueued,
        Dropped,
        ReplacedActive
    }

    public struct AddOutcome
    {
        public AddResult Result;
        public int SpawnedId;
        public int ReplacedId;
        public string Key;
    }

    public class NotificationQueueModel : ModelBase<NotificationQueueModel>
    {
        private readonly int maxActive;
        private readonly List<ActiveEntry> active = new();
        private readonly List<PendingEntry> pending = new();
        private readonly Dictionary<string, KeyPresence> keyPresence = new();
        private int nextId;

        public IReadOnlyList<ActiveEntry> Active => active;
        public IReadOnlyList<PendingEntry> Pending => pending;
        public int ActiveCount => active.Count;
        public int PendingCount => pending.Count;
        public int MaxActive => maxActive;

        public NotificationQueueModel(int maxActive)
        {
            this.maxActive = maxActive;
        }

        public bool HasKey(string key) => keyPresence.ContainsKey(key);

        public bool HasActiveCapacity() => active.Count < maxActive;

        public string ResolveKey(NotificationData data)
        {
            return string.IsNullOrEmpty(data.Key) ? data.MessageType.FullName : data.Key;
        }

        public AddOutcome TryAdd(NotificationData data)
        {
            var key = ResolveKey(data);

            if (data.Dedupe != DedupePolicy.None && keyPresence.ContainsKey(key))
            {
                if (data.Dedupe == DedupePolicy.IgnoreIfSameKeyExists)
                    return new AddOutcome { Result = AddResult.Dropped, Key = key };

                if (data.Dedupe == DedupePolicy.ReplaceIfSameKeyExists)
                {
                    int replacedId = FindActiveByKey(key, data.MessageType);
                    if (replacedId >= 0)
                    {
                        int newId = GenerateId();
                        ReplaceActiveEntry(replacedId, newId, data, key);
                        NotifyChanged();
                        return new AddOutcome
                        {
                            Result = AddResult.ReplacedActive,
                            SpawnedId = newId,
                            ReplacedId = replacedId,
                            Key = key
                        };
                    }
                }
            }

            if (active.Count >= maxActive)
            {
                int evictedId = TryEvict(data, key);
                if (evictedId < 0 && active.Count >= maxActive)
                {
                    if (active.Count < maxActive)
                    {
                        var result = SpawnNew(data, key);
                        NotifyChanged();
                        return result;
                    }
                    EnqueuePending(data, key);
                    NotifyChanged();
                    return new AddOutcome { Result = AddResult.Enqueued, Key = key };
                }
            }

            if (active.Count < maxActive)
            {
                var result = SpawnNew(data, key);
                NotifyChanged();
                return result;
            }

            EnqueuePending(data, key);
            NotifyChanged();
            return new AddOutcome { Result = AddResult.Enqueued, Key = key };
        }

        private int TryEvict(NotificationData data, string key)
        {
            switch (data.Overflow)
            {
                case OverflowPolicy.DropNew:
                    return -1;

                case OverflowPolicy.DropOldest:
                    if (active.Count == 0) return -1;
                    var oldest = active[0];
                    RemoveActiveAt(0);
                    keyPresence.Remove(oldest.Key);
                    return oldest.Id;

                case OverflowPolicy.DropLowestPriority:
                    return EvictLowestPriority();

                case OverflowPolicy.ReplaceSameKeyOrDropNew:
                    return EvictByKey(key, null);

                default:
                    return -1;
            }
        }

        private int EvictLowestPriority()
        {
            if (active.Count == 0) return -1;

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

            var entry = active[lowestIndex];
            RemoveActiveAt(lowestIndex);
            keyPresence.Remove(entry.Key);
            return entry.Id;
        }

        private int EvictByKey(string key, Type messageType)
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (pending[i].Key != key) continue;
                if (messageType != null && pending[i].Request.MessageType != messageType) continue;
                pending.RemoveAt(i);
                keyPresence.Remove(key);
                return -1;
            }

            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i].Key != key) continue;
                if (messageType != null && active[i].MessageType != messageType) continue;
                var entry = active[i];
                RemoveActiveAt(i);
                keyPresence.Remove(key);
                return entry.Id;
            }

            return -1;
        }

        public int RemoveByKey(string key, Type messageType = null)
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (pending[i].Key != key) continue;
                if (messageType != null && pending[i].Request.MessageType != messageType) continue;
                pending.RemoveAt(i);
                keyPresence.Remove(key);
                NotifyChanged();
                return -1;
            }

            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i].Key != key) continue;
                if (messageType != null && active[i].MessageType != messageType) continue;
                return active[i].Id;
            }

            return -1;
        }

        public bool RemoveActiveById(int id, out ActiveEntry removed)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Id == id)
                {
                    removed = active[i];
                    RemoveActiveAt(i);
                    keyPresence.Remove(removed.Key);
                    NotifyChanged();
                    return true;
                }
            }
            removed = default;
            return false;
        }

        public PendingEntry? DequeuePending()
        {
            if (pending.Count == 0) return null;
            var next = pending[0];
            pending.RemoveAt(0);
            return next;
        }

        public List<int> GetSortedActiveIds()
        {
            var sorted = new List<ActiveEntry>(active);
            sorted.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            var ids = new List<int>(sorted.Count);
            for (int i = 0; i < sorted.Count; i++)
                ids.Add(sorted[i].Id);
            return ids;
        }

        public override NotificationQueueModel Clone()
        {
            return new NotificationQueueModel(maxActive);
        }

        public override void Reset()
        {
            active.Clear();
            pending.Clear();
            keyPresence.Clear();
            nextId = 0;
            NotifyChanged();
        }

        private AddOutcome SpawnNew(NotificationData data, string key)
        {
            int id = GenerateId();
            keyPresence[key] = KeyPresence.Active;
            active.Add(new ActiveEntry
            {
                Id = id,
                Priority = data.Priority,
                Key = key,
                MessageType = data.MessageType
            });
            return new AddOutcome { Result = AddResult.Spawned, SpawnedId = id, Key = key };
        }

        private void EnqueuePending(NotificationData data, string key)
        {
            keyPresence[key] = KeyPresence.Pending;
            int index = pending.FindIndex(p => (int)p.Request.Priority < (int)data.Priority);
            var entry = new PendingEntry { Request = data, Key = key };
            if (index < 0) pending.Add(entry);
            else pending.Insert(index, entry);
        }

        private int FindActiveByKey(string key, Type messageType)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Key != key) continue;
                if (messageType != null && active[i].MessageType != messageType) continue;
                return active[i].Id;
            }
            return -1;
        }

        private void ReplaceActiveEntry(int oldId, int newId, NotificationData data, string key)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Id != oldId) continue;
                active[i] = new ActiveEntry
                {
                    Id = newId,
                    Priority = data.Priority,
                    Key = key,
                    MessageType = data.MessageType
                };
                return;
            }
        }

        private void RemoveActiveAt(int index)
        {
            active.RemoveAt(index);
        }

        private int GenerateId() => nextId++;

        private enum KeyPresence : byte { Pending, Active }
    }
}
