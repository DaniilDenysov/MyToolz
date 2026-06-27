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
        private readonly Dictionary<string, SettingSOAbstract> savableComponents = new();
        private ISaver<SavableData> saver;
        private bool hasSavedThisSession;
        private bool hasLoaded;

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
            hasLoaded = true;

            if (loaded?.Data == null || loaded.Data.Count == 0)
            {
                return;
            }

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

        private void OnEnable()
        {
            Application.quitting += Save;
        }

        private void OnDisable()
        {
            Application.quitting -= Save;
        }

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
            hasSavedThisSession = true;
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                hasSavedThisSession = false;
                Save();
            }
        }

        private void OnDestroy()
        {
            if (!hasSavedThisSession)
            {
                Save();
            }
        }
    }
}
