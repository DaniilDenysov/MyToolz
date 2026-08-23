using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyToolz.Utilities.Debug;
using MyToolz.EditorToolz;
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

        [FoldoutGroup("Persistance Settings"), SerializeField, Tooltip("Where to create/read the save folder.")]
        [OnValueChanged(nameof(RebuildPaths))]
        private SaveRoot root = SaveRoot.PersistentDataPath;

        [FoldoutGroup("Persistance Settings"), SerializeField, Tooltip("Subfolder inside the chosen root. Will be created if missing.")]
        [OnValueChanged(nameof(RebuildPaths))]
        private string filePath = "Saves";

        [FoldoutGroup("Persistance Settings"), SerializeField, Tooltip("File name without extension. Invalid characters are removed. Extension is provided by the chosen strategy.")]
        [OnValueChanged(nameof(RebuildPaths))]
        private string fileName = "save";

        [FoldoutGroup("Persistance Settings"), SerializeField]
        private bool useCache = false;

        [FoldoutGroup("Persistance Settings"), SerializeField, SubclassSelector]
        private SerializationStrategy<T> serializationStrategy = new NewtonsoftJsonStrategy<T>();

        [FoldoutGroup("Persistance Settings"), SerializeField, SubclassSelector]
        private EncryptionStrategy encryptionStrategy = new NoEncryptionStrategy();

        [FoldoutGroup("Persistance Settings"), SerializeReference, SubclassSelector, Tooltip("Where the raw save bytes are stored. FileStorage (default) writes files; use PlayerPrefs or PlatformStorage for WebGL-safe saving.")]
        private StorageStrategy storageStrategy = new FileStorageStrategy();

        private string resolvedFolder => resolvedFolderCache;
        private string fullPathPreview => fullPath;
        private bool fileExistsInspector => FileExists();

        protected T cache;

        protected string fullPath;
        private string tempPath;
        private string backupPath;
        private string resolvedFolderCache;
        private string key;

        private bool _hasSavedThisSession;

        [Button]
        private void OpenFolder()
        {
#if UNITY_EDITOR
            EnsureFolderExists();
            UnityEditor.EditorUtility.RevealInFinder(resolvedFolderCache);
#else
            DebugUtility.LogWarning(this, "OpenFolder is only available in the Editor.");
#endif
        }

        [Button]
        private void RevealFile()
        {
#if UNITY_EDITOR
            EnsureFolderExists();
            if (!File.Exists(fullPath))
            {
                File.WriteAllText(fullPath, "{}");
            }

            UnityEditor.EditorUtility.RevealInFinder(fullPath);
#else
            DebugUtility.LogWarning(this, "RevealFile is only available in the Editor.");
#endif
        }

        [Button]
        private void CopyFullPathToClipboard()
        {
#if UNITY_EDITOR
            UnityEditor.EditorGUIUtility.systemCopyBuffer = fullPath;
            DebugUtility.Log(this, $"Copied path:\n{fullPath}");
#else
            DebugUtility.LogWarning(this, "Copy path is only available in the Editor.");
#endif
        }

        [Button]
        private void DeleteFile()
        {
            EnsureStorageAssigned();
            StorageLocation location = BuildLocation();

            if (storageStrategy.Exists(location))
            {
                storageStrategy.Delete(location);
                DebugUtility.LogWarning(this, $"Deleted save: {key}");
            }
            else
            {
                DebugUtility.LogWarning(this, "No save to delete.");
            }
        }

        public override void InstallBindings()
        {
            Container.Bind<ISaver<T>>().FromInstance(this).AsSingle();
        }

        protected virtual void Awake()
        {
            EnsureStrategyAssigned();
            EnsureEncryptionStrategyAssigned();
            EnsureStorageAssigned();
            RebuildPaths();
            DebugUtility.Log(this, $"[SaveLoadBase] Awake {GetType().Name} instanceId={GetInstanceID()} scene={gameObject.scene.name} useCache={useCache} strategy={serializationStrategy.GetType().Name} encryption={encryptionStrategy.GetType().Name} storage={storageStrategy.GetType().Name}");
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            RebuildPaths();
        }
#endif

        private void EnsureStrategyAssigned()
        {
            if (serializationStrategy != null)
            {
                return;
            }

            serializationStrategy = new NewtonsoftJsonStrategy<T>();
            DebugUtility.LogWarning(this, "No serialization strategy assigned in the inspector. Defaulting to NewtonsoftJsonStrategy.");
        }

        private void EnsureEncryptionStrategyAssigned()
        {
            if (encryptionStrategy != null)
            {
                return;
            }

            encryptionStrategy = new NoEncryptionStrategy();
            DebugUtility.LogWarning(this, "No encryption strategy assigned in the inspector. Defaulting to NoEncryptionStrategy.");
        }

        private void EnsureStorageAssigned()
        {
            if (storageStrategy != null)
            {
                return;
            }

            storageStrategy = new FileStorageStrategy();
            DebugUtility.LogWarning(this, "No storage strategy assigned in the inspector. Defaulting to FileStorageStrategy.");
        }

        private void RebuildPaths()
        {
            string sanitized = SanitizeFileName(fileName);
            string extension = serializationStrategy?.FileExtension ?? ".json";

            string rootPath = root switch
            {
                SaveRoot.PersistentDataPath => Application.persistentDataPath,
                SaveRoot.DataPath => Application.dataPath,
                SaveRoot.StreamingAssetsPath => Application.streamingAssetsPath,
                SaveRoot.TemporaryCachePath => Application.temporaryCachePath,
                _ => Application.persistentDataPath
            };

            resolvedFolderCache = string.IsNullOrWhiteSpace(filePath)
                ? rootPath
                : Path.Combine(rootPath, filePath);

            fullPath = Path.Combine(resolvedFolderCache, sanitized + extension);
            tempPath = fullPath + ".tmp";
            backupPath = fullPath + ".bak";

            // Backend-agnostic id for key/value stores (PlayerPrefs, browser storage).
            key = string.IsNullOrWhiteSpace(filePath)
                ? sanitized + extension
                : $"{filePath.Replace('\\', '/').TrimEnd('/')}/{sanitized}{extension}";
        }

        private StorageLocation BuildLocation() =>
            new StorageLocation(fullPath, tempPath, backupPath, resolvedFolderCache, key);

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "save";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            string cleaned = new string(name.Where(c => !invalid.Contains(c)).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "save" : cleaned;
        }

        private void EnsureFolderExists()
        {
            if (!Directory.Exists(resolvedFolderCache))
            {
                Directory.CreateDirectory(resolvedFolderCache);
            }
        }

        private Color FileExistsColor() => FileExists() ? new Color(0.5f, 0.9f, 0.5f) : new Color(0.9f, 0.6f, 0.4f);

        protected void SaveToFile(T data)
        {
            string raw = serializationStrategy.Serialize(data);
            string processed = encryptionStrategy.Encrypt(raw);

            storageStrategy.Write(BuildLocation(), processed);

            DebugUtility.Log(this, "Saved!");
        }

        protected T LoadFromFile()
        {
            StorageLocation location = BuildLocation();

            if (TryLoadSlot(location, primary: true, out T primary))
            {
                return primary;
            }

            if (TryLoadSlot(location, primary: false, out T backup))
            {
                DebugUtility.Log(this, "Restored save from backup.");
                return backup;
            }

            return null;
        }

        private bool TryLoadSlot(in StorageLocation location, bool primary, out T result)
        {
            result = null;

            try
            {
                string raw;
                bool found = primary
                    ? storageStrategy.TryReadPrimary(location, out raw)
                    : storageStrategy.TryReadBackup(location, out raw);

                if (!found)
                {
                    return false;
                }

                string decrypted = encryptionStrategy.Decrypt(raw);
                result = serializationStrategy.Deserialize(decrypted);

                if (result != null)
                {
                    DebugUtility.Log(this, primary ? "Loaded!" : "Loaded from backup.");
                    return true;
                }
            }
            catch (Exception e)
            {
                DebugUtility.LogError(this, $"Failed to load {(primary ? "save" : "backup")} data. Reason: {e.Message}");
            }

            return false;
        }

        protected async Task SaveToFileAsync(T data)
        {
            string raw = serializationStrategy.Serialize(data);
            string processed = encryptionStrategy.Encrypt(raw);

            await storageStrategy.WriteAsync(BuildLocation(), processed);

            DebugUtility.Log(this, "Saved!");
        }

        protected bool FileExists() => storageStrategy != null && storageStrategy.Exists(BuildLocation());

        public void Save(T obj)
        {
            if (obj == null)
            {
                DebugUtility.LogWarning(this, "Persistance called with null object.");
                return;
            }

            SaveToFile(obj);
            _hasSavedThisSession = true;
        }

        public T Load()
        {
            if (useCache)
            {
                if (cache == null)
                {
                    cache = LoadFromFile() ?? new T();
                }

                return cache;
            }

            return LoadFromFile() ?? new T();
        }

        public virtual void Save()
        {
            if (useCache && cache != null)
            {
                Save(cache);
                return;
            }

            DebugUtility.LogWarning(this, "Parameterless Save was called with no cached data; skipping to avoid overwriting the existing file with empty data.");
        }

        protected virtual void OnEnable()
        {
            Application.quitting += Save;
        }

        protected virtual void OnDisable()
        {
            Application.quitting -= Save;
        }

        protected virtual void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                _hasSavedThisSession = false;
                Save();
            }
        }

        protected virtual void OnDestroy()
        {
            if (!_hasSavedThisSession)
            {
                Save();
            }
        }
    }
}
