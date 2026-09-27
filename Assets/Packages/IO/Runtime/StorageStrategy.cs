using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MyToolz.IO
{
    /// <summary>
    /// Everything a <see cref="StorageStrategy"/> might need to persist one save.
    /// File-based stores use the paths; key/value stores (PlayerPrefs, browser
    /// storage) use <see cref="Key"/> — a stable logical id like "Saves/save.json".
    /// </summary>
    public readonly struct StorageLocation
    {
        public readonly string FullPath;
        public readonly string TempPath;
        public readonly string BackupPath;
        public readonly string Folder;
        public readonly string Key;

        public StorageLocation(string fullPath, string tempPath, string backupPath, string folder, string key)
        {
            FullPath = fullPath;
            TempPath = tempPath;
            BackupPath = backupPath;
            Folder = folder;
            Key = key;
        }
    }

    /// <summary>
    /// Abstracts WHERE the already-serialized, already-encrypted save bytes go.
    /// Swap or route these to make one codebase persist correctly on every
    /// platform without platform branches in the saver itself.
    /// </summary>
    [Serializable]
    public abstract class StorageStrategy
    {
        /// <summary>
        /// True when persistence completion cannot be represented by the synchronous
        /// APIs. WebGL file saves use this because IDBFS -> IndexedDB synchronization
        /// completes asynchronously in the browser.
        /// </summary>
        public virtual bool RequiresAsyncPersistence => false;

        /// <summary>Name of the backend that will actually handle this request.</summary>
        public virtual string ActiveStrategyName => GetType().Name;

        public abstract void Write(in StorageLocation location, string content);

        /// <summary>Reads the current save. Returns false when nothing is stored.</summary>
        public abstract bool TryReadPrimary(in StorageLocation location, out string content);

        /// <summary>Reads the previous good save, if the backend keeps one. Default: none.</summary>
        public virtual bool TryReadBackup(in StorageLocation location, out string content)
        {
            content = null;
            return false;
        }

        /// <summary>Reads a completed-but-not-committed pending payload, such as a .tmp file.</summary>
        public virtual bool TryReadPending(in StorageLocation location, out string content)
        {
            content = null;
            return false;
        }

        public abstract bool Exists(in StorageLocation location);

        /// <summary>True when a rolling backup exists even if the primary is absent.</summary>
        public virtual bool BackupExists(in StorageLocation location) => false;

        /// <summary>True when any primary/recovery/diagnostic data belonging to this save exists.</summary>
        public virtual bool AnyDataExists(in StorageLocation location) => Exists(location) || BackupExists(location);

        public abstract void Delete(in StorageLocation location);

        public virtual Task WriteAsync(StorageLocation location, string content)
        {
            Write(location, content);
            return Task.CompletedTask;
        }

        public virtual Task DeleteAsync(StorageLocation location)
        {
            Delete(location);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Rewrites a validated backup into the primary slot. File backends override
        /// this so the known-good backup is not replaced by the corrupt primary.
        /// </summary>
        public virtual void RestorePrimary(in StorageLocation location, string validatedContent) =>
            Write(location, validatedContent);

        public virtual Task RestorePrimaryAsync(StorageLocation location, string validatedContent)
        {
            RestorePrimary(location, validatedContent);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Best-effort diagnostic preservation for payloads that were present but
        /// could not be decrypted/deserialized. Backends may intentionally no-op.
        /// </summary>
        public virtual void PreserveCorrupt(in StorageLocation location, string content, bool primary) { }
    }

    /// <summary>
    /// Writes to a file with a temp-then-swap and a rolling .bak. File.Replace is
    /// attempted where available, with a recoverable copy fallback for filesystems
    /// where Replace is unsupported or unreliable.
    /// </summary>
    [Serializable]
    public class FileStorageStrategy : StorageStrategy
    {
        public override bool RequiresAsyncPersistence
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public override void Write(in StorageLocation location, string content)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new InvalidOperationException("WebGL file persistence requires WriteAsync/SaveAsync.");
#else
            WriteFileCore(location, content);
            OnPersisted(location);
#endif
        }

        public override async Task WriteAsync(StorageLocation location, string content)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            await WebGlFileSync.RunExclusiveAsync(async () =>
            {
                WriteFileCore(location, content);
                await OnPersistedAsync(location);
                await WebGlFileSync.FlushAsync();
            });
#else
            EnsureFolder(location.Folder);
            await File.WriteAllTextAsync(location.TempPath, content);
            Commit(location);
            await OnPersistedAsync(location);
#endif
        }

        public override bool TryReadPrimary(in StorageLocation location, out string content)
        {
            content = null;
            if (!File.Exists(location.FullPath))
            {
                return false;
            }

            content = File.ReadAllText(location.FullPath);
            return true;
        }

        public override bool TryReadBackup(in StorageLocation location, out string content)
        {
            content = null;
            if (!File.Exists(location.BackupPath))
            {
                return false;
            }

            content = File.ReadAllText(location.BackupPath);
            return true;
        }

        public override bool TryReadPending(in StorageLocation location, out string content)
        {
            content = null;
            if (!File.Exists(location.TempPath))
            {
                return false;
            }

            content = File.ReadAllText(location.TempPath);
            return true;
        }

        public override bool Exists(in StorageLocation location) => File.Exists(location.FullPath);

        public override bool BackupExists(in StorageLocation location) => File.Exists(location.BackupPath);

        public override bool AnyDataExists(in StorageLocation location) =>
            File.Exists(location.FullPath) ||
            File.Exists(location.BackupPath) ||
            File.Exists(location.TempPath) ||
            File.Exists(location.FullPath + ".corrupt") ||
            File.Exists(location.BackupPath + ".corrupt");

        public override void Delete(in StorageLocation location)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new InvalidOperationException("WebGL file persistence requires DeleteAsync.");
#else
            DeleteFileCore(location);
            OnPersisted(location);
#endif
        }

        public override async Task DeleteAsync(StorageLocation location)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            await WebGlFileSync.RunExclusiveAsync(async () =>
            {
                DeleteFileCore(location);
                await OnPersistedAsync(location);
                await WebGlFileSync.FlushAsync();
            });
#else
            DeleteFileCore(location);
            await OnPersistedAsync(location);
#endif
        }

        public override void RestorePrimary(in StorageLocation location, string validatedContent)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new InvalidOperationException("WebGL file persistence requires RestorePrimaryAsync.");
#else
            RestorePrimaryCore(location, validatedContent);
            OnPersisted(location);
#endif
        }

        public override async Task RestorePrimaryAsync(StorageLocation location, string validatedContent)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            await WebGlFileSync.RunExclusiveAsync(async () =>
            {
                RestorePrimaryCore(location, validatedContent);
                await OnPersistedAsync(location);
                await WebGlFileSync.FlushAsync();
            });
#else
            RestorePrimaryCore(location, validatedContent);
            await OnPersistedAsync(location);
#endif
        }

        public override void PreserveCorrupt(in StorageLocation location, string content, bool primary)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return;
#else
            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            try
            {
                EnsureFolder(location.Folder);
                string sourcePath = primary ? location.FullPath : location.BackupPath;
                string corruptPath = sourcePath + ".corrupt";
                File.WriteAllText(corruptPath, content);
            }
            catch
            {
                // Diagnostic preservation must never hide the original load failure.
            }
#endif
        }

        /// <summary>Runs after a synchronous write/delete lands on disk.</summary>
        protected virtual void OnPersisted(in StorageLocation location) { }

        /// <summary>Async counterpart used by WebGL to await IndexedDB synchronization.</summary>
        protected virtual Task OnPersistedAsync(StorageLocation location)
        {
            OnPersisted(location);
            return Task.CompletedTask;
        }

        protected static void WriteFileCore(in StorageLocation location, string content)
        {
            EnsureFolder(location.Folder);
            File.WriteAllText(location.TempPath, content);
            Commit(location);
        }

        protected static void DeleteFileCore(in StorageLocation location)
        {
            DeleteIfExists(location.FullPath);
            DeleteIfExists(location.BackupPath);
            DeleteIfExists(location.TempPath);
            DeleteIfExists(location.FullPath + ".corrupt");
            DeleteIfExists(location.BackupPath + ".corrupt");
        }

        protected static void RestorePrimaryCore(in StorageLocation location, string validatedContent)
        {
            EnsureFolder(location.Folder);
            File.WriteAllText(location.TempPath, validatedContent);

            // Do not rotate the current primary into .bak here: the current primary
            // is exactly the payload that failed validation. Keep the known-good
            // backup intact while restoring it to the primary slot.
            try
            {
                if (File.Exists(location.FullPath))
                {
                    File.Copy(location.TempPath, location.FullPath, overwrite: true);
                    File.Delete(location.TempPath);
                }
                else
                {
                    File.Move(location.TempPath, location.FullPath);
                }
            }
            catch
            {
                // Leave the .tmp and .bak in place for recovery/inspection.
                throw;
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void Commit(in StorageLocation location)
        {
            if (!File.Exists(location.FullPath))
            {
                File.Move(location.TempPath, location.FullPath);
                return;
            }

            try
            {
                // Atomic on filesystems/.NET implementations that support it.
                File.Replace(location.TempPath, location.FullPath, location.BackupPath);
            }
            catch (Exception replaceException) when (IsExpectedReplaceFailure(replaceException))
            {
                try
                {
                    PortableCommit(location);
                }
                catch (Exception fallbackException)
                {
                    throw new IOException(
                        "Failed to replace the save file using both File.Replace and the portable fallback.",
                        new AggregateException(replaceException, fallbackException));
                }
            }
        }

        private static bool IsExpectedReplaceFailure(Exception exception) =>
            exception is PlatformNotSupportedException ||
            exception is NotSupportedException ||
            exception is IOException ||
            exception is UnauthorizedAccessException;

        private static void PortableCommit(in StorageLocation location)
        {
            // Keep the previous known-good primary first. Then copy the completed
            // temp payload over the primary. If that second step fails, attempt to
            // put the backup back before propagating the error.
            File.Copy(location.FullPath, location.BackupPath, overwrite: true);

            try
            {
                File.Copy(location.TempPath, location.FullPath, overwrite: true);
                File.Delete(location.TempPath);
            }
            catch
            {
                try
                {
                    if (File.Exists(location.BackupPath))
                    {
                        File.Copy(location.BackupPath, location.FullPath, overwrite: true);
                    }
                }
                catch
                {
                    // Preserve the original fallback exception; the .bak remains.
                }

                throw;
            }
        }
    }

    /// <summary>
    /// Stores the save as a single PlayerPrefs string. Works on every Unity target,
    /// including WebGL. Best for small saves; large blobs belong in a file backend.
    /// </summary>
    [Serializable]
    public sealed class PlayerPrefsStorageStrategy : StorageStrategy
    {
        private const int WebGlHardLimitBytes = 1024 * 1024;
        private const int WebGlRecommendedPrimaryBudgetBytes = 512 * 1024;
        private const int WebGlRecommendedRollingPairBudgetBytes = 768 * 1024;

        private static string BackupKey(in StorageLocation location) => location.Key + ".bak";
        private static string CorruptKey(in StorageLocation location, bool primary) =>
            location.Key + (primary ? ".corrupt" : ".bak.corrupt");

        public override bool RequiresAsyncPersistence
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public override void Write(in StorageLocation location, string content)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new InvalidOperationException(
                "PlayerPrefsStorageStrategy cannot confirm WebGL IndexedDB persistence synchronously. Use WriteAsync/SaveAsync.");
#else
            WriteCore(location, content);
#endif
        }

        public override Task WriteAsync(StorageLocation location, string content)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return WebGlFileSync.RunExclusiveAsync(async () =>
            {
                WriteCore(location, content);
                await WebGlFileSync.FlushAsync();
            });
#else
            WriteCore(location, content);
            return Task.CompletedTask;
#endif
        }

        private static void WriteCore(in StorageLocation location, string content)
        {
            string backupKey = BackupKey(location);
            bool keepBackup = true;

#if UNITY_WEBGL && !UNITY_EDITOR
            int newByteCount = Encoding.UTF8.GetByteCount(content ?? string.Empty);
            if (newByteCount >= WebGlHardLimitBytes)
            {
                throw new InvalidOperationException(
                    $"PlayerPrefs save '{location.Key}' is {newByteCount} bytes. WebGL PlayerPrefs is limited to about 1 MB total; use WebGlFileStorageStrategy for large saves.");
            }

            if (newByteCount > WebGlRecommendedPrimaryBudgetBytes)
            {
                Debug.LogWarning(
                    $"[MyToolz.IO] PlayerPrefs save '{location.Key}' is {newByteCount} bytes. This exceeds the conservative 512 KB per-save budget and leaves less headroom for other PlayerPrefs keys. Prefer WebGlFileStorageStrategy.");
            }

            if (PlayerPrefs.HasKey(location.Key))
            {
                string previous = PlayerPrefs.GetString(location.Key);
                int rollingPairBytes = newByteCount + Encoding.UTF8.GetByteCount(previous ?? string.Empty);
                if (rollingPairBytes >= WebGlRecommendedRollingPairBudgetBytes)
                {
                    keepBackup = false;
                    Debug.LogWarning(
                        $"[MyToolz.IO] PlayerPrefs backup skipped for '{location.Key}' because the primary + backup pair would consume {rollingPairBytes} bytes. " +
                        "The WebGL PlayerPrefs limit is store-wide and Unity exposes no API to measure all keys, so a conservative headroom budget is used. Prefer WebGlFileStorageStrategy for larger saves.");
                }
            }
#endif

            if (PlayerPrefs.HasKey(location.Key) && keepBackup)
            {
                PlayerPrefs.SetString(backupKey, PlayerPrefs.GetString(location.Key));
            }
            else if (!keepBackup && PlayerPrefs.HasKey(backupKey))
            {
                PlayerPrefs.DeleteKey(backupKey);
            }

            try
            {
                PlayerPrefs.SetString(location.Key, content);
                PlayerPrefs.Save();
            }
            catch (PlayerPrefsException e)
            {
                throw new IOException(
                    $"PlayerPrefs rejected save '{location.Key}'. On WebGL this commonly means the store-wide IndexedDB/PlayerPrefs quota was exceeded.", e);
            }
        }

        public override bool TryReadPrimary(in StorageLocation location, out string content)
        {
            if (PlayerPrefs.HasKey(location.Key))
            {
                content = PlayerPrefs.GetString(location.Key);
                return true;
            }

            content = null;
            return false;
        }

        public override bool TryReadBackup(in StorageLocation location, out string content)
        {
            string backupKey = BackupKey(location);
            if (PlayerPrefs.HasKey(backupKey))
            {
                content = PlayerPrefs.GetString(backupKey);
                return true;
            }

            content = null;
            return false;
        }

        public override bool Exists(in StorageLocation location) => PlayerPrefs.HasKey(location.Key);

        public override bool BackupExists(in StorageLocation location) => PlayerPrefs.HasKey(BackupKey(location));

        public override bool AnyDataExists(in StorageLocation location) =>
            PlayerPrefs.HasKey(location.Key) ||
            PlayerPrefs.HasKey(BackupKey(location)) ||
            PlayerPrefs.HasKey(CorruptKey(location, primary: true)) ||
            PlayerPrefs.HasKey(CorruptKey(location, primary: false));

        public override void Delete(in StorageLocation location)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new InvalidOperationException(
                "PlayerPrefsStorageStrategy cannot confirm WebGL IndexedDB deletion synchronously. Use DeleteAsync.");
#else
            DeleteCore(location);
#endif
        }

        public override Task DeleteAsync(StorageLocation location)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return WebGlFileSync.RunExclusiveAsync(async () =>
            {
                DeleteCore(location);
                await WebGlFileSync.FlushAsync();
            });
#else
            DeleteCore(location);
            return Task.CompletedTask;
#endif
        }

        private static void DeleteCore(in StorageLocation location)
        {
            string[] keys =
            {
                location.Key,
                BackupKey(location),
                CorruptKey(location, primary: true),
                CorruptKey(location, primary: false)
            };

            bool changed = false;
            foreach (string prefKey in keys)
            {
                if (!PlayerPrefs.HasKey(prefKey))
                {
                    continue;
                }

                PlayerPrefs.DeleteKey(prefKey);
                changed = true;
            }

            if (changed)
            {
                PlayerPrefs.Save();
            }
        }

        public override void RestorePrimary(in StorageLocation location, string validatedContent)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new InvalidOperationException(
                "PlayerPrefsStorageStrategy cannot confirm WebGL IndexedDB restoration synchronously. Use RestorePrimaryAsync.");
#else
            RestorePrimaryCore(location, validatedContent);
#endif
        }

        public override Task RestorePrimaryAsync(StorageLocation location, string validatedContent)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return WebGlFileSync.RunExclusiveAsync(async () =>
            {
                RestorePrimaryCore(location, validatedContent);
                await WebGlFileSync.FlushAsync();
            });
#else
            RestorePrimaryCore(location, validatedContent);
            return Task.CompletedTask;
#endif
        }

        private static void RestorePrimaryCore(in StorageLocation location, string validatedContent)
        {
            try
            {
                PlayerPrefs.SetString(location.Key, validatedContent);
                PlayerPrefs.Save();
            }
            catch (PlayerPrefsException e)
            {
                throw new IOException($"PlayerPrefs rejected restoration of '{location.Key}'.", e);
            }
        }

        public override void PreserveCorrupt(in StorageLocation location, string content, bool primary)
        {
            if (string.IsNullOrEmpty(content))
            {
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // The original corrupt primary/backup already remains in PlayerPrefs. Do
            // not duplicate it into the store and consume more of the global ~1 MB
            // budget. LastLoadStatus exposes the recovery condition to gameplay code.
            return;
#else
            try
            {
                PlayerPrefs.SetString(CorruptKey(location, primary), content);
                PlayerPrefs.Save();
            }
            catch
            {
                // Diagnostic preservation must never hide the original load failure.
            }
#endif
        }
    }

    /// <summary>
    /// A file save that flushes Unity's IDBFS virtual filesystem to IndexedDB after
    /// every mutation. On a real WebGL player, callers must use the async APIs so
    /// completion/failure of FS.syncfs is observable instead of reporting success
    /// while the browser commit is still pending.
    /// </summary>
    [Serializable]
    public sealed class WebGlFileStorageStrategy : FileStorageStrategy
    {
    }

    /// <summary>
    /// Routes to a different backend per platform. Empty slots always resolve to a
    /// non-null emergency PlayerPrefs backend instead of causing NullReferenceException.
    /// </summary>
    [Serializable]
    public sealed class PlatformStorageStrategy : StorageStrategy
    {
        private static readonly StorageStrategy EmergencyFallback = new PlayerPrefsStorageStrategy();

        [SerializeReference, SubclassSelector, Tooltip("Used in the Editor (Play mode), so you can inspect saves on disk.")]
        private StorageStrategy editor = new FileStorageStrategy();

        [SerializeReference, SubclassSelector, Tooltip("Windows / macOS / Linux standalone builds.")]
        private StorageStrategy standalone = new FileStorageStrategy();

        [SerializeReference, SubclassSelector, Tooltip("Android / iOS.")]
        private StorageStrategy mobile = new FileStorageStrategy();

        [SerializeReference, SubclassSelector, Tooltip("WebGL — WebGlFileStorage is the durable default; PlayerPrefs is intended only for small saves.")]
        private StorageStrategy web = new WebGlFileStorageStrategy();

        [SerializeReference, SubclassSelector, Tooltip("Any platform not matched above.")]
        private StorageStrategy fallback = new PlayerPrefsStorageStrategy();

        private StorageStrategy SafeFallback => fallback ?? EmergencyFallback;

        private StorageStrategy Active
        {
            get
            {
                if (Application.isEditor)
                {
                    return editor ?? SafeFallback;
                }

                return Application.platform switch
                {
                    RuntimePlatform.WindowsPlayer or RuntimePlatform.OSXPlayer or RuntimePlatform.LinuxPlayer
                        => standalone ?? SafeFallback,
                    RuntimePlatform.Android or RuntimePlatform.IPhonePlayer
                        => mobile ?? SafeFallback,
                    RuntimePlatform.WebGLPlayer
                        => web ?? SafeFallback,
                    _ => SafeFallback
                };
            }
        }

        public override bool RequiresAsyncPersistence => Active.RequiresAsyncPersistence;
        public override string ActiveStrategyName => Active.ActiveStrategyName;

        public override void Write(in StorageLocation location, string content) => Active.Write(location, content);

        public override Task WriteAsync(StorageLocation location, string content) => Active.WriteAsync(location, content);

        public override bool TryReadPrimary(in StorageLocation location, out string content) =>
            Active.TryReadPrimary(location, out content);

        public override bool TryReadBackup(in StorageLocation location, out string content) =>
            Active.TryReadBackup(location, out content);

        public override bool TryReadPending(in StorageLocation location, out string content) =>
            Active.TryReadPending(location, out content);

        public override bool Exists(in StorageLocation location) => Active.Exists(location);

        public override bool BackupExists(in StorageLocation location) => Active.BackupExists(location);

        public override bool AnyDataExists(in StorageLocation location) => Active.AnyDataExists(location);

        public override void Delete(in StorageLocation location) => Active.Delete(location);

        public override Task DeleteAsync(StorageLocation location) => Active.DeleteAsync(location);

        public override void RestorePrimary(in StorageLocation location, string validatedContent) =>
            Active.RestorePrimary(location, validatedContent);

        public override Task RestorePrimaryAsync(StorageLocation location, string validatedContent) =>
            Active.RestorePrimaryAsync(location, validatedContent);

        public override void PreserveCorrupt(in StorageLocation location, string content, bool primary) =>
            Active.PreserveCorrupt(location, content, primary);
    }
}
