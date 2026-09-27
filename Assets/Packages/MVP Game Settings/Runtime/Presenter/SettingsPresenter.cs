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

        [Tooltip("Setting changes are batched: the file is written this many seconds (unscaled) after the last change. Pausing, quitting and destroying the presenter write pending changes immediately.")]
        [SerializeField, Min(0f)] private float saveDelaySeconds = 1f;

        [Tooltip("Upper bound on how long a stream of changes can postpone the write.")]
        [SerializeField, Min(0f)] private float maxSaveDelaySeconds = 5f;

        private readonly Dictionary<string, SettingSOAbstract> savableComponents = new();
        private ISaver<SavableData> saver;
        private bool hasSavedThisSession;
        private bool hasLoaded;
        private bool dirty;
        private float firstChangeAt;
        private float lastChangeAt;

        /// <summary>True while a setting change is waiting to be written.</summary>
        public bool HasPendingChanges => dirty;

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
            SavableData loaded = saver.Load();

            if (loaded?.Data != null)
            {
                foreach (SettingEntry entry in loaded.Data)
                {
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
                setting.OnSettingUpdated += MarkDirty;
                setting.CompleteLoad();
            }
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
            if (!dirty)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now - lastChangeAt >= saveDelaySeconds || now - firstChangeAt >= Mathf.Max(saveDelaySeconds, maxSaveDelaySeconds))
            {
                Save();
            }
        }

        /// <summary>
        /// Records that a setting changed. Changes (including the mirrored updates of a setting's
        /// twin copies) are coalesced into one write instead of one full save per change.
        /// </summary>
        private void MarkDirty()
        {
            if (!hasLoaded)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (!dirty)
            {
                dirty = true;
                firstChangeAt = now;
            }

            lastChangeAt = now;
        }

        /// <summary>Writes pending changes now, if there are any.</summary>
        public void Flush()
        {
            if (dirty)
            {
                Save();
            }
        }

        /// <summary>Writes every setting immediately, whether or not anything changed.</summary>
        public void Save()
        {
            if (!hasLoaded)
            {
                DebugUtility.LogWarning(this, "Save skipped: settings were not loaded yet, refusing to overwrite the save file with defaults.");
                return;
            }

            SavableData data = new SavableData
            {
                Data = savableComponents.Values.Select(c => c.Save()).Where(entry => entry != null).ToList()
            };
            saver.Save(data);
            dirty = false;
            hasSavedThisSession = true;
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
            // WebGL and some Android launchers deliver focus loss without a pause.
            if (!hasFocus)
            {
                Flush();
            }
        }

        private void OnDestroy()
        {
            foreach (SettingSOAbstract setting in savableComponents.Values)
            {
                setting.OnSettingUpdated -= MarkDirty;
            }

            if (dirty || !hasSavedThisSession)
            {
                Save();
            }
        }
    }
}
