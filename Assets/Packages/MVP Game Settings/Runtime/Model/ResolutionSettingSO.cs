using MyToolz.EditorToolz;
using MyToolz.GameSettings.Data;
using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyToolz.ScriptableObjects.GameSettings
{
    [Serializable]
    public class CurrentResolution
    {
        public int Index;
        public string Name;

        public int Width;
        public int Height;

        public int RefreshRate;
    }

    [CreateAssetMenu(fileName = "ResolutionSettingSO", menuName = "MyToolz/GameSettings/ResolutionSettingSO")]
    public class ResolutionSettingSO : MultipleOptionSettingSO<CurrentResolution, List<CurrentResolution>>
    {
        public IReadOnlyList<string> OptionNames => options.Select(o => o.Name).ToList();

        public int CurrentIndex
        {
            get
            {
                if (currentValue == null)
                {
                    return 0;
                }
                int idx = options.IndexOf(currentValue);
                return idx < 0 ? 0 : idx;
            }
        }

        public override string ValueToString(CurrentResolution value)
        {
            return value.Name;
        }

#if UNITY_EDITOR
        [Button("Refresh Default Resolutions")]
        public void RefreshDefaultResolutions()
        {
            if (options == null)
            {
                options = new List<CurrentResolution>();
            }
            options.Clear();

            Dictionary<string, Resolution> unique = new();

            foreach (Resolution r in Screen.resolutions.Reverse())
            {
                string key = $"{r.width}x{r.height}";
                if (!unique.ContainsKey(key))
                {
                    unique.Add(key, r);
                }
            }

            int i = 0;
            int bestIndex = 0;

            foreach (KeyValuePair<string, Resolution> kv in unique)
            {
                Resolution r = kv.Value;

                CurrentResolution entry = new CurrentResolution
                {
                    Index = i,
                    Width = r.width,
                    Height = r.height,
                    RefreshRate = GetRefreshRateInt(r),
                    Name = $"{r.width} x {r.height}"
                };

                options.Add(entry);

                if (r.width == Screen.width && r.height == Screen.height)
                {
                    bestIndex = i;
                }

                i++;
            }

            if (options.Count > 0)
            {
                defaultValue = options[Mathf.Clamp(bestIndex, 0, options.Count - 1)];
            }
        }
#endif

        public override void SetCurrentValue(CurrentResolution newValue)
        {
            if (!IsValueValid(newValue))
            {
                DebugUtility.LogError(this, $"Invalid value on {settingName}, it will not be accepted!");
                return;
            }
            base.SetCurrentValue(newValue);
            ApplyCurrent();
        }

        private void ApplyCurrent()
        {
            Screen.SetResolution(currentValue.Width, currentValue.Height, Screen.fullScreenMode);
        }

        protected override void OnLoaded()
        {
            ApplyCurrent();
        }

        public override SettingEntry Save()
        {
            if (!IsCurrentValueValid())
            {
                DebugUtility.LogWarning(this, $"Invalid value on {settingName}, it will fallback to default value");
            }
            return new SettingEntry(ID, CurrentIndex);
        }

        public override void Load(SettingEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
            {
                DebugUtility.LogError(this, $"Loaded entry on {settingName} is null or has empty ID!");
                return;
            }
            if (!string.Equals(id, entry.Id))
            {
                DebugUtility.LogError(this, $"ID mismatch on {settingName}, loaded {entry.Id} doesn't match {id}");
                return;
            }
            try
            {
                int idx = entry.GetValue<int>();
                idx = Mathf.Clamp(idx, 0, AvailableOptions.Count - 1);
                SetCurrentValue(AvailableOptions[idx]);
            }
            catch (Exception e)
            {
                DebugUtility.LogError(this, $"Deserialization error for {settingName} with ID {entry.Id}: {e}");
            }
            NotifyValueUpdated();
            OnLoaded();
        }

        protected override bool IsValueValid(CurrentResolution value)
        {
            return value != null;
        }

        private static int GetRefreshRateInt(Resolution r)
        {
#if UNITY_2022_2_OR_NEWER
            return (int)r.refreshRateRatio.value;
#else
            return r.refreshRate;
#endif
        }
    }
}
