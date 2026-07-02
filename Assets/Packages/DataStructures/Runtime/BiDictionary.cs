using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.DataStructures
{
    public class BiDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> forward = new Dictionary<TKey, TValue>();
        private Dictionary<TValue, TKey> reverse = new Dictionary<TValue, TKey>();

        public bool TryAdd(TKey key, TValue value)
        {
            if (forward.ContainsKey(key) || reverse.ContainsKey(value))
            {
                return false;
            }

            forward.Add(key, value);
            reverse.Add(value, key);
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return forward.TryGetValue(key, out value);
        }

        public bool TryGetKey(TValue value, out TKey key)
        {
            return reverse.TryGetValue(value, out key);
        }

        public bool Remove(TKey key)
        {
            if (forward.TryGetValue(key, out TValue value))
            {
                forward.Remove(key);
                reverse.Remove(value);
                return true;
            }
            return false;
        }

        public bool RemoveByValue(TValue value)
        {
            if (reverse.TryGetValue(value, out TKey key))
            {
                reverse.Remove(value);
                forward.Remove(key);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            forward.Clear();
            reverse.Clear();
        }

        public bool ContainsKey(TKey key)
        {
            return forward.ContainsKey(key);
        }

        public int Count => forward.Keys.Count;

        public bool ContainsValue(TValue value)
        {
            return reverse.ContainsKey(value);
        }

        public IEnumerable<TKey> Keys => forward.Keys;
        public IEnumerable<TValue> Values => reverse.Keys;
    }
}
