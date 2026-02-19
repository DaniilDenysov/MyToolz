using MyToolz.EditorToolz;
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
                if (currentValue == null) return 0;
                var idx = options.IndexOf(currentValue);
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
            if (options == null) options = new List<CurrentResolution>();
            options.Clear();

            var unique = new Dictionary<string, Resolution>();

            foreach (var r in Screen.resolutions.Reverse())
            {
                string key = $"{r.width}x{r.height}";
                if (!unique.ContainsKey(key))
                    unique.Add(key, r);
            }

            int i = 0;
            int bestIndex = 0;

            foreach (var kv in unique)
            {
                var r = kv.Value;

                var entry = new CurrentResolution
                {
                    Index = i,
                    Width = r.width,
                    Height = r.height,
                    RefreshRate = GetRefreshRateInt(r),
                    Name = $"{r.width} x {r.height}"
                };

                options.Add(entry);

                if (r.width == Screen.width && r.height == Screen.height)
                    bestIndex = i;

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
            Screen.SetResolution(currentValue.Width, currentValue.Height, Screen.fullScreenMode);
        }

        public override (string id, object value) Save()
        {
            if (!IsCurrentValueValid())
            {
                DebugUtility.LogError(this, $"Invalid value on {settingName}, it will fallback to default value");
            }
            return (ID, CurrentIndex);
        }

        public override void Load((string id, object value) data)
        {
            if (string.IsNullOrEmpty(data.id) || string.IsNullOrWhiteSpace(data.id))
            {
                DebugUtility.LogError(this, $"Loaded ID on {settingName} is null or empty!");
                return;
            }
            if (!string.Equals(id, data.id))
            {
                DebugUtility.LogError(this, $"ID missmatch on {settingName}, loaded {data.id} doesn't match {id}");
                return;
            }
            try
            {
                int idx = 0;
                if (data.value is int i) idx = i;
                else if (data.value is long l) idx = (int)l;
                else if (data.value is float f) idx = (int)f;
                else if (data.value is double d) idx = (int)d;
                else
                {
                    DebugUtility.LogWarning(this, $"Resolution load: unsupported type {data.value?.GetType()} - falling back to default.");
                    idx = DefaultValueIndex;
                }

                idx = Mathf.Clamp(idx, 0, AvailableOptions.Count - 1);
                SetCurrentValue(AvailableOptions[idx]);
            }
            catch (Exception e)
            {
                DebugUtility.LogError(this, $"Unexpected cast error for {settingName} with ID {data.id} with error: {e}");
            }
            NotifyValueUpdated();
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
