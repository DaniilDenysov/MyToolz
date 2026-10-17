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

    /// <summary>
    /// Active notifications (at most <see cref="MaxActive"/>) plus a priority-ordered pending queue (at
    /// most <see cref="MaxPending"/>). Keys are counted per entry, so several notifications may share a
    /// key under <see cref="DedupePolicy.None"/> and removing one does not hide the others from dedupe.
    /// </summary>
    public class NotificationQueueModel : ModelBase<NotificationQueueModel>
    {
        public const int DefaultMaxPending = 32;

        private readonly int maxActive;
        private readonly int maxPending;
        private readonly List<ActiveEntry> active = new();
        private readonly List<PendingEntry> pending = new();
        private readonly Dictionary<string, int> keyCounts = new();
        private int nextId;

        public IReadOnlyList<ActiveEntry> Active => active;
        public IReadOnlyList<PendingEntry> Pending => pending;
        public int ActiveCount => active.Count;
        public int PendingCount => pending.Count;
        public int MaxActive => maxActive;
        public int MaxPending => maxPending;

        public NotificationQueueModel(int maxActive) : this(maxActive, DefaultMaxPending) { }

        public NotificationQueueModel(int maxActive, int maxPending)
        {
            this.maxActive = Math.Max(1, maxActive);
            this.maxPending = Math.Max(0, maxPending);
        }

        /// <summary>True while any active or pending notification uses <paramref name="key"/>.</summary>
        public bool HasKey(string key) => key != null && keyCounts.ContainsKey(key);

        public bool HasActiveCapacity() => active.Count < maxActive;

        public string ResolveKey(NotificationData data)
        {
            return string.IsNullOrEmpty(data.Key) ? data.MessageType?.FullName ?? string.Empty : data.Key;
        }

        public AddOutcome TryAdd(NotificationData data)
        {
            var key = ResolveKey(data);

            if (data.Dedupe != DedupePolicy.None && HasKey(key))
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

                    // The existing entry is still waiting: replace it in the queue instead of adding a second one.
                    if (TryReplacePending(data, key))
                    {
                        NotifyChanged();
                        return new AddOutcome { Result = AddResult.Enqueued, Key = key };
                    }
                }
            }

            if (active.Count < maxActive)
            {
                var result = SpawnNew(data, key);
                NotifyChanged();
                return result;
            }

            int evictedId = TryEvict(data, key);
            if (active.Count < maxActive)
            {
                var result = SpawnNew(data, key);
                if (evictedId >= 0)
                {
                    result.Result = AddResult.ReplacedActive;
                    result.ReplacedId = evictedId;
                }
                NotifyChanged();
                return result;
            }

            // Nothing active ranks below the new notification, so it is the lowest priority one.
            if (data.Overflow == OverflowPolicy.DropLowestPriority)
            {
                return new AddOutcome { Result = AddResult.Dropped, Key = key };
            }

            if (data.Overflow == OverflowPolicy.ReplaceSameKeyOrDropNew)
            {
                if (TryReplacePending(data, key))
                {
                    NotifyChanged();
                    return new AddOutcome { Result = AddResult.Enqueued, Key = key };
                }
                return new AddOutcome { Result = AddResult.Dropped, Key = key };
            }

            if (data.Overflow == OverflowPolicy.DropNew)
            {
                return new AddOutcome { Result = AddResult.Dropped, Key = key };
            }

            if (!EnqueuePending(data, key))
            {
                return new AddOutcome { Result = AddResult.Dropped, Key = key };
            }

            NotifyChanged();
            return new AddOutcome { Result = AddResult.Enqueued, Key = key };
        }

        private int TryEvict(NotificationData data, string key)
        {
            switch (data.Overflow)
            {
                case OverflowPolicy.DropOldest:
                    if (active.Count == 0) return -1;
                    var oldest = active[0];
                    RemoveActiveAt(0);
                    return oldest.Id;

                case OverflowPolicy.DropLowestPriority:
                    return EvictLowestPriority(data.Priority);

                case OverflowPolicy.ReplaceSameKeyOrDropNew:
                    return EvictActiveByKey(key, null);

                default:
                    return -1;
            }
        }

        // Only evicts an entry of strictly lower priority than the incoming one.
        private int EvictLowestPriority(NotificationPriority incoming)
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

            if (lowestPriority >= (int)incoming) return -1;

            var entry = active[lowestIndex];
            RemoveActiveAt(lowestIndex);
            return entry.Id;
        }

        private int EvictActiveByKey(string key, Type messageType)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i].Key != key) continue;
                if (messageType != null && active[i].MessageType != messageType) continue;
                var entry = active[i];
                RemoveActiveAt(i);
                return entry.Id;
            }

            return -1;
        }

        private bool TryReplacePending(NotificationData data, string key)
        {
            for (int i = 0; i < pending.Count; i++)
            {
                if (pending[i].Key != key) continue;
                pending.RemoveAt(i);
                InsertPending(new PendingEntry { Request = data, Key = key });
                return true;
            }
            return false;
        }

        public int RemoveByKey(string key, Type messageType = null)
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (pending[i].Key != key) continue;
                if (messageType != null && pending[i].Request.MessageType != messageType) continue;
                RemovePendingAt(i);
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
            RemovePendingAt(0);
            return next;
        }

        /// <summary>
        /// Moves the highest-priority pending notification into the active set without re-running
        /// dedupe or overflow rules (it already passed them when it was queued).
        /// </summary>
        public bool TryPromotePending(out PendingEntry promoted, out AddOutcome outcome)
        {
            promoted = default;
            outcome = default;
            if (pending.Count == 0 || active.Count >= maxActive) return false;

            promoted = pending[0];
            RemovePendingAt(0);
            outcome = SpawnNew(promoted.Request, promoted.Key);
            NotifyChanged();
            return true;
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
            return new NotificationQueueModel(maxActive, maxPending);
        }

        public override void Reset()
        {
            active.Clear();
            pending.Clear();
            keyCounts.Clear();
            nextId = 0;
            NotifyChanged();
        }

        private AddOutcome SpawnNew(NotificationData data, string key)
        {
            int id = GenerateId();
            AddKey(key);
            active.Add(new ActiveEntry
            {
                Id = id,
                Priority = data.Priority,
                Key = key,
                MessageType = data.MessageType
            });
            return new AddOutcome { Result = AddResult.Spawned, SpawnedId = id, Key = key };
        }

        private bool EnqueuePending(NotificationData data, string key)
        {
            if (maxPending == 0) return false;

            if (pending.Count >= maxPending)
            {
                // Full: make room only by dropping a strictly lower-priority entry (the last one).
                var last = pending[pending.Count - 1];
                if ((int)last.Request.Priority >= (int)data.Priority) return false;
                RemovePendingAt(pending.Count - 1);
            }

            InsertPending(new PendingEntry { Request = data, Key = key });
            return true;
        }

        // Keeps pending ordered by priority (highest first), FIFO within one priority.
        private void InsertPending(PendingEntry entry)
        {
            AddKey(entry.Key);
            int index = pending.FindIndex(p => (int)p.Request.Priority < (int)entry.Request.Priority);
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
                if (active[i].Key != key)
                {
                    RemoveKey(active[i].Key);
                    AddKey(key);
                }
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
            RemoveKey(active[index].Key);
            active.RemoveAt(index);
        }

        private void RemovePendingAt(int index)
        {
            RemoveKey(pending[index].Key);
            pending.RemoveAt(index);
        }

        private void AddKey(string key)
        {
            if (key == null) return;
            keyCounts.TryGetValue(key, out int count);
            keyCounts[key] = count + 1;
        }

        private void RemoveKey(string key)
        {
            if (key == null || !keyCounts.TryGetValue(key, out int count)) return;
            if (count <= 1) keyCounts.Remove(key);
            else keyCounts[key] = count - 1;
        }

        private int GenerateId() => nextId++;
    }
}
