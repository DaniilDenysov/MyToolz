using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.Utilities.Debug
{
    public class LogGateSettingsSO : ScriptableObject
    {

        [Serializable]
        public struct Entry
        {
            public string path;
            public bool enabled;
        }

        [SerializeField] private bool defaultEnabled = true;
        [SerializeField] private List<Entry> entries = new List<Entry>();

        public bool DefaultEnabled => defaultEnabled;
        public IReadOnlyList<Entry> Entries => entries;

        public void SetDefaultEnabled(bool enabled) => defaultEnabled = enabled;

        public void Set(string path, bool enabled)
        {
            path = Normalize(path);
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].path, path, StringComparison.Ordinal))
                {
                    entries[i] = new Entry { path = path, enabled = enabled };
                    return;
                }
            }
            entries.Add(new Entry { path = path, enabled = enabled });
        }

        public bool TryGet(string path, out bool enabled)
        {
            path = Normalize(path);
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].path, path, StringComparison.Ordinal))
                {
                    enabled = entries[i].enabled;
                    return true;
                }
            }
            enabled = default;
            return false;
        }

        public void Remove(string path)
        {
            path = Normalize(path);
            entries.RemoveAll(e => string.Equals(e.path, path, StringComparison.Ordinal));
        }

        private string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "*";
            s = s.Trim();
            return s;
        }
    }
}
