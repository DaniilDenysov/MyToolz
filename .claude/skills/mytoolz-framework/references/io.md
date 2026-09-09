# IO (Save / Load)

Package: `Assets/Packages/IO` · Namespace: `MyToolz.IO`

Persistent save/load built on three swappable strategies: **serialization** (how data
becomes text), **encryption** (optional transform of that text), and **storage** (where the
bytes actually go). Writes are crash-safe. **Requires `com.unity.nuget.newtonsoft-json`** —
without it the package (and the project) won't compile.

## The abstraction

```csharp
public interface ISaver<T>
{
    void Save();      // saves the cached instance (only if useCache && cache != null)
    void Save(T obj); // saves the given instance
    T Load();         // loads, or returns new T() if nothing is stored (never null)
}
```

`SaveLoadBase<T> : MonoInstaller, ISaver<T> where T : class, new()` — the base you extend.
Because it's a `MonoInstaller`, adding it to a scene / GameObjectContext both configures the
saver in the Inspector **and** binds it into Zenject.

## Setting up a saver

```csharp
using MyToolz.IO;

[System.Serializable]
public class Loadout          // must be `class` with a public parameterless ctor
{
    public List<string> Weapons = new();
    public int Credits;
}

// Often this is all you need — the base does the work.
public class LoadoutSaver : SaveLoadBase<Loadout> { }
```

Then drop `LoadoutSaver` on a context object. Its `InstallBindings()` binds
`ISaver<Loadout>` to itself as a singleton.

## Consuming it (inject the interface)

```csharp
using MyToolz.IO;
using Zenject;

public class LoadoutService
{
    ISaver<Loadout> _saver;

    [Inject]
    void Construct(ISaver<Loadout> saver) => _saver = saver;

    public void Persist(Loadout l) => _saver.Save(l);
    public Loadout Current() => _saver.Load();   // returns new Loadout() if no file yet
}
```

Inject `ISaver<T>`, never the concrete `LoadoutSaver`.

## Inspector configuration (`Persistance Settings` foldout)

| Setting               | Notes |
|-----------------------|-------|
| `root`                | `PersistentDataPath` (default), `DataPath`, `StreamingAssetsPath`, `TemporaryCachePath`. |
| `filePath`            | Subfolder under the root (created if missing). Default `Saves`. |
| `fileName`            | Name without extension (invalid chars stripped). Extension comes from the strategy. |
| `useCache`            | Keep an in-memory copy; enables parameterless `Save()` and auto-save. |
| `serializationStrategy` (`[SubclassSelector]`) | Default `NewtonsoftJsonStrategy<T>`. |
| `encryptionStrategy`  (`[SubclassSelector]`) | Default `NoEncryptionStrategy`. |
| `storageStrategy`     (`[SubclassSelector]`) | Default `FileStorageStrategy`. |

Inspector `[Button]`s: Open Folder, Reveal File, Copy Path, Delete File.

## Strategies

**Serialization** (`SerializationStrategy<T>`, `[Serializable]`):
- `NewtonsoftJsonStrategy<T>` — default; supports collections, dictionaries, private fields.
  **Use this unless you have a specific reason not to.**
- `UnityJsonStrategy<T>` — `JsonUtility`-based; no collections/dicts/non-public fields (it
  warns at runtime). Avoid for anything non-trivial.
- Custom: subclass `SerializationStrategy<T>`, mark `[Serializable]`, implement
  `FileExtension` / `Serialize` / `Deserialize`.

**Storage** (`StorageStrategy`, `[Serializable]`) — abstracts *where* the bytes land:
- `FileStorageStrategy` — default. Atomic temp-file → swap with a rolling `.bak`; correct on
  desktop and mobile.
- `PlayerPrefsStorageStrategy` — WebGL-safe, simplest for small saves.
- `WebGlFileStorageStrategy` — file save that flushes Unity's IndexedDB after each write, for
  larger WebGL saves; behaves like `FileStorageStrategy` off WebGL.
- `PlatformStorageStrategy` — routes to a different backend per platform (editor / standalone
  / mobile / web / fallback) so one component is correct everywhere.
- Custom: subclass `StorageStrategy`.

**Encryption** (`EncryptionStrategy`, `[Serializable]`):
- `NoEncryptionStrategy` (default), `XorEncryptionStrategy`, `AesEncryptionStrategy`.

## Reliability & lifecycle

- `FileStorageStrategy` writes to `.tmp`, then atomically replaces the real file keeping the
  previous version as `.bak`. `Load()` falls back to the `.bak` if the primary is missing or
  corrupt.
- `Load()` never returns null — it returns `new T()` when nothing is stored.
- Auto-save: the base subscribes to `Application.quitting`, saves on `OnApplicationPause`, and
  saves on `OnDestroy` if it hasn't already this session. Parameterless `Save()` only writes
  when `useCache` is on and a cache exists (guards against overwriting a good file with empty
  data).

## Platform note

On **WebGL** a plain file write lives only in memory and is lost on refresh. Use
`PlayerPrefsStorageStrategy` (small saves) or `WebGlFileStorageStrategy` (larger saves), or
`PlatformStorageStrategy` to pick automatically.

## Do / Don't

- ✅ Data type is `class` + parameterless ctor; initialize collections inline.
- ✅ Inject `ISaver<T>`; keep `NewtonsoftJsonStrategy` unless you need otherwise.
- ✅ For WebGL builds, choose a WebGL-safe storage strategy.
- ❌ Don't hand-roll `File.WriteAllText` for saves — you lose atomicity, backup, and platform routing.
- ❌ Don't inject or reference the concrete saver subclass.
