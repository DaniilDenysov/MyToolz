# SaveLoadBase — Setup & Usage

## Android and WebGL compatibility fixes

- Plain `FileStorageStrategy` now uses the same queued, awaited IndexedDB flush as `WebGlFileStorageStrategy` in WebGL players. Existing serialized file backends therefore receive the fix without scene edits. Direct storage callers must use `WriteAsync`, `DeleteAsync`, and `RestorePrimaryAsync` on WebGL; `SaveLoadBase.Save(data)` handles this automatically.
- Android/iOS `DataPath` and `StreamingAssetsPath` save roots resolve to `persistentDataPath` in players because packaged assets are not writable save directories. WebGL file saves always resolve to the stable `/idbfs/<productName>` directory, including when a temporary root was configured, so a hosting platform can replace the build URL without changing the save location. Editor paths are unchanged. This does not import bundled seed data or migrate saves from Unity's older URL-hashed `persistentDataPath` directory.
- Save/load and installer binding initialize their strategies and paths without depending on `Awake` ordering.
- Encryption selection uses managed-reference serialization so the selected concrete strategy and key settings can survive scene serialization. Existing saves retain their format; keep the same encryption key when loading encrypted data.
- A failed `TryLoad` clears the previous cache, null `Save(data)` reports failure through `LastSaveTask`, and revealing a missing save in the Editor no longer creates an empty save file.
- An empty XOR key reports a configuration error instead of a division-by-zero failure.

Regression coverage: `Assets/Tests/EditMode/IOPersistenceTests.cs`. Run `FindMe.Tests.EditMode.IOPersistenceTests` in the Unity Test Runner. Platform acceptance checks: on Android, save twice, background/resume, then force-stop/relaunch and check the latest value; on WebGL, await `SaveAsync`, reload the page, deploy a newer build at the same origin, and check the latest value. Repeat with a serialized plain `FileStorageStrategy`, and verify an IndexedDB failure does not report successful persistence.

## v1.2.0 Encryption hardening

Every key a strategy uses ships inside the build, so no strategy makes a save secret from a determined
player. Pick by what you need:

| Strategy | What it gives you |
|---|---|
| `NoEncryptionStrategy` | Plain text. |
| `XorEncryptionStrategy` | **Obfuscation only.** Unreadable at a glance; trivially reversible and does not detect edits. |
| `AesEncryptionStrategy` | AES-256-CBC with a **random IV per save** and an **HMAC-SHA256** tag (encrypt-then-MAC). Any edit to the stored bytes fails authentication and the load falls through to the backup. |

- AES saves are written as `MTAES2:<base64(iv | cipher | mac)>`. Saves written by 1.1.x (fixed IV, no MAC)
  are still readable and are upgraded by the next save. Because those legacy saves carry no tag, a player
  could still hand-craft one; the MAC protects every save written from 1.2.0 on.
- The AES and HMAC keys are derived from the `key` field. The default key is public: `SaveLoadBase` logs a
  warning in the editor and development builds until you set a project-specific one. The `iv` field is now
  only used to read legacy saves.
- The `MTIO1` integrity envelope below is a plain SHA-256 digest: it catches corruption (torn or truncated
  writes), not tampering. Tamper evidence comes from the AES strategy's MAC.

## v1.1.3 Reliability Fixes

- Non-WebGL save/recovery/delete mutations now share one gate. A synchronous `Save(data)` queues rather than racing an already-running async mutation.
- `LastRecoveryTask` now faults when temp/backup-to-primary repair persistence fails instead of logging and completing successfully.
- Real WebGL file storage no longer creates `.corrupt` diagnostic copies outside the `WebGlFileSync` queue.
- New saves carry a backward-compatible SHA-256 integrity envelope; v1.1.2 and older saves remain readable.

## Required Dependencies

**Newtonsoft.Json for Unity** is declared as a package dependency:

```json
"com.unity.nuget.newtonsoft-json": "3.2.1"
```

## Internal Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |
| Editor Toolz | `com.mytoolz.editortoolz` |

---

## Recommended Platform Storage

With `SaveRoot.PersistentDataPath`:

| Target | Recommended backend | Notes |
|---|---|---|
| Editor | `FileStorageStrategy` | Editor Play Mode always uses the Editor slot. It does not emulate the selected build target's storage slot. |
| Windows/macOS/Linux | `FileStorageStrategy` | Uses a normal persistent file with `.tmp` and rolling `.bak`. |
| Android/iOS | `FileStorageStrategy` | Save at progress checkpoints and on pause/focus loss; do not rely only on quit/destruction. |
| WebGL | `WebGlFileStorageStrategy` | Default WebGL backend. Uses `/idbfs/<productName>` and follows VFS mutation with an awaited `FS.syncfs` result. |
| WebGL (small saves) | `PlayerPrefsStorageStrategy` | Uses Unity's PlayerPrefs location and async IndexedDB flush. Keep data small; unlike the file backend, its location can still follow Unity's URL-derived path on hosts that replace build URLs. |
| Fallback | `PlayerPrefsStorageStrategy` | Appropriate only for relatively small key/value payloads; keeps a rolling `.bak` key. |

`DataPath` and `StreamingAssetsPath` are not portable writable save locations in player builds. `TemporaryCachePath` is writable but disposable.

The source default for the WebGL slot is `WebGlFileStorageStrategy`. Existing serialized inspector values are not changed automatically; both file strategy types now persist through IndexedDB on WebGL.

## Serialization Strategies

| Strategy | When to use |
|---|---|
| `NewtonsoftJsonStrategy` | Default for general JSON saves. |
| `UnityJsonStrategy` | Only for Unity-serializable field shapes. Dictionaries are unsupported and polymorphic managed data has Unity serializer limitations. Public fields and private fields marked `[SerializeField]` can be serialized. |

To add a custom strategy, subclass `SerializationStrategy<T>` and mark it `[Serializable]`.

## Write Safety

File saves are written to a `.tmp` file first. When supported, `File.Replace` swaps the temp file into place and rolls the old primary into `.bak`. On platforms/filesystems where `File.Replace` fails with an expected compatibility or I/O exception, the implementation falls back to a recoverable copy sequence that first preserves the previous primary as `.bak`.

A fully written `.tmp` payload is also treated as a recovery candidate. This covers a crash after the temp write but before the first `File.Move`/replace commit; the temp is integrity-checked, decrypted, and deserialized before promotion. A validated temp or backup is restored into the primary slot without rotating a corrupt primary over the known-good backup. On non-WebGL file backends, corruption can also be copied to `.corrupt` for diagnosis. WebGL intentionally leaves the unreadable source in place instead of creating a diagnostic copy outside the IndexedDB operation queue. Deleting a save removes primary, backup, temp, and diagnostic corrupt copies; an orphaned backup is deleted even if the primary is absent.

New saves use a versioned `MTIO1` integrity envelope containing a SHA-256 digest of the stored encrypted/serialized payload. The digest is verified before decrypt/deserialization, so accidental byte corruption in primary, `.tmp`, or `.bak` is rejected and recovery can fall through to another candidate. Saves produced by v1.1.2 and earlier have no envelope and remain readable; the next successful save upgrades them automatically. The digest is for corruption detection, not tamper-proof authentication.

On non-WebGL platforms, save, recovery, and delete mutations share one `SemaphoreSlim` gate. `Save(data)` remains synchronous when the slot is idle; if an async mutation is already in progress, it captures the payload immediately and queues it instead of blocking Unity's main thread or racing the same `.tmp`/`.bak` files. `LastSaveTask` represents that queued operation.

## WebGL Persistence

WebGL persistence is asynchronous because Unity stores persistent files (including PlayerPrefs) in browser IndexedDB. `WebGlFileStorageStrategy` and WebGL `PlayerPrefsStorageStrategy` therefore require the async persistence path.

File strategies use `/idbfs/<productName>` rather than `Application.persistentDataPath` in WebGL players. The directory is stable across build uploads as long as the product name and website origin remain unchanged. The WebGL template should also set `autoSyncPersistentDataPath: true`; the explicit IO flush remains the completion boundary used by `SaveAsync`.

```csharp
bool saved = await asyncSaver.SaveAsync(data);
```

`Save(data)` remains source-compatible and starts the tracked async operation, but it cannot make Unity lifecycle callbacks wait. `LastSaveTask` exposes the most recently started operation when code has access to the concrete `SaveLoadBase<T>` instance. Code that needs a definite commit point must await `SaveAsync(data)` at a real gameplay checkpoint while the page is alive.

All authoritative WebGL save/delete/restore mutations are serialized through a single-threaded task queue; no `SemaphoreSlim` is used in a WebGL player. Diagnostic corrupt-copy creation is deliberately disabled in a real WebGL player so it cannot mutate IDBFS outside that queue. The JavaScript bridge:

- waits for the `FS.syncfs` callback before reporting success,
- retries only errors classified as plausibly transient (`AbortError`, `UnknownError`, `InvalidStateError`, `TransactionInactiveError`, `NetworkError`),
- performs at most three attempts,
- times out an attempt after 15 seconds,
- does not retry quota/security failures,
- reports terminal failure/timeout back to C#.

A timeout means the browser did not confirm whether the outstanding `syncfs` committed and that operation cannot be cancelled. To avoid starting a potentially overlapping persistence cycle, further WebGL writes are failed fast for that session and the page must be reloaded.

A browser/tab/process can still be terminated without waiting for an outstanding asynchronous operation. No implementation can make `beforeunload` reliably block on IndexedDB. Save important progress early and await `SaveAsync` rather than relying on page close, quit, or destruction.

### PlayerPrefs on WebGL

The WebGL PlayerPrefs limit is store-wide and Unity does not expose an enumeration/total-size API. The strategy therefore cannot prove remaining capacity. It uses conservative headroom warnings/budgets, skips a rolling backup when the primary+backup pair would consume too much of the store, and surfaces `PlayerPrefsException` as a save failure. For anything beyond small state, prefer `WebGlFileStorageStrategy`.

## Load Recovery State

`Load()` remains backward-compatible: when no readable save is available it returns `new T()`. The result is no longer ambiguous through the status API:

```csharp
var data = saver.Load();
switch (saver.LastLoadStatus)
{
    case SaveLoadBase<MyData>.LoadStatus.Missing:
        // genuine first run / no save
        break;
    case SaveLoadBase<MyData>.LoadStatus.Unreadable:
        // a primary or backup existed/read failed but could not be decoded
        break;
    case SaveLoadBase<MyData>.LoadStatus.RecoveredPending:
        // a completed .tmp payload was validated and promoted
        break;
    case SaveLoadBase<MyData>.LoadStatus.RecoveredBackup:
        // backup was loaded and primary repair was requested
        break;
}
```

`TryLoad(out data, out status)` is available when callers want a non-ambiguous success result. It returns `true` only when the primary, pending temp, or backup actually decoded. `LastRecoveryTask` exposes any queued temp/backup-to-primary repair; it completes only when repair persistence succeeds and faults if the repair fails. Loading recovered data and successfully repairing the primary are therefore separately observable.

If `Load()` has to return a blank `new T()` because the stored data is unreadable, cache-based lifecycle autosaves are blocked. This prevents pause/quit/destruction from immediately overwriting the corrupt/recoverable evidence with defaults. The game must explicitly call `Save(data)` or `SaveAsync(data)` after it has intentionally chosen reset/recovery state.

## Cache and Lifecycle Saves

With **Use Cache** enabled, both `Load()` and `Save(data)` establish the cached state object. Later parameterless lifecycle saves use that same object.

Parameterless `Save()` does not create an empty object when no cache exists. With **Use Cache** disabled it intentionally does nothing and logs a warning. Lifecycle hooks are safety nets, not a substitute for explicit checkpoint saves.

## Path Safety

`filePath` is treated as a relative path inside the selected root. Rooted paths and `..` traversal are rejected, path segments are sanitized, and the resolved path is checked to remain under the selected root. `fileName` is sanitized before the serialization extension is appended.
