using System;
using System.IO;
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
    /// platform (e.g. files on desktop, PlayerPrefs/IndexedDB on WebGL) without
    /// branching. Peer to <see cref="SerializationStrategy{T}"/> and
    /// <see cref="EncryptionStrategy"/>.
    /// </summary>
    [Serializable]
    public abstract class StorageStrategy
    {
        public abstract void Write(in StorageLocation location, string content);

        /// <summary>Reads the current save. Returns false when nothing is stored.</summary>
        public abstract bool TryReadPrimary(in StorageLocation location, out string content);

        /// <summary>Reads the previous good save, if the backend keeps one. Default: none.</summary>
        public virtual bool TryReadBackup(in StorageLocation location, out string content)
        {
            content = null;
            return false;
        }

        public abstract bool Exists(in StorageLocation location);

        public abstract void Delete(in StorageLocation location);

        public virtual Task WriteAsync(StorageLocation location, string content)
        {
            Write(location, content);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Writes to a file with a crash-safe temp-then-swap and a rolling .bak.
    /// The default backend; correct on desktop and mobile. On WebGL a plain file
    /// write lives only in memory and is lost on refresh — use
    /// <see cref="WebGlFileStorageStrategy"/> (which flushes to IndexedDB) or
    /// <see cref="PlayerPrefsStorageStrategy"/> there instead.
    /// </summary>
    [Serializable]
    public class FileStorageStrategy : StorageStrategy
    {
        public override void Write(in StorageLocation location, string content)
        {
            EnsureFolder(location.Folder);
            File.WriteAllText(location.TempPath, content);
            Commit(location);
            OnPersisted(location);
        }

        public override async Task WriteAsync(StorageLocation location, string content)
        {
            EnsureFolder(location.Folder);
            await File.WriteAllTextAsync(location.TempPath, content);
            Commit(location);
            OnPersisted(location);
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

        public override bool Exists(in StorageLocation location) => File.Exists(location.FullPath);

        public override void Delete(in StorageLocation location)
        {
            DeleteIfExists(location.FullPath);
            DeleteIfExists(location.BackupPath);
            DeleteIfExists(location.TempPath);
            OnPersisted(location);
        }

        /// <summary>Runs after a write/delete lands on disk. WebGL uses it to flush IndexedDB.</summary>
        protected virtual void OnPersisted(in StorageLocation location) { }

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
            if (File.Exists(location.FullPath))
            {
                try
                {
                    // Atomic on NTFS: swaps temp into place and keeps the old file as .bak.
                    File.Replace(location.TempPath, location.FullPath, location.BackupPath);
                }
                catch (PlatformNotSupportedException)
                {
                    File.Copy(location.FullPath, location.BackupPath, overwrite: true);
                    File.Delete(location.FullPath);
                    File.Move(location.TempPath, location.FullPath);
                }
            }
            else
            {
                File.Move(location.TempPath, location.FullPath);
            }
        }
    }

    /// <summary>
    /// Stores the save as a single PlayerPrefs string. Works on EVERY platform,
    /// WebGL included (Unity persists PlayerPrefs to IndexedDB and Save() flushes
    /// it), so it's the simplest WebGL-safe option. Best for small saves — large
    /// blobs belong in <see cref="WebGlFileStorageStrategy"/>.
    /// </summary>
    [Serializable]
    public sealed class PlayerPrefsStorageStrategy : StorageStrategy
    {
        public override void Write(in StorageLocation location, string content)
        {
            PlayerPrefs.SetString(location.Key, content);
            PlayerPrefs.Save();
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

        public override bool Exists(in StorageLocation location) => PlayerPrefs.HasKey(location.Key);

        public override void Delete(in StorageLocation location)
        {
            if (PlayerPrefs.HasKey(location.Key))
            {
                PlayerPrefs.DeleteKey(location.Key);
                PlayerPrefs.Save();
            }
        }
    }

    /// <summary>
    /// A file save that flushes Unity's IndexedDB-backed virtual filesystem after
    /// every write/delete, so saves survive a page refresh on WebGL. Off WebGL it
    /// behaves exactly like <see cref="FileStorageStrategy"/>, so it stays testable
    /// in the Editor. Use for larger WebGL saves; for small ones PlayerPrefs is
    /// simpler.
    /// </summary>
    [Serializable]
    public sealed class WebGlFileStorageStrategy : FileStorageStrategy
    {
        protected override void OnPersisted(in StorageLocation location)
        {
            WebGlFileSync.Flush();
        }
    }

    /// <summary>
    /// Routes to a different backend per platform — one save component that does
    /// the right thing everywhere. Assign any strategy to each slot (that's the
    /// "multiple options per platform"): e.g. files on desktop/mobile, PlayerPrefs
    /// or WebGL-file on the web.
    /// </summary>
    [Serializable]
    public sealed class PlatformStorageStrategy : StorageStrategy
    {
        [SerializeReference, SubclassSelector, Tooltip("Used in the Editor (Play mode), so you can inspect saves on disk.")]
        private StorageStrategy editor = new FileStorageStrategy();

        [SerializeReference, SubclassSelector, Tooltip("Windows / macOS / Linux standalone builds.")]
        private StorageStrategy standalone = new FileStorageStrategy();

        [SerializeReference, SubclassSelector, Tooltip("Android / iOS.")]
        private StorageStrategy mobile = new FileStorageStrategy();

        [SerializeReference, SubclassSelector, Tooltip("WebGL — PlayerPrefs or WebGlFileStorage keep saves across refreshes.")]
        private StorageStrategy web = new PlayerPrefsStorageStrategy();

        [SerializeReference, SubclassSelector, Tooltip("Any platform not matched above.")]
        private StorageStrategy fallback = new PlayerPrefsStorageStrategy();

        private StorageStrategy Active
        {
            get
            {
                if (Application.isEditor && editor != null)
                {
                    return editor;
                }

                return Application.platform switch
                {
                    RuntimePlatform.WindowsPlayer or RuntimePlatform.OSXPlayer or RuntimePlatform.LinuxPlayer
                        => standalone ?? fallback,
                    RuntimePlatform.Android or RuntimePlatform.IPhonePlayer
                        => mobile ?? fallback,
                    RuntimePlatform.WebGLPlayer
                        => web ?? fallback,
                    _ => fallback
                };
            }
        }

        public override void Write(in StorageLocation location, string content) => Active.Write(location, content);

        public override Task WriteAsync(StorageLocation location, string content) => Active.WriteAsync(location, content);

        public override bool TryReadPrimary(in StorageLocation location, out string content) =>
            Active.TryReadPrimary(location, out content);

        public override bool TryReadBackup(in StorageLocation location, out string content) =>
            Active.TryReadBackup(location, out content);

        public override bool Exists(in StorageLocation location) => Active.Exists(location);

        public override void Delete(in StorageLocation location) => Active.Delete(location);
    }
}
