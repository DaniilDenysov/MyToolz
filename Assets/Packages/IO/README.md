# IO — Save / Load

`SaveLoadBase<T>` is a Zenject `MonoInstaller` that persists one data object through pluggable
serialization, encryption and storage strategies. Subclass it, drop it on a context, and inject
the interfaces it binds.

```csharp
[Serializable] public class Progress { public int Level; }

public class ProgressSaver : SaveLoadBase<Progress> { }

// Consumers inject interfaces, never the concrete saver.
[Inject] void Construct(IRecoverableSaver<Progress> saver) { this.saver = saver; }

if (saver.TryLoad(out Progress progress, out LoadStatus status)) { /* existing save */ }
else if (status == LoadStatus.Missing) { progress = new Progress(); /* first run */ }
else { /* LoadStatus.Unreadable: a save exists but could not be decoded — ask/recover */ }
```

## Bound interfaces

| Interface | Purpose |
|---|---|
| `ISaver<T>` | `Load()`, `Save(data)`, parameterless `Save()` (cache only). |
| `IRecoverableSaver<T>` | Adds `LastLoadStatus` and `TryLoad(out data, out status)`. |
| `IAsyncSaver<T>` | `Task<bool> SaveAsync(data)` — awaitable completion (required for a definite WebGL commit). |

## What changed in 2.0.0

- **Load outcomes are explicit.** `LoadStatus` (`NotAttempted`, `LoadedPrimary`, `RecoveredPending`,
  `RecoveredBackup`, `Missing`, `Unreadable`) is a namespace-level enum; `LoadStatusExtensions.IsLoaded()`
  tells whether stored data was decoded. `Load()` still returns `new T()` for missing/unreadable data.
- **The cache stays consistent.** With **Use Cache**, `Save(data)` / `SaveAsync(data)` replace the cached
  object, so `Load(A) → Save(B) → Save()` writes B (it used to write A over B).
- **Unreadable saves are protected.** When the primary/backup exist but cannot be decoded, the payload is
  copied to `.corrupt` (non-WebGL), `LastLoadStatus` is `Unreadable`, and automatic lifecycle saves are
  blocked until the game calls `Save(data)` explicitly. A blank `new T()` can no longer silently replace
  recoverable data.
- **Automatic saves skip unchanged state.** Pause, focus loss, quit and destroy can fire back to back. The
  parameterless `Save()` hashes the serialized cache and skips the write when it matches what was last
  loaded from or written to storage.
- **Integrity envelope.** New saves are prefixed with `MTIO1:<sha256>`; corrupted primary, `.tmp` or `.bak`
  payloads are rejected before decryption and recovery falls through to the next candidate. Saves written
  by 1.x (no envelope) remain readable and are upgraded on the next save. The digest detects corruption; it
  is not tamper-proof authentication.
- **Interrupted writes.** A complete, validated `.tmp` is promoted (`RecoveredPending`). An incomplete
  `.tmp` with no primary/backup is treated as `Missing`, not `Unreadable`.
- **Strategy selection persists.** The serialization and encryption strategy fields are `[SerializeReference]`,
  so the concrete strategy picked in the inspector is saved with the scene.
- **Serialized mutations.** Save, recovery and delete share one gate (non-WebGL) or one queue (WebGL), so the
  same `.tmp`/`.bak` files are never mutated concurrently. A synchronous `Save(data)` that arrives during an
  async mutation captures its payload immediately and queues; `LastSaveTask` tracks it.
- **Initialization no longer depends on `Awake`.** Save/load and installer binding initialize strategies and
  paths lazily.
- **Path safety.** `filePath` must be relative; rooted paths and `..` traversal fall back to the root.
- **Platform roots.** In players, Android/iOS `DataPath`/`StreamingAssetsPath` resolve to `persistentDataPath`.
  WebGL file saves use the stable `/idbfs/<productName>` directory, or `/idbfs/<webStorageName>` when that
  field is set (see [WebGL persistence](#webgl-persistence)).
- **Encryption can be built in code.** Override `CreateEncryptionStrategy()` to key a strategy from a secret
  that never sits in a serialized asset; `new AesEncryptionStrategy(byte[] secret)` then accepts only payloads
  it sealed itself (see [Encryption strategies](#encryption-strategies)).
- **Default storage** for new components is `PlatformStorageStrategy` (files on Editor/Standalone/Mobile,
  `WebGlFileStorageStrategy` on WebGL). Existing serialized components keep their configured backend.
- An empty XOR key reports a configuration error instead of dividing by zero.

## Dependencies

| Dependency | Notes |
|---|---|
| `com.unity.nuget.newtonsoft-json` | Declared in `package.json`. `NewtonsoftJsonStrategy` references it directly. |
| Debug Utility, Editor Toolz | MyToolz packages, declared in `package.json`. |
| Zenject | External — install separately. |
| SerializeReferenceExtensions | External — install separately (provides `[SubclassSelector]`). |

## Recommended platform storage

With `SaveRoot.PersistentDataPath`:

| Target | Backend | Notes |
|---|---|---|
| Editor | `FileStorageStrategy` | Play Mode always uses the Editor slot; it does not emulate the build target. |
| Windows/macOS/Linux | `FileStorageStrategy` | `.tmp` write, then `File.Replace` with a rolling `.bak`. |
| Android/iOS | `FileStorageStrategy` | Save at checkpoints and on pause/focus loss; quit/destroy are not guaranteed. |
| WebGL | `WebGlFileStorageStrategy` | `/idbfs/<productName>` (or `<webStorageName>`); every mutation is followed by an awaited `FS.syncfs`. |
| WebGL (small saves) | `PlayerPrefsStorageStrategy` | ~1 MB store-wide limit; conservative budgets, backup skipped when too large. |

`DataPath` and `StreamingAssetsPath` are not portable writable locations in players. `TemporaryCachePath`
is writable but disposable.

## Serialization strategies

| Strategy | When to use |
|---|---|
| `NewtonsoftJsonStrategy` | Default. Collections, dictionaries, polymorphism. |
| `UnityJsonStrategy` | Only Unity-serializable shapes (no dictionaries; limited polymorphism). Logs a warning on every use. |

Custom strategy: subclass `SerializationStrategy<T>`, mark it `[Serializable]`; it appears in the dropdown.

## Encryption strategies

Every key a strategy uses ships inside the build, so no strategy makes a save secret from a determined
player. Pick by what you need:

| Strategy | What it gives you |
|---|---|
| `NoEncryptionStrategy` | Default. Plain text. |
| `XorEncryptionStrategy` | **Obfuscation only.** Unreadable at a glance; trivially reversible and does not detect edits. |
| `AesEncryptionStrategy` | AES-256-CBC with a **random IV per save** and an **HMAC-SHA256** tag (encrypt-then-MAC). Any edit to the stored bytes fails authentication and the load falls through to the backup. |

- AES saves are written as `MTAES2:<base64(iv | cipher | mac)>`. Saves written by IO 1.1.x (fixed IV, no MAC)
  are still readable while `acceptLegacy` is on (the default) and are upgraded by the next save. Those legacy
  saves carry no tag, so a player could hand-craft one; turn `acceptLegacy` off once no 1.1.x saves are left.
- The AES and HMAC keys are derived from the `key` field. The default key is public: `SaveLoadBase` logs a
  warning in the editor and development builds until you set a project-specific one. The `iv` field is only
  used to read legacy saves; keep it to read them.
- The `MTIO1` integrity envelope is a plain SHA-256 digest: it catches corruption (torn or truncated writes),
  not tampering. Tamper evidence comes from the AES strategy's MAC.
- Subclass `EncryptionStrategy` and mark it `[Serializable]` for your own; it appears in the dropdown.

### Strict, code-keyed AES

For saves the player must not be able to edit (currency, purchases), build the strategy in code instead of the
inspector, so the secret never sits in a serialized asset, and refuse everything that is not sealed:

```csharp
public class ProfileSaver : SaveLoadBase<PlayerProfile>
{
    protected override EncryptionStrategy CreateEncryptionStrategy() => new AesEncryptionStrategy(Secret());
}
```

- A non-null `CreateEncryptionStrategy()` result is used instead of the inspector's strategy. It is held in a
  non-serialized field, so it never ends up in the scene or prefab, not even from the edit-mode inspector buttons.
- `new AesEncryptionStrategy(byte[] secret)` needs 32+ bytes (copied) and reads **only** `MTAES2` payloads with a
  valid MAC: plain text, the 1.1.x format and saves sealed with another key all fail to load (status
  `Unreadable`). Never change the secret once players have saves.
- `AesEncryptionStrategy.IsSealed(payload)` tells whether a payload is in the sealed format.
- The secret still ships inside the build. This stops editing a save file by hand, not someone who extracts the
  key from the binary; anything that must be fully trusted has to be decided by a server.

## Write safety and recovery order

1. Primary — loaded as-is (`LoadedPrimary`).
2. Pending `.tmp` — if complete and valid it is promoted to primary (`RecoveredPending`).
3. Backup `.bak` — restored to primary without rotating the corrupt primary over it (`RecoveredBackup`).

`LastRecoveryTask` completes when the repair is persisted and faults when it fails. Deleting a save removes
primary, backup, temp and `.corrupt` copies.

## WebGL persistence

File saves live under `/idbfs/<productName>`. Set **Web Storage Name** (`webStorageName`) to pin the folder when
the product name may change: renaming the product would otherwise start every web player with an empty save.
Changing the field itself leaves existing web saves behind, so set it before the first web release or to the
old product name.

Unity stores WebGL files and PlayerPrefs in IndexedDB, so completion is asynchronous:

```csharp
bool saved = await asyncSaver.SaveAsync(data);
```

All WebGL mutations run through a single-threaded queue (no `SemaphoreSlim` in the player). The JavaScript
bridge waits for the `FS.syncfs` callback, retries only transient IndexedDB errors (at most three attempts,
15 s timeout each) and reports failure back to C#. After a timeout the commit state is unknown, so further
writes fail fast until the page is reloaded. Browsers can close a tab without waiting for pending work —
await `SaveAsync` at real checkpoints instead of relying on page close. Set `autoSyncPersistentDataPath: true`
in the WebGL template; the explicit flush remains the completion boundary.

## Cache and lifecycle saves

With **Use Cache**, `Load()`, `TryLoad()` and `Save(data)` establish the cached object that pause, focus-loss,
quit and destroy saves write. Without a cache, parameterless `Save()` does nothing and logs a warning — it
never writes an empty object over an existing save. Lifecycle hooks are safety nets, not checkpoints.

## Tests

`Assets/Tests/EditMode/IOPersistenceTests.cs`, `StorageStrategyTests.cs`, `EncryptionStrategyTests.cs`,
`SerializationStrategyTests.cs`.
