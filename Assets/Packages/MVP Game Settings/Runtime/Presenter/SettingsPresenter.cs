using MyToolz.EditorToolz;
using MyToolz.GameSettings.Data;
using MyToolz.IO;
using MyToolz.ScriptableObjects.GameSettings;
using MyToolz.Utilities.Debug;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace MyToolz.GameSettings
{
    public class SettingsPresenter : MonoBehaviour
    {
        [SerializeField] private SettingSOAbstract[] settings;
        [SerializeField, Min(0f), Tooltip("Seconds (unscaled) to wait after the last setting change before saving. " +
            "Coalesces slider drags into one write. Pause, focus loss, quit and destroy flush immediately.")]
        private float saveDelay = 0.5f;

        private readonly Dictionary<string, SettingSOAbstract> savableComponents = new();
        private ISaver<SavableData> saver;
        private bool hasLoaded;
        private bool dirty;
        private float saveAt;

        public bool IsLoaded => hasLoaded;
        public LoadStatus LoadStatus { get; private set; } = LoadStatus.NotAttempted;

#if UNITY_EDITOR
        [Button("Refresh")]
        public void Rebuild()
        {
            settings = FindAllAssets<SettingSOAbstract>().ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private static List<T> FindAllAssets<T>() where T : ScriptableObject
        {
            return UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
                .Select(UnityEditor.AssetDatabase.LoadAssetAtPath<T>)
                .Where(a => a != null)
                .ToList();
        }
#endif

        [Inject]
        private void Construct(ISaver<SavableData> saver)
        {
            this.saver = saver;
        }

        private void Awake()
        {
            foreach (SettingSOAbstract setting in settings)
            {
                if (setting == null)
                {
                    DebugUtility.LogError(this, "Unable to initialize setting, null exception!");
                    continue;
                }
                if (!savableComponents.TryAdd(setting.ID, setting))
                {
                    DebugUtility.LogError(this, $"Unable to initialize setting '{setting.SettingName}', ID is not unique!");
                    continue;
                }
            }
        }

        private void Start()
        {
            SavableData loaded = LoadSettings();

            if (loaded?.Data != null)
            {
                foreach (SettingEntry entry in loaded.Data)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.Id))
                    {
                        continue;
                    }
                    if (!savableComponents.TryGetValue(entry.Id, out SettingSOAbstract settingComponent))
                    {
                        continue;
                    }
                    if (settingComponent == null)
                    {
                        DebugUtility.LogError(this, "Setting component is null!");
                        continue;
                    }
                    settingComponent.Load(entry);
                }
            }

            hasLoaded = true;
            foreach (SettingSOAbstract setting in savableComponents.Values)
            {
                setting.OnSettingUpdated -= MarkDirty;
                setting.OnSettingUpdated += MarkDirty;
                setting.CompleteLoad();
            }
        }

        private SavableData LoadSettings()
        {
            if (saver == null)
            {
                DebugUtility.LogError(this, "No ISaver<SavableData> is bound; settings use their defaults and will not be saved.");
                LoadStatus = LoadStatus.Missing;
                return null;
            }

            if (saver is IRecoverableSaver<SavableData> recoverable)
            {
                recoverable.TryLoad(out SavableData data, out LoadStatus status);
                LoadStatus = status;
                if (status == LoadStatus.Unreadable)
                {
                    DebugUtility.LogError(this,
                        "Saved settings exist but could not be read. Defaults are in use; the unreadable save is only replaced once a setting is changed.");
                }
                return data;
            }

            SavableData legacy = saver.Load();
            LoadStatus = legacy != null ? LoadStatus.LoadedPrimary : LoadStatus.Missing;
            return legacy;
        }

        private void OnEnable()
        {
            Application.quitting += Flush;
        }

        private void OnDisable()
        {
            Application.quitting -= Flush;
            Flush();
        }

        private void Update()
        {
            if (dirty && Time.unscaledTime >= saveAt)
            {
                Save();
            }
        }

        private void MarkDirty()
        {
            if (!hasLoaded)
            {
                return;
            }

            dirty = true;
            saveAt = Time.unscaledTime + saveDelay;
        }

        /// <summary>Writes pending changes now, if there are any.</summary>
        public void Flush()
        {
            if (dirty)
            {
                Save();
            }
        }

        /// <summary>Writes every setting now, whether or not it changed.</summary>
        public void Save()
        {
            if (!hasLoaded)
            {
                DebugUtility.LogWarning(this, "Save skipped: settings were not loaded yet, refusing to overwrite the save file with defaults.");
                return;
            }

            if (saver == null)
            {
                return;
            }

            SavableData data = new SavableData
            {
                Data = savableComponents.Values.Select(c => c.Save()).Where(entry => entry != null).ToList()
            };
            saver.Save(data);
            dirty = false;
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                Flush();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Flush();
            }
        }

        private void OnDestroy()
        {
            foreach (SettingSOAbstract setting in savableComponents.Values)
            {
                if (setting != null)
                {
                    setting.OnSettingUpdated -= MarkDirty;
                }
            }

            Flush();
        }
    }
}
