using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
#if !UNITY_WEBGL || UNITY_EDITOR
using System.Threading;
#endif
using System.Threading.Tasks;
using MyToolz.Utilities.Debug;
using MyToolz.EditorToolz;
using UnityEngine;
using Zenject;

namespace MyToolz.IO
{
    /// <summary>Outcome of the most recent storage load attempt.</summary>
    public enum LoadStatus
    {
        NotAttempted,
        LoadedPrimary,
        RecoveredPending,
        RecoveredBackup,
        Missing,
        Unreadable
    }

    public static class LoadStatusExtensions
    {
        /// <summary>True when stored data was decoded (primary, pending temp, or backup).</summary>
        public static bool IsLoaded(this LoadStatus status) =>
            status == LoadStatus.LoadedPrimary ||
            status == LoadStatus.RecoveredPending ||
            status == LoadStatus.RecoveredBackup;
    }

    public interface ISaver<T>
    {
        void Save();
        void Save(T obj);
        T Load();
    }

    public interface IAsyncSaver<T>
    {
        Task<bool> SaveAsync(T obj);
    }

    /// <summary>
    /// Saver that can tell callers why a load produced the data it did. Use it to tell a
    /// first run (<see cref="LoadStatus.Missing"/>) apart from a save that exists but could
    /// not be decoded (<see cref="LoadStatus.Unreadable"/>).
    /// </summary>
    public interface IRecoverableSaver<T> : ISaver<T>
    {
        LoadStatus LastLoadStatus { get; }

        /// <summary>Returns true only when stored data was decoded. Missing and unreadable both return false.</summary>
        bool TryLoad(out T data, out LoadStatus status);
    }

    public abstract class SaveLoadBase<T> : MonoInstaller, IRecoverableSaver<T>, IAsyncSaver<T> where T : class, new()
    {
        public enum SaveRoot
        {
            PersistentDataPath,
            DataPath,
            StreamingAssetsPath,
            TemporaryCachePath
        }

        private enum SlotLoadOutcome
        {
            Missing,
            Loaded,
            Unreadable
        }

        private enum LoadSlot
        {
            Primary,
            Pending,
            Backup
        }

        [FoldoutGroup("Persistance Settings"), SerializeField, Tooltip("Where to create/read the save folder.")]
        [OnValueChanged(nameof(RebuildPaths))]
        private SaveRoot root = SaveRoot.PersistentDataPath;

        [FoldoutGroup("Persistance Settings"), SerializeField, Tooltip("Relative subfolder inside the chosen root. Rooted paths and '..' traversal are rejected.")]
        [OnValueChanged(nameof(RebuildPaths))]
        private string filePath = "Saves";

        [FoldoutGroup("Persistance Settings"), SerializeField, Tooltip("File name without extension. Invalid characters are removed. Extension is provided by the chosen strategy.")]
        [OnValueChanged(nameof(RebuildPaths))]
        private string fileName = "save";

        [FoldoutGroup("Persistance Settings"), SerializeField]
        private bool useCache = false;

        // SerializeReference (not SerializeField): the field type is abstract, so only a managed
        // reference can persist the concrete strategy chosen in the inspector.
        [FoldoutGroup("Persistance Settings"), SerializeReference, SubclassSelector]
        private SerializationStrategy<T> serializationStrategy = new NewtonsoftJsonStrategy<T>();

        [FoldoutGroup("Persistance Settings"), SerializeReference, SubclassSelector]
        private EncryptionStrategy encryptionStrategy = new NoEncryptionStrategy();

        [FoldoutGroup("Persistance Settings"), SerializeReference, SubclassSelector, Tooltip("Where the raw save bytes are stored. PlatformStorage routes per platform.")]
        private StorageStrategy storageStrategy = new PlatformStorageStrategy();

        private string resolvedFolder => resolvedFolderCache;
        private string fullPathPreview => fullPath;
        private bool fileExistsInspector => FileExists();

        protected T cache;

        protected string fullPath;
        private string tempPath;
        private string backupPath;
        private string resolvedFolderCache;
        private string key;

#if !UNITY_WEBGL || UNITY_EDITOR
        private readonly SemaphoreSlim mutationGate = new SemaphoreSlim(1, 1);
#endif
        private const string IntegrityEnvelopePrefix = "MTIO1:";
        private const int Sha256HexLength = 64;

        private bool isQuitting;
        private bool initialized;
        private bool blockAutomaticSaveAfterUnreadableLoad;
        private Task<bool> lastSaveTask = Task.FromResult(true);
        private Task lastRecoveryTask = Task.CompletedTask;

        // Hash of the serialized state last known to be on storage. Automatic (parameterless)
        // saves skip the write when the cache still serializes to the same content.
        private string lastPersistedStateHash;

        public LoadStatus LastLoadStatus { get; private set; } = LoadStatus.NotAttempted;

        /// <summary>Most recently started save task. Useful for diagnostics/explicit shutdown flows.</summary>
        public Task<bool> LastSaveTask => lastSaveTask;

        /// <summary>
        /// Pending backup/temp-to-primary repair started by the most recent recovered load.
        /// The task faults when persistence of the repair fails, so callers can observe
        /// the difference between loading recovered data and successfully repairing disk.
        /// </summary>
        public Task LastRecoveryTask => lastRecoveryTask;

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
            UnityEditor.EditorUtility.RevealInFinder(File.Exists(fullPath) ? fullPath : resolvedFolderCache);
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
            EnsureInitialized();
            StorageLocation location = BuildLocation();

            if (!storageStrategy.AnyDataExists(location))
            {
                DebugUtility.LogWarning(this, "No save, recovery, or diagnostic data to delete.");
                return;
            }

            if (storageStrategy.RequiresAsyncPersistence)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                _ = DeleteFileAsync(location, acquireMutationGate: false);
#else
                _ = DeleteFileAsync(location, acquireMutationGate: true);
#endif
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // A custom synchronous WebGL backend cannot use SemaphoreSlim because it is
            // intentionally excluded from WebGL player builds.
            DeleteFileSynchronous(location);
#else
            if (mutationGate.Wait(0))
            {
                try
                {
                    DeleteFileSynchronous(location);
                }
                finally
                {
                    mutationGate.Release();
                }
            }
            else
            {
                _ = DeleteFileAsync(location, acquireMutationGate: true);
                DebugUtility.LogWarning(this,
                    "Delete queued behind an in-progress save/recovery operation so the same storage slot is not mutated concurrently.");
            }
#endif
        }

        private void DeleteFileSynchronous(StorageLocation location)
        {
            try
            {
                storageStrategy.Delete(location);
                ResetStateAfterDelete();
                DebugUtility.LogWarning(this, $"Deleted save: {key} using {storageStrategy.ActiveStrategyName}");
            }
            catch (Exception e)
            {
                DebugUtility.LogError(this, $"Failed to delete save '{key}' using {storageStrategy.ActiveStrategyName}.\n{e}");
            }
        }

        private async Task DeleteFileAsync(StorageLocation location, bool acquireMutationGate)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (acquireMutationGate)
            {
                await mutationGate.WaitAsync();
            }
#endif
            try
            {
                await storageStrategy.DeleteAsync(location);
                ResetStateAfterDelete();
                DebugUtility.LogWarning(this, $"Deleted save: {key} using {storageStrategy.ActiveStrategyName}");
            }
            catch (Exception e)
            {
                DebugUtility.LogError(this, $"Failed to delete save '{key}' using {storageStrategy.ActiveStrategyName}.\n{e}");
            }
            finally
            {
#if !UNITY_WEBGL || UNITY_EDITOR
                if (acquireMutationGate)
                {
                    mutationGate.Release();
                }
#endif
            }
        }

        private void ResetStateAfterDelete()
        {
            cache = null;
            LastLoadStatus = LoadStatus.Missing;
            blockAutomaticSaveAfterUnreadableLoad = false;
            lastRecoveryTask = Task.CompletedTask;
            lastPersistedStateHash = null;
        }

        public override void InstallBindings()
        {
            EnsureInitialized();
            Container.Bind<ISaver<T>>().FromInstance(this).AsSingle();
            Container.Bind<IRecoverableSaver<T>>().FromInstance(this).AsSingle();
            Container.Bind<IAsyncSaver<T>>().FromInstance(this).AsSingle();
        }

        protected virtual void Awake()
        {
            EnsureInitialized();

            if (ResolveWritableRoot(root, Application.platform, Application.isEditor) != root)
            {
                string fallbackName = Application.platform == RuntimePlatform.WebGLPlayer
                    ? $"stable IDBFS directory '{GetWebGlStorageRoot(Application.productName)}'"
                    : "PersistentDataPath";
                DebugUtility.LogWarning(this,
                    $"[SaveLoadBase] Root '{root}' cannot provide durable saves on {Application.platform}. Using {fallbackName}.");
            }

            DebugUtility.Log(this,
                $"[SaveLoadBase] Awake {GetType().Name} instanceId={GetInstanceID()} scene={gameObject.scene.name} " +
                $"useCache={useCache} strategy={serializationStrategy.GetType().Name} encryption={encryptionStrategy.GetType().Name} " +
                $"storage={storageStrategy.GetType().Name} activeStorage={storageStrategy.ActiveStrategyName}");
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

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            EnsureStrategyAssigned();
            EnsureEncryptionStrategyAssigned();
            EnsureStorageAssigned();
            RebuildPaths();
            initialized = true;
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

            storageStrategy = new PlatformStorageStrategy();
            DebugUtility.LogWarning(this, "No storage strategy assigned in the inspector. Defaulting to PlatformStorageStrategy.");
        }

        private void RebuildPaths()
        {
            string sanitizedName = SanitizeFileName(fileName);
            string extension = serializationStrategy?.FileExtension ?? ".json";
            string sanitizedRelativePath = SanitizeRelativePath(filePath);

            SaveRoot writableRoot = ResolveWritableRoot(root, Application.platform, Application.isEditor);
            string rootPath = ResolveRootPath(
                writableRoot,
                Application.platform,
                Application.isEditor,
                Application.productName);

            resolvedFolderCache = ResolveInsideRoot(rootPath, sanitizedRelativePath);
            fullPath = Path.Combine(resolvedFolderCache, sanitizedName + extension);
            tempPath = fullPath + ".tmp";
            backupPath = fullPath + ".bak";

            // Backend-agnostic id for key/value stores (PlayerPrefs, browser storage).
            key = string.IsNullOrWhiteSpace(sanitizedRelativePath)
                ? sanitizedName + extension
                : $"{sanitizedRelativePath.Replace('\\', '/').TrimEnd('/')}/{sanitizedName}{extension}";
        }

        private static string ResolveRootPath(
            SaveRoot writableRoot,
            RuntimePlatform platform,
            bool isEditor,
            string productName)
        {
            if (!isEditor && platform == RuntimePlatform.WebGLPlayer)
            {
                return GetWebGlStorageRoot(productName);
            }

            return writableRoot switch
            {
                SaveRoot.PersistentDataPath => Application.persistentDataPath,
                SaveRoot.DataPath => Application.dataPath,
                SaveRoot.StreamingAssetsPath => Application.streamingAssetsPath,
                SaveRoot.TemporaryCachePath => Application.temporaryCachePath,
                _ => Application.persistentDataPath
            };
        }

        private static string GetWebGlStorageRoot(string productName)
        {
            string directoryName = SanitizeFileName(productName);
            return "/idbfs/" + directoryName;
        }

        private static SaveRoot ResolveWritableRoot(SaveRoot requestedRoot, RuntimePlatform platform, bool isEditor)
        {
            if (isEditor)
            {
                return requestedRoot;
            }

            if (platform == RuntimePlatform.WebGLPlayer ||
                ((platform == RuntimePlatform.Android || platform == RuntimePlatform.IPhonePlayer) &&
                 (requestedRoot == SaveRoot.DataPath || requestedRoot == SaveRoot.StreamingAssetsPath)))
            {
                return SaveRoot.PersistentDataPath;
            }

            return requestedRoot;
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
            string cleaned = new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();

            if (string.IsNullOrWhiteSpace(cleaned) || cleaned == "." || cleaned == "..")
            {
                return "save";
            }

            return cleaned;
        }

        private string SanitizeRelativePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string normalized = path.Trim().Replace('\\', '/');
            bool looksRooted = normalized.StartsWith("/", StringComparison.Ordinal) ||
                               (normalized.Length >= 2 && char.IsLetter(normalized[0]) && normalized[1] == ':') ||
                               Path.IsPathRooted(normalized);

            string[] rawSegments = normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (looksRooted || rawSegments.Any(segment => segment == ".."))
            {
                DebugUtility.LogWarning(this,
                    $"[SaveLoadBase] filePath '{path}' is not a safe relative path. Falling back to the selected save root.");
                return string.Empty;
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            string[] cleanedSegments = rawSegments
                .Where(segment => segment != ".")
                .Select(segment => new string(segment.Where(c => !invalid.Contains(c)).ToArray()).Trim())
                .Where(segment => !string.IsNullOrWhiteSpace(segment) && segment != "." && segment != "..")
                .ToArray();

            return cleanedSegments.Length == 0
                ? string.Empty
                : Path.Combine(cleanedSegments);
        }

        private string ResolveInsideRoot(string rootPath, string relativePath)
        {
            try
            {
                string absoluteRoot = Path.GetFullPath(rootPath);
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    return absoluteRoot;
                }

                string candidate = Path.GetFullPath(Path.Combine(absoluteRoot, relativePath));
                string rootPrefix = absoluteRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                                    Path.DirectorySeparatorChar;

                if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    DebugUtility.LogWarning(this,
                        $"[SaveLoadBase] Resolved filePath escaped save root. Falling back to '{absoluteRoot}'.");
                    return absoluteRoot;
                }

                return candidate;
            }
            catch (Exception e)
            {
                DebugUtility.LogWarning(this,
                    $"[SaveLoadBase] Could not normalize filePath '{relativePath}'. Falling back to root. Reason: {e.Message}");
                return rootPath;
            }
        }

        private void EnsureFolderExists()
        {
            if (!Directory.Exists(resolvedFolderCache))
            {
                Directory.CreateDirectory(resolvedFolderCache);
            }
        }

        private Color FileExistsColor() => FileExists() ? new Color(0.5f, 0.9f, 0.5f) : new Color(0.9f, 0.6f, 0.4f);

        private bool TryPreparePayload(T data, out string processed, out string stateHash)
        {
            processed = null;
            stateHash = null;
            try
            {
                string raw = serializationStrategy.Serialize(data);
                stateHash = ComputeSha256Hex(raw);
                string encrypted = encryptionStrategy.Encrypt(raw);
                processed = AddIntegrityEnvelope(encrypted);
                return true;
            }
            catch (Exception e)
            {
                LogSaveFailure(processed, e);
                return false;
            }
        }

        private static string AddIntegrityEnvelope(string payload)
        {
            string safePayload = payload ?? string.Empty;
            string hash = ComputeSha256Hex(safePayload);
            return IntegrityEnvelopePrefix + hash + "\n" + safePayload;
        }

        private static string ValidateAndUnwrapIntegrityEnvelope(string storedPayload)
        {
            if (storedPayload == null || !storedPayload.StartsWith(IntegrityEnvelopePrefix, StringComparison.Ordinal))
            {
                // Backward compatibility: v1.1.2 and earlier payloads had no envelope.
                return storedPayload;
            }

            int digestStart = IntegrityEnvelopePrefix.Length;
            int separatorIndex = storedPayload.IndexOf('\n', digestStart);
            if (separatorIndex < 0 || separatorIndex - digestStart != Sha256HexLength)
            {
                throw new InvalidDataException("Save integrity envelope is malformed.");
            }

            string expectedHash = storedPayload.Substring(digestStart, Sha256HexLength);
            string payload = storedPayload.Substring(separatorIndex + 1);
            string actualHash = ComputeSha256Hex(payload);

            if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Save integrity check failed (SHA-256 mismatch).");
            }

            return payload;
        }

        private static string ComputeSha256Hex(string value)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
            StringBuilder builder = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }

        private bool PersistProcessedSynchronous(string processed, string stateHash)
        {
            try
            {
                StorageLocation location = BuildLocation();
                storageStrategy.Write(location, processed);
                lastPersistedStateHash = stateHash;

                DebugUtility.Log(this,
                    $"Saved using {storageStrategy.ActiveStrategyName}: {location.Key} ({Encoding.UTF8.GetByteCount(processed ?? string.Empty)} bytes)");
                return true;
            }
            catch (Exception e)
            {
                LogSaveFailure(processed, e);
                return false;
            }
        }

        protected T LoadFromFile()
        {
            return LoadFromFile(out _);
        }

        private T LoadFromFile(out LoadStatus status)
        {
            EnsureInitialized();
            StorageLocation location = BuildLocation();
            lastPersistedStateHash = null;

            SlotLoadOutcome primaryOutcome = TryLoadSlot(location, LoadSlot.Primary, out T primary, out _);
            if (primaryOutcome == SlotLoadOutcome.Loaded)
            {
                status = LoadStatus.LoadedPrimary;
                return primary;
            }

            // A .tmp payload is written completely before commit. If the process died
            // between those steps (especially on the first-ever save), a validated temp
            // payload can be newer than the rolling backup and is safe to promote.
            SlotLoadOutcome pendingOutcome = TryLoadSlot(location, LoadSlot.Pending, out T pending, out string validatedPendingPayload);
            if (pendingOutcome == SlotLoadOutcome.Loaded)
            {
                RestoreValidatedPayload(location, validatedPendingPayload, "pending temp");
                status = LoadStatus.RecoveredPending;
                return pending;
            }

            SlotLoadOutcome backupOutcome = TryLoadSlot(location, LoadSlot.Backup, out T backup, out string validatedBackupPayload);
            if (backupOutcome == SlotLoadOutcome.Loaded)
            {
                RestoreValidatedPayload(location, validatedBackupPayload, "backup");
                status = LoadStatus.RecoveredBackup;
                return backup;
            }

            // An unreadable .tmp is an interrupted write, not evidence worth protecting: the
            // integrity envelope already proved it incomplete. Only primary/backup decide
            // between "nothing saved yet" and "a save exists but cannot be decoded".
            status = primaryOutcome == SlotLoadOutcome.Missing &&
                     backupOutcome == SlotLoadOutcome.Missing
                ? LoadStatus.Missing
                : LoadStatus.Unreadable;

            return null;
        }

        private SlotLoadOutcome TryLoadSlot(in StorageLocation location, LoadSlot slot, out T result, out string storedPayload)
        {
            result = null;
            storedPayload = null;

            try
            {
                bool found = slot switch
                {
                    LoadSlot.Primary => storageStrategy.TryReadPrimary(location, out storedPayload),
                    LoadSlot.Pending => storageStrategy.TryReadPending(location, out storedPayload),
                    LoadSlot.Backup => storageStrategy.TryReadBackup(location, out storedPayload),
                    _ => false
                };

                if (!found)
                {
                    return SlotLoadOutcome.Missing;
                }

                string protectedPayload = ValidateAndUnwrapIntegrityEnvelope(storedPayload);
                string decrypted = encryptionStrategy.Decrypt(protectedPayload);
                result = serializationStrategy.Deserialize(decrypted);

                if (result != null)
                {
                    if (slot == LoadSlot.Primary)
                    {
                        // Only the primary is known to be on storage as-is; recovered slots are
                        // still being repaired, so the next automatic save must not be skipped.
                        lastPersistedStateHash = ComputeSha256Hex(decrypted);
                    }

                    string message = slot switch
                    {
                        LoadSlot.Primary => "Loaded!",
                        LoadSlot.Pending => "Loaded from validated pending temp payload.",
                        LoadSlot.Backup => "Loaded from backup.",
                        _ => "Loaded."
                    };
                    DebugUtility.Log(this, message);
                    return SlotLoadOutcome.Loaded;
                }

                throw new InvalidDataException("Deserializer returned null.");
            }
            catch (Exception e)
            {
                if (!string.IsNullOrEmpty(storedPayload) && slot != LoadSlot.Pending)
                {
                    storageStrategy.PreserveCorrupt(location, storedPayload, slot == LoadSlot.Primary);
                }

                string slotName = slot switch
                {
                    LoadSlot.Primary => "save",
                    LoadSlot.Pending => "pending temp save",
                    LoadSlot.Backup => "backup",
                    _ => "save slot"
                };

                if (slot == LoadSlot.Pending)
                {
                    DebugUtility.LogWarning(this,
                        $"Ignoring incomplete {slotName} data using {storageStrategy.ActiveStrategyName} (interrupted write). Reason: {e.Message}");
                }
                else
                {
                    DebugUtility.LogError(this,
                        $"Failed to load {slotName} data using {storageStrategy.ActiveStrategyName}. " +
                        $"The unreadable payload was preserved when the backend supports it. Reason: {e.Message}");
                }

                result = null;
                return SlotLoadOutcome.Unreadable;
            }
        }

        private void RestoreValidatedPayload(StorageLocation location, string validatedPayload, string sourceName)
        {
            if (storageStrategy.RequiresAsyncPersistence)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                lastRecoveryTask = RestoreValidatedPayloadAsync(location, validatedPayload, sourceName, acquireMutationGate: false);
#else
                lastRecoveryTask = RestoreValidatedPayloadAsync(location, validatedPayload, sourceName, acquireMutationGate: true);
#endif
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            RestoreValidatedPayloadSynchronous(location, validatedPayload, sourceName);
#else
            if (mutationGate.Wait(0))
            {
                try
                {
                    RestoreValidatedPayloadSynchronous(location, validatedPayload, sourceName);
                }
                finally
                {
                    mutationGate.Release();
                }
            }
            else
            {
                lastRecoveryTask = RestoreValidatedPayloadAsync(location, validatedPayload, sourceName, acquireMutationGate: true);
            }
#endif
        }

        private void RestoreValidatedPayloadSynchronous(StorageLocation location, string validatedPayload, string sourceName)
        {
            try
            {
                storageStrategy.RestorePrimary(location, validatedPayload);
                lastRecoveryTask = Task.CompletedTask;
                DebugUtility.Log(this, $"Restored validated {sourceName} payload to the primary save slot.");
            }
            catch (Exception e)
            {
                DebugUtility.LogError(this,
                    $"Loaded the recovery payload successfully, but failed to restore it to the primary slot using {storageStrategy.ActiveStrategyName}.\n{e}");
                lastRecoveryTask = Task.FromException(e);
            }
        }

        private async Task RestoreValidatedPayloadAsync(StorageLocation location, string validatedPayload, string sourceName, bool acquireMutationGate)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (acquireMutationGate)
            {
                await mutationGate.WaitAsync();
            }
#endif
            try
            {
                await storageStrategy.RestorePrimaryAsync(location, validatedPayload);
                DebugUtility.Log(this, $"Restored validated {sourceName} payload to the primary save slot.");
            }
            catch (Exception e)
            {
                DebugUtility.LogError(this,
                    $"Loaded the recovery payload successfully, but failed to persist the restored primary slot using {storageStrategy.ActiveStrategyName}.\n{e}");
                throw;
            }
            finally
            {
#if !UNITY_WEBGL || UNITY_EDITOR
                if (acquireMutationGate)
                {
                    mutationGate.Release();
                }
#endif
            }
        }

        private async Task<bool> TrySaveToFileAsync(T data)
        {
            // Serialize before waiting for an earlier mutation so this request captures
            // the object's state at the time Save/SaveAsync was called.
            if (!TryPreparePayload(data, out string processed, out string stateHash))
            {
                return false;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL strategies serialize their complete authoritative mutation +
            // IndexedDB flush in WebGlFileSync without SemaphoreSlim/System.Threading.
            return await PersistProcessedAsync(processed, stateHash);
#else
            await mutationGate.WaitAsync();
            try
            {
                return await PersistProcessedAsync(processed, stateHash);
            }
            finally
            {
                mutationGate.Release();
            }
#endif
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        private async Task<bool> PersistPreparedQueuedAsync(string processed, string stateHash)
        {
            await mutationGate.WaitAsync();
            try
            {
                return await PersistProcessedAsync(processed, stateHash);
            }
            finally
            {
                mutationGate.Release();
            }
        }
#endif

        private async Task<bool> PersistProcessedAsync(string processed, string stateHash)
        {
            try
            {
                StorageLocation location = BuildLocation();
                await storageStrategy.WriteAsync(location, processed);
                lastPersistedStateHash = stateHash;

                DebugUtility.Log(this,
                    $"Saved using {storageStrategy.ActiveStrategyName}: {location.Key} ({Encoding.UTF8.GetByteCount(processed ?? string.Empty)} bytes)");
                return true;
            }
            catch (Exception e)
            {
                LogSaveFailure(processed, e);
                return false;
            }
        }

        private void LogSaveFailure(string processed, Exception exception)
        {
            int byteCount = processed == null ? 0 : Encoding.UTF8.GetByteCount(processed);
            DebugUtility.LogError(this,
                $"SAVE FAILED\n" +
                $"Platform: {Application.platform}\n" +
                $"Strategy: {storageStrategy?.ActiveStrategyName ?? "<null>"}\n" +
                $"Key: {key}\n" +
                $"Path: {fullPath}\n" +
                $"Bytes: {byteCount}\n" +
                exception);
        }

        // Backward-compatible protected wrappers retained for existing subclasses.
        // They now route through the public synchronization paths so subclasses cannot
        // accidentally bypass slot-level mutation serialization.
        protected void SaveToFile(T data)
        {
            Save(data);
        }

        protected async Task SaveToFileAsync(T data)
        {
            await SaveAsync(data);
        }

        protected bool FileExists() => storageStrategy != null && storageStrategy.Exists(BuildLocation());

        public void Save(T obj)
        {
            EnsureInitialized();
            if (obj == null)
            {
                DebugUtility.LogWarning(this, "Persistence called with null object.");
                lastSaveTask = Task.FromResult(false);
                return;
            }

            // An explicit Save(data) is an intentional recovery/reset decision. It is
            // therefore allowed to replace a previously unreadable save and re-enable
            // later cache-based lifecycle saves.
            blockAutomaticSaveAfterUnreadableLoad = false;

            if (useCache)
            {
                cache = obj;
            }

            if (storageStrategy.RequiresAsyncPersistence)
            {
                lastSaveTask = TrySaveToFileAsync(obj);
                return;
            }

            if (!TryPreparePayload(obj, out string processed, out string stateHash))
            {
                lastSaveTask = Task.FromResult(false);
                return;
            }

            PersistPrepared(processed, stateHash);
        }

        private void PersistPrepared(string processed, string stateHash)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Only relevant to a custom synchronous WebGL storage strategy.
            lastSaveTask = Task.FromResult(PersistProcessedSynchronous(processed, stateHash));
#else
            if (mutationGate.Wait(0))
            {
                bool saved;
                try
                {
                    saved = PersistProcessedSynchronous(processed, stateHash);
                }
                finally
                {
                    mutationGate.Release();
                }

                lastSaveTask = Task.FromResult(saved);
            }
            else
            {
                // Never block Unity's main thread waiting for an async save continuation.
                // Queue this captured payload behind the current mutation instead.
                lastSaveTask = PersistPreparedQueuedAsync(processed, stateHash);
                DebugUtility.LogWarning(this,
                    "Save queued behind an in-progress save/recovery/delete operation; no concurrent access to the same temp/backup files will occur.");
            }
#endif
        }

        /// <summary>
        /// Explicit awaitable save for callers that need to know when persistence is
        /// complete. This is required for a definite WebGL IndexedDB completion point.
        /// </summary>
        public Task<bool> SaveAsync(T obj)
        {
            EnsureInitialized();
            if (obj == null)
            {
                DebugUtility.LogWarning(this, "Persistence called with null object.");
                lastSaveTask = Task.FromResult(false);
                return lastSaveTask;
            }

            blockAutomaticSaveAfterUnreadableLoad = false;

            if (useCache)
            {
                cache = obj;
            }

            lastSaveTask = TrySaveToFileAsync(obj);
            return lastSaveTask;
        }

        /// <summary>
        /// Loads data using the legacy API. Missing data returns a new T. Unreadable
        /// data also returns a new T for backward compatibility, but LastLoadStatus is
        /// set to Unreadable and automatic cache saves are blocked until Save(data) or
        /// SaveAsync(data) explicitly establishes replacement state.
        /// </summary>
        public T Load()
        {
            if (useCache && cache != null)
            {
                return cache;
            }

            T loaded = LoadFromFile(out LoadStatus status);
            LastLoadStatus = status;
            blockAutomaticSaveAfterUnreadableLoad = status == LoadStatus.Unreadable;

            T value = loaded ?? new T();
            if (useCache)
            {
                cache = value;
            }

            if (status == LoadStatus.Unreadable)
            {
                DebugUtility.LogError(this,
                    "Save data exists but could not be loaded from either the primary or backup slot. " +
                    "Load() returned a new default object for backward compatibility. Automatic lifecycle saves are blocked until Save(data) explicitly confirms replacement state. Check LastLoadStatus before treating this as a new game.");
            }

            return value;
        }

        /// <summary>
        /// Non-ambiguous load API. Returns true only when a primary, pending temp, or backup payload
        /// was successfully decoded. Missing and unreadable saves both return false;
        /// inspect status to distinguish them.
        /// </summary>
        public bool TryLoad(out T data, out LoadStatus status)
        {
            data = LoadFromFile(out status);
            LastLoadStatus = status;
            blockAutomaticSaveAfterUnreadableLoad = status == LoadStatus.Unreadable;

            if (useCache)
            {
                cache = data;
            }

            return data != null;
        }

        public virtual void Save()
        {
            if (!useCache)
            {
                DebugUtility.LogWarning(this,
                    "Parameterless Save requires Use Cache. Call Save(data), or enable Use Cache and initialize it via Load()/Save(data).");
                return;
            }

            if (cache == null)
            {
                DebugUtility.LogWarning(this,
                    "Parameterless Save has no cached state. Call Load() once or Save(data) before relying on pause/focus/quit autosaves. Existing save data was not overwritten.");
                return;
            }

            SaveAutomatic(cache);
        }

        /// <summary>
        /// True after a load found stored data it could not decode. Automatic saves are refused
        /// until an explicit <see cref="Save(T)"/> / <see cref="SaveAsync(T)"/> confirms replacement state.
        /// </summary>
        public bool IsAutomaticSaveBlocked => blockAutomaticSaveAfterUnreadableLoad;

        /// <summary>
        /// Saves state on the framework's behalf (lifecycle hooks, model-change autosaves). Unlike
        /// <see cref="Save(T)"/> it never replaces unreadable data and skips the write when
        /// <paramref name="data"/> serializes to exactly what storage already holds.
        /// Returns false when the save was refused or could not be prepared.
        /// </summary>
        protected bool SaveAutomatic(T data)
        {
            if (data == null)
            {
                return false;
            }

            if (blockAutomaticSaveAfterUnreadableLoad)
            {
                DebugUtility.LogError(this,
                    "Automatic save skipped because the last load was unreadable. Call Save(data) or SaveAsync(data) explicitly after the game has decided to reset/recover the state. This prevents a blank new T() from overwriting recoverable evidence.");
                return false;
            }

            EnsureInitialized();
            if (useCache)
            {
                cache = data;
            }

            if (!TryPreparePayload(data, out string processed, out string stateHash))
            {
                lastSaveTask = Task.FromResult(false);
                return false;
            }

            // Pause, focus loss, quit and destroy can all fire back to back. Skip the write when
            // the state is byte-for-byte what was last loaded from or written to storage.
            if (stateHash == lastPersistedStateHash)
            {
                lastSaveTask = Task.FromResult(true);
                return true;
            }

            if (storageStrategy.RequiresAsyncPersistence)
            {
                lastSaveTask = PersistPreparedAsync(processed, stateHash);
                return true;
            }

            PersistPrepared(processed, stateHash);
            return true;
        }

        private Task<bool> PersistPreparedAsync(string processed, string stateHash)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return PersistProcessedAsync(processed, stateHash);
#else
            return PersistPreparedQueuedAsync(processed, stateHash);
#endif
        }

        protected virtual void OnEnable()
        {
            Application.quitting += HandleApplicationQuitting;
        }

        protected virtual void OnDisable()
        {
            Application.quitting -= HandleApplicationQuitting;
        }

        private void HandleApplicationQuitting()
        {
            isQuitting = true;
            Save();
        }

        protected virtual void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                Save();
            }
        }

        protected virtual void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Save();
            }
        }

        protected virtual void OnDestroy()
        {
            // On desktop this catches scene/object destruction. On mobile/WebGL it is
            // only a last line of defense; callers should save at meaningful progress
            // checkpoints because process/tab termination is not guaranteed to wait.
            if (!isQuitting)
            {
                Save();
            }

        }
    }
}
