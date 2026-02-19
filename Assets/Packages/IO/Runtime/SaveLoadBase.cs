using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyToolz.EditorToolz;
using MyToolz.Utilities.Debug;
using Newtonsoft.Json;
using UnityEngine;
using Zenject;

namespace MyToolz.IO
{
    public interface ISaver<T>
    {
        void Save();
        void Save(T obj);
        T Load();
    }

    public abstract class SaveLoadBase<T> : MonoInstaller, ISaver<T> where T : class, new()
    {
        public enum SaveRoot
        {
            PersistentDataPath,
            DataPath,
            StreamingAssetsPath,
            TemporaryCachePath
        }


        [FoldoutGroup("Save Settings"), SerializeField, Tooltip("Where to create/read the save folder.")]
        [OnValueChanged(nameof(RebuildPaths))]
        private SaveRoot _root = SaveRoot.PersistentDataPath;

        [FoldoutGroup("Save Settings"), SerializeField, Tooltip("Subfolder inside the chosen root. Will be created if missing.")]
        [OnValueChanged(nameof(RebuildPaths))]
        private string _filePath = "Saves";

        [FoldoutGroup("Save Settings"), SerializeField, Tooltip("File name without or with .json extension. Invalid characters are removed.")]
        [OnValueChanged(nameof(RebuildPaths))]
        private string _fileName = "save.json";

        [FoldoutGroup("Save Settings"), SerializeField]
        private bool _useCache = false;
        private string _resolvedFolder => _resolvedFolderCache;

        private string _fullPathPreview => _fullPath;

        //[GUIColor(nameof(FileExistsColor))]
        private bool _fileExistsInspector => FileExists();

        protected T cache;

        //[FoldoutGroup("Actions")]
        //[Button(ButtonSizes.Medium)]
        [Button]
        private void OpenFolder()
        {
#if UNITY_EDITOR
            EnsureFolderExists();
            UnityEditor.EditorUtility.RevealInFinder(_resolvedFolderCache);
#else
            DebugUtility.LogWarning("OpenFolder is only available in the Editor.");
#endif
        }

        //[FoldoutGroup("Actions")]
        //[Button(ButtonSizes.Medium)]
        [Button]
        private void RevealFile()
        {
#if UNITY_EDITOR
            EnsureFolderExists();
            if (!File.Exists(_fullPath))
                File.WriteAllText(_fullPath, "{}");
            UnityEditor.EditorUtility.RevealInFinder(_fullPath);
#else
            DebugUtility.LogWarning("RevealFile is only available in the Editor.");
#endif
        }

        //[FoldoutGroup("Actions")]
        //[Button(ButtonSizes.Medium)]
        [Button]
        private void CopyFullPathToClipboard()
        {
#if UNITY_EDITOR
            UnityEditor.EditorGUIUtility.systemCopyBuffer = _fullPath;
            DebugUtility.Log(this, $"Copied path:\n{_fullPath}");
#else
            DebugUtility.LogWarning(this, "Copy path is only available in the Editor.");
#endif
        }

        //[FoldoutGroup("Actions")]
        //[Button(ButtonSizes.Medium), GUIColor(0.9f, 0.3f, 0.3f)]
        [Button]
        private void DeleteFile()
        {
            if (File.Exists(_fullPath))
            {
                File.Delete(_fullPath);
                DebugUtility.LogWarning(this, $"Deleted file: {_fullPath}");
            }
            else
            {
                DebugUtility.LogWarning(this, "No file to delete.");
            }
        }

        protected string _fullPath;
        private string _resolvedFolderCache;

        public override void InstallBindings()
        {
            Container.Bind<ISaver<T>>().FromInstance(this).AsSingle();
        }

        protected virtual void Awake()
        {
            RebuildPaths();
            EnsureFolderExists();
            DebugUtility.Log(this, $"[SaveLoadBase] Awake {GetType().Name} instanceId={GetInstanceID()} scene={gameObject.scene.name} useCache={_useCache}");
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            RebuildPaths();
        }
#endif

        private void RebuildPaths()
        {
            _fileName = SanitizeFileName(_fileName);
            if (!Path.GetExtension(_fileName).Equals(".json", StringComparison.OrdinalIgnoreCase))
                _fileName = Path.ChangeExtension(_fileName, ".json");

            string rootPath = _root switch
            {
                SaveRoot.PersistentDataPath => Application.persistentDataPath,
                SaveRoot.DataPath => Application.dataPath,
                SaveRoot.StreamingAssetsPath => Application.streamingAssetsPath,
                SaveRoot.TemporaryCachePath => Application.temporaryCachePath,
                _ => Application.persistentDataPath
            };

            _resolvedFolderCache = string.IsNullOrWhiteSpace(_filePath)
                ? rootPath
                : Path.Combine(rootPath, _filePath);

            _fullPath = Path.Combine(_resolvedFolderCache, _fileName);
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "save.json";

            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(name.Where(c => !invalid.Contains(c)).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "save.json" : cleaned;
        }

        private void EnsureFolderExists()
        {
            if (!Directory.Exists(_resolvedFolderCache))
                Directory.CreateDirectory(_resolvedFolderCache);
        }

        private Color FileExistsColor() => FileExists() ? new Color(0.5f, 0.9f, 0.5f) : new Color(0.9f, 0.6f, 0.4f);

        protected void SaveToFile(T data)
        {
            EnsureFolderExists();
            DebugUtility.Log(this, "Saved!");
            string jsonData = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(_fullPath, jsonData);
        }

        protected T LoadFromFile()
        {
            if (!File.Exists(_fullPath)) return null;
            DebugUtility.Log(this, "Loaded!");
            string jsonData = File.ReadAllText(_fullPath);
            return JsonConvert.DeserializeObject<T>(jsonData);
        }

        protected async Task SaveToFileAsync(T data)
        {
            EnsureFolderExists();
            DebugUtility.Log(this, "Saved!");
            string jsonData = JsonConvert.SerializeObject(data, Formatting.Indented);
            await File.WriteAllTextAsync(_fullPath, jsonData);
        }

        protected bool FileExists() => File.Exists(_fullPath);

        public void Save(T obj)
        {
            if (obj == null)
            {
                DebugUtility.LogWarning(this, "Save called with null object.");
                return;
            }
            SaveToFile(obj);
        }

        public T Load()
        {
            if (_useCache)
            {
                if (cache == null)
                    cache = LoadFromFile() ?? new T();

                return cache;
            }

            return LoadFromFile() ?? new T();
        }

        public virtual void Save()
        {
            if (_useCache)
            {
                if (cache == null) cache = new();
                Save(cache);
            }
            else
            {
                Save(new());
            }
        }

        protected virtual void OnEnable()
        {
            Application.quitting += Save;
        }

        protected virtual void OnDisable()
        {
            Application.quitting -= Save;
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) Save();
        }

        private void OnDestroy()
        {
            Save();
        }
    }
}
