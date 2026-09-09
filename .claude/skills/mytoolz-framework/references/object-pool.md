# Object Pool

Package: `Assets/Packages/Object Pool` · Namespaces:
`MyToolz.DesignPatterns.ObjectPool` (installers, `IPoolable`) and `MyToolz.Events`
(the request events). Built on **Zenject `MemoryPool`** and driven through the **EventBus**.

Use it for anything spawned and destroyed frequently — projectiles, hit VFX, UI labels,
killfeed entries. Never `Instantiate`/`Destroy` those in a hot path.

## The two halves

### 1. The pool (scene setup, usually done once per prefab family)

Create a component that subclasses `DefaultObjectPoolInstaller<T>` (where `T : MonoBehaviour`
is your pooled type), put it on a GameObject in the scene, and fill its `poolObjects` list
in the Inspector. It's a `PrivateSingleton`, resolves the Zenject container, and binds a
`MemoryPool` per prefab.

```csharp
using MyToolz.DesignPatterns.ObjectPool;

// Enough for most cases — one installer component per pooled type.
public class ProjectilePool : DefaultObjectPoolInstaller<Projectile> { }
```

Each `PoolObject` entry has:

| Field            | Meaning                                             |
|------------------|-----------------------------------------------------|
| `Prefab`         | The prefab to pool (matched by instance id).        |
| `DefaultCapacity`| Pre-warmed instances.                               |
| `MaxCapacity`    | Upper bound used by the capacity mode.              |
| `CapacityMode`   | `SoftLock` / `HardLock` / `QueueLock` (see below).  |

Installer-level fields: `destroyIfNotInPool` (destroy released objects with no pool) and
`contextMode` — `Scene` (default, uses the scene's Zenject container) or `Project` (uses
`ProjectContext`, for pools that must persist across scenes).

**Capacity modes:**
- `SoftLock` — grow past `MaxCapacity` when needed (default, most forgiving).
- `HardLock` — refuse to spawn beyond `MaxCapacity` (`Get` returns null, logs a warning).
- `QueueLock` — at capacity, recycle the **oldest active** instance to serve the new request.

For custom spawn/create/despawn logic, subclass `ObjectPoolInstaller<T, P>` instead and
override `InitializePools` (see `DefaultObjectPoolInstaller` as the template).

### 2. Requests (runtime, from gameplay code)

Talk to the pool only through events — never call the installer directly.

```csharp
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;

// Spawn:
EventBus<PoolRequest<Projectile>>.Raise(new PoolRequest<Projectile>
{
    Prefab   = projectilePrefab,   // must be listed in a ProjectilePool installer
    Position = muzzle.position,
    Rotation = muzzle.rotation,
    Parent   = null,               // optional transform parent
    Callback = p =>                // runs after the object is spawned & placed
    {
        p.Initialize(init);
    },
});

// Release (commonly raised by the pooled object about itself):
EventBus<ReleaseRequest<Projectile>>.Raise(new ReleaseRequest<Projectile>
{
    PoolObject = this,
    Callback   = p => { /* optional post-release */ },
});

// Release everything of a type (e.g. round reset):
EventBus<PoolAllRequest<Projectile>>.Raise(new PoolAllRequest<Projectile> { Callback = null });
```

The event payloads (`MyToolz.Events`):

```csharp
struct PoolRequest<T>    : IEvent { public T Prefab; public Vector3 Position; public Quaternion Rotation; public Transform Parent; public Action<T> Callback; }
struct ReleaseRequest<T> : IEvent { public T PoolObject; public Action<T> Callback; }
struct PoolAllRequest<T> : IEvent { public Action<T> Callback; }
```

## IPoolable — reset state on reuse

Pooled instances are reused, not recreated, so reset per-spawn state. Implement `IPoolable`
on the prefab's component and the installer calls it automatically:

```csharp
public class Projectile : MonoBehaviour, IPoolable
{
    public void OnSpawned()   { /* reset velocity, trail, timers */ }
    public void OnDespawned() { /* stop effects, clear references */ }
}
```

## Common mistakes

- Raising `PoolRequest<T>` for a prefab **no installer lists** → logs "No pool found for
  prefab". Add it to a `poolObjects` entry.
- Forgetting `IPoolable` → recycled objects keep stale state (velocity, health, trails).
- Releasing an object twice → logs "Object already released" and no-ops.
- Using `HardLock` and not handling the null/refused case in your `Get` path.
