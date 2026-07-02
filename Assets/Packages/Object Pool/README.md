# Object Pool

Event-driven object pooling system using the EventBus for pool requests and releases. Pools are Zenject memory pools resolved from the `ProjectContext`; installers are singleton MonoBehaviours placed in the scene.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |
| Event Bus | `com.mytoolz.eventbus` |
| Singleton | `com.mytoolz.singleton` |

External: Zenject. Optional: Unity Addressables (for `AddressableObjectPoolInstaller`).

## Structure

```
Runtime/
├── Events.cs                          PoolRequest<T> and ReleaseRequest<T> event definitions
├── IPoolable.cs                       Optional callback interface for pooled components (OnSpawned / OnDespawned)
├── ObjectPoolInstaller.cs             Abstract base handling pool requests, instance tracking, and release validation
├── DefaultObjectPoolInstaller.cs      Concrete generic installer for direct prefab references
└── AddressableObjectPoolInstaller.cs  Concrete generic installer for Addressable prefabs
```

## Usage

Subclass `DefaultObjectPoolInstaller<T>` (or `AddressableObjectPoolInstaller<T>`) for each pooled type, add it to the scene, and configure its pool objects (prefab, default capacity, max capacity). Request and release objects through the EventBus:

```csharp
EventBus<PoolRequest<MyPrefab>>.Raise(new PoolRequest<MyPrefab>
{
    Prefab = prefab,
    Position = spawnPoint,
    Rotation = Quaternion.identity,
    Callback = obj => obj.Fire()
});

EventBus<ReleaseRequest<MyPrefab>>.Raise(new ReleaseRequest<MyPrefab> { PoolObject = obj });
```

Notes:

- Pools are bound with the configured initial and max capacity — the pool never grows past `MaxCapacity`.
- Pooled components may implement `IPoolable` to receive `OnSpawned` / `OnDespawned` callbacks.
- Releasing an object twice is detected and ignored with a warning. Releasing an object that doesn't belong to any pool destroys it when `destroyIfNotInPool` is enabled.
