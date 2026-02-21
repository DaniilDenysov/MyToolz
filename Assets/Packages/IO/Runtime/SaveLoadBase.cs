using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyToolz.EditorToolz;
using MyToolz.Utilities.Debug;
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
        private SaveRoot root = SaveRoot.PersistentDataPath;

        [FoldoutGroup("Save Settings"), SerializeField, Tooltip("Subfolder inside the chosen root. Will be created if missing.")]
        [OnValueChanged(nameof(RebuildPaths))]
        private string filePath = "Saves";

        [FoldoutGroup("Save Settings"), SerializeField, Tooltip("File name without extension. Invalid characters are removed. Extension is provided by the chosen strategy.")]
        [OnValueChanged(nameof(RebuildPaths))]
        private string fileName = "save";

        [FoldoutGroup("Save Settings"), SerializeField]
        private bool useCache = false;

        [FoldoutGroup("Save Settings"), SerializeField, SubclassSelector]
        private SerializationStrategy<T> serializationStrategy = new NewtonsoftJsonStrategy<T>(); 

        private string resolvedFolder => resolvedFolderCache;
        private string fullPathPreview => fullPath;
        private bool fileExistsInspector => FileExists();

        protected T cache;

        protected string fullPath;
        private string tempPath;
        private string backupPath;
        private string resolvedFolderCache;

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
                File.WriteAllText(fullPath, "{}");
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
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                DebugUtility.LogWarning(this, $"Deleted file: {fullPath}");
            }
            else
            {
                DebugUtility.LogWarning(this, "No file to delete.");
            }
        }

        public override void InstallBindings()
        {
            Container.Bind<ISaver<T>>().FromInstance(this).AsSingle();
        }

        protected virtual void Awake()
        {
            EnsureStrategyAssigned();
            RebuildPaths();
            EnsureFolderExists();
            DebugUtility.Log(this, $"[SaveLoadBase] Awake {GetType().Name} instanceId={GetInstanceID()} scene={gameObject.scene.name} useCache={useCache} strategy={serializationStrategy.GetType().Name}");
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            RebuildPaths();
        }
#endif

        private void EnsureStrategyAssigned()
        {
            if (serializationStrategy != null) return;

            serializationStrategy = new NewtonsoftJsonStrategy<T>();
            DebugUtility.LogWarning(this, "No serialization strategy assigned in the inspector. Defaulting to NewtonsoftJsonStrategy.");
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
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "save";

            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(name.Where(c => !invalid.Contains(c)).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "save" : cleaned;
        }

        private void EnsureFolderExists()
        {
            if (!Directory.Exists(resolvedFolderCache))
                Directory.CreateDirectory(resolvedFolderCache);
        }

        private Color FileExistsColor() => FileExists() ? new Color(0.5f, 0.9f, 0.5f) : new Color(0.9f, 0.6f, 0.4f);

        protected void SaveToFile(T data)
        {
            EnsureFolderExists();

            string raw = serializationStrategy.Serialize(data);

            File.WriteAllText(tempPath, raw);

            if (File.Exists(fullPath))
                File.Copy(fullPath, backupPath, overwrite: true);

            File.Move(tempPath, fullPath);

            DebugUtility.Log(this, "Saved!");
        }

        protected T LoadFromFile()
        {
            if (File.Exists(fullPath))
            {
                try
                {
                    string raw = File.ReadAllText(fullPath);
                    T result = serializationStrategy.Deserialize(raw);

                    if (result != null)
                    {
                        DebugUtility.Log(this, "Loaded!");
                        return result;
                    }
                }
                catch (Exception e)
                {
                    DebugUtility.LogError(this, $"Failed to load save file, attempting backup. Reason: {e.Message}");
                }
            }

            if (File.Exists(backupPath))
            {
                try
                {
                    DebugUtility.LogWarning(this, "Loading from backup file.");
                    string raw = File.ReadAllText(backupPath);
                    T result = serializationStrategy.Deserialize(raw);

                    if (result != null)
                    {
                        File.Copy(backupPath, fullPath, overwrite: true);
                        DebugUtility.Log(this, "Restored save from backup.");
                        return result;
                    }
                }
                catch (Exception e)
                {
                    DebugUtility.LogError(this, $"Backup file also failed to load. Reason: {e.Message}");
                }
            }

            return null;
        }

        protected async Task SaveToFileAsync(T data)
        {
            EnsureFolderExists();

            string raw = serializationStrategy.Serialize(data);

            await File.WriteAllTextAsync(tempPath, raw);

            if (File.Exists(fullPath))
                File.Copy(fullPath, backupPath, overwrite: true);

            File.Move(tempPath, fullPath);

            DebugUtility.Log(this, "Saved!");
        }

        protected bool FileExists() => File.Exists(fullPath);

        public void Save(T obj)
        {
            if (obj == null)
            {
                DebugUtility.LogWarning(this, "Save called with null object.");
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
                    cache = LoadFromFile() ?? new T();

                return cache;
            }

            return LoadFromFile() ?? new T();
        }

        public virtual void Save()
        {
            if (useCache)
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
            if (pause)
            {
                _hasSavedThisSession = false;
                Save();
            }
        }

        private void OnDestroy()
        {
            if (!_hasSavedThisSession)
                Save();
        }
    }
}
