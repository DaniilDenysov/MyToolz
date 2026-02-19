using MyToolz.GameSettings.Data;
using MyToolz.IO;
using MyToolz.ScriptableObjects.GameSettings;
using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace MyToolz.GameSettings.Data
{
    [Serializable]
    public class SavableData
    {
        public List<(string id, object value)> data = new();
    }
}

namespace MyToolz.GameSettings
{
    public class SettingsPresenter : MonoBehaviour
    {
        [SerializeField] private SettingSOAbstract [] settings;

        private Dictionary<string, SettingSOAbstract> savableComponents = new Dictionary<string, SettingSOAbstract>();
        private SavableData cached;
        private ISaver<SavableData> saver;

        [Inject]
        private void Construct(ISaver<SavableData> saver)
        {
            this.saver = saver;
        }

        private void Awake()
        {
            foreach (var setting in settings)
            {
                if (setting == null)
                {
                    DebugUtility.LogError(this, $"Unable to initialize setting, null exception!");
                    continue;
                }
                if (!savableComponents.TryAdd(setting.ID, setting))
                {
                    DebugUtility.LogError(this, $"Unable to initialize setting, value is not unique!");
                    continue;
                }
            }
        }

        private void Start()
        {
            cached = saver.Load();

            if (cached == null)
            {
                DebugUtility.LogError(this, "Cache is missing, creating fallback!");
                cached = new SavableData();
            }

            if (cached.data == null)
                cached.data = new();

            if (cached.data.Count == 0) return;

            foreach (var data in cached.data)
            {
                if (!savableComponents.TryGetValue(data.Item1, out var saverComp)) continue;
                if (saverComp == null)
                {
                    DebugUtility.LogError(this, "Saver is null!");
                    continue;
                }
                saverComp.Load(data);
            }
        }

        public void Save()
        {
            RefreshCache();
            saver.Save();
        }

        private void RefreshCache()
        {
            if (cached == null)
                cached = saver.Load() ?? new SavableData();

            if (cached.data == null)
                cached.data = new();

            cached.data = savableComponents.Values.Select(c => c.Save()).ToList();
        }
    }
}
