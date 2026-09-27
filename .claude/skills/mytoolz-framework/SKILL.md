---
name: mytoolz-framework
description: >-
  How to write code that uses the MyToolz Unity framework (this repo) correctly and
  idiomatically. Use this WHENEVER you are adding or editing C# in this project and
  touch any MyToolz system — logging (DebugUtility), events (EventBus / IEventListener),
  object pooling (PoolRequest / ObjectPoolInstaller), state machines, singletons, saving
  and loading (SaveLoadBase / ISaver), MVP screens, or Zenject installers/bindings — even
  if the user does not name the system explicitly. Reach for it when you see DebugUtility,
  EventBus<...>, [Inject], MonoInstaller, Singleton<T>, SaveLoadBase, or an MVP
  Model/View/Presenter, or when the user asks how to log, fire an event, pool an object,
  save data, or add a new package. It keeps generated code consistent with house
  conventions instead of falling back to raw Unity idioms.
---

# Using the MyToolz framework

MyToolz is a Unity 2022.3 monorepo. Reusable packages live in `Assets/Packages/`
(each self-contained with its own `package.json`); game code lives in `Assets/Scripts/`
and uses **Mirror** for networking. Everything is under the `MyToolz` root namespace.

Your job when writing code here is to use these systems the way the codebase already
does, not to reintroduce raw Unity idioms (`Debug.Log`, `Instantiate`/`Destroy` for
frequently spawned objects, `FindObjectOfType`, hand-rolled singletons, manual C# events
across systems). This skill gives you the minimum-viable usage for each core system and
points to a per-system reference file when you need depth.

## Golden rules (apply to all generated code)

1. **Log through `DebugUtility`, never `UnityEngine.Debug`.** Call it with `this` as the
   first argument: `DebugUtility.Log(this, "msg")`. Passing `this` gives auto-tagging
   *and* per-type on/off gating. See [references/debug-utility.md](references/debug-utility.md).
2. **Cross-system communication goes through `EventBus<T>`**, not direct references or
   ad-hoc C# events. Subscribers implement `IEventListener`. See
   [references/event-bus.md](references/event-bus.md).
3. **Zenject is the only DI mechanism.** Bind in `MonoInstaller.InstallBindings()`,
   resolve with `[Inject] void Construct(...)`. No service locators, no `FindObjectOfType`.
   See [references/zenject-installers.md](references/zenject-installers.md). The only
   hand-rolled singletons allowed are the `Singleton<T>` bases, only where DI cannot reach —
   and default to `PrivateSingleton<T>`. **`PublicSingleton<T>` (a global static `Instance`)
   is a red flag and is prohibited unless the user explicitly asks for global static access.**
   See [references/singleton.md](references/singleton.md).
4. **Frequently spawned/destroyed objects (projectiles, labels, VFX) are pooled** via the
   Object Pool events, not `Instantiate`/`Destroy`. See
   [references/object-pool.md](references/object-pool.md).
5. **Inspector tooling attributes come from `MyToolz.EditorToolz`** (`[Button]`,
   `[FoldoutGroup]`, `[OnValueChanged]`, `[ReadOnly]`, `[ShowIf]`, `[HideIf]`, `[Required]`,
   `[SubclassSelector]`) — not Odin Inspector.
6. **New packages** go in `Assets/Packages/<Name>/` with a `Runtime/` (and optional
   `Editor/`) subfolder and a `package.json`, under a `MyToolz.*` namespace.

## Which system, and where to read more

| You need to…                                   | Use                                   | Namespace                                   | Reference |
|------------------------------------------------|---------------------------------------|---------------------------------------------|-----------|
| Log something                                  | `DebugUtility.Log(this, …)`           | `MyToolz.Utilities.Debug`                   | [debug-utility.md](references/debug-utility.md) |
| Let systems talk without hard references       | `EventBus<T>` + `IEventListener`      | `MyToolz.DesignPatterns.EventBus`, `MyToolz.Events` | [event-bus.md](references/event-bus.md) |
| Spawn/despawn many objects cheaply             | `PoolRequest<T>` / `ReleaseRequest<T>`| `MyToolz.DesignPatterns.ObjectPool`, `MyToolz.Events` | [object-pool.md](references/object-pool.md) |
| Model exclusive behaviour states              | `IState` / `SimplePriorityStateMachine`| `MyToolz.DesignPatterns.StateMachine`       | [state-machine.md](references/state-machine.md) |
| Enforce a single instance, DI unavailable      | `PrivateSingleton<T>` (not `PublicSingleton<T>`) | `MyToolz.DesignPatterns.Singleton`  | [singleton.md](references/singleton.md) |
| Save/load data to disk                         | `SaveLoadBase<T>` / `ISaver<T>`       | `MyToolz.IO`                                | [io.md](references/io.md) |
| A screen/UI feature                            | MVP (Model + View + Presenter + Installer) | `MyToolz.DesignPatterns.MVP.*` + per-package | [mvp.md](references/mvp.md) |
| Manage screens as a layered stack              | `UIScreen` / `UILayerStateManager`    | `MyToolz.UI.Management`                      | [ui-management-system.md](references/ui-management-system.md) |
| Handle player input / input modes             | `InputCommandSO` / `InputModeSO` / `InputStateManager` | `MyToolz.InputManagement`      | [input.md](references/input.md) |
| Play a sound effect or music                   | `PlayAudioClipSO` / `PlaySong` events | `MyToolz.Audio`                             | [audio.md](references/audio.md) |
| Localize player-facing text                    | `LocalizationText` / `LocalizationBindingSO` | `MyToolz.Localization`               | [localization.md](references/localization.md) |
| Animate UI (show/hide, hover, click)           | `UITweener` + tween strategies        | `MyToolz.Tweener.UI`, `MyToolz.Tweener`     | [ui-tweener.md](references/ui-tweener.md), [tweener.md](references/tweener.md) |
| Wire dependencies / write an installer         | Zenject `MonoInstaller` + `[Inject]`  | `Zenject`                                   | [zenject-installers.md](references/zenject-installers.md) |

## Minimum-viable usage per core system

These snippets are enough for the common case. Open the linked reference file for
configuration options, gotchas, and platform notes.

### DebugUtility

```csharp
using MyToolz.Utilities.Debug;

DebugUtility.Log(this, "Player spawned");
DebugUtility.LogWarning(this, "Pool nearly full");
DebugUtility.LogError(this, $"Failed to load: {e.Message}");
```

`this` context enables per-type gating (toggle a type/namespace on or off in the
`LogGateSettings` asset) — prefer it over the context-less `DebugUtility.Log("msg")`
overload, which always logs and cannot be gated.

### EventBus

```csharp
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;

// 1. Define the event (structs are the house style).
public struct PlayerDied : IEvent { public int PlayerId; }

// 2. Raise it from anywhere.
EventBus<PlayerDied>.Raise(new PlayerDied { PlayerId = id });

// 3. Listen for it: hold the binding, register/deregister in pairs.
public class Scoreboard : IEventListener
{
    EventBinding<PlayerDied> _died;

    public void RegisterEvents()
    {
        _died = new EventBinding<PlayerDied>(OnPlayerDied);
        EventBus<PlayerDied>.Register(_died);
    }

    public void UnregisterEvents() => EventBus<PlayerDied>.Deregister(_died);

    void OnPlayerDied(PlayerDied e) { /* … */ }
}
```

Call `RegisterEvents()`/`UnregisterEvents()` from `OnEnable`/`OnDisable` (MonoBehaviours)
or the owner's lifecycle. Buses auto-clear when exiting Play mode, so you won't leak
bindings across editor sessions.

### Object Pool

```csharp
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;

// Spawn (a DefaultObjectPoolInstaller<Projectile> in the scene must list this prefab):
EventBus<PoolRequest<Projectile>>.Raise(new PoolRequest<Projectile>
{
    Prefab = projectilePrefab,
    Position = muzzle.position,
    Rotation = muzzle.rotation,
    Callback = p => p.Initialize(init),   // runs after spawn
});

// Return to the pool (usually from the pooled object itself):
EventBus<ReleaseRequest<Projectile>>.Raise(new ReleaseRequest<Projectile>
{
    PoolObject = this,
});
```

Pooled prefabs should implement `IPoolable` (`OnSpawned`/`OnDespawned`) to reset state.

### State Machine

```csharp
using MyToolz.DesignPatterns.StateMachine;

public class ChaseState : IState
{
    public void OnEnter() { /* start chasing */ }
    public void OnExit()  { /* stop chasing */ }
}
```

For behaviour components that auto-select by priority, extend `SimplePriorityStateMachine`
and give each child state an `IPriorityState.Priority`. See the reference for the priority
strategies and the canonical `InputStateManager` example.

### Singleton

```csharp
using MyToolz.DesignPatterns.Singleton;

// Default to PrivateSingleton: enforces one instance WITHOUT a public static accessor.
public class ProjectilePool : PrivateSingleton<ProjectilePool>
{
    // Override OnSingletonAwake — NOT Awake. A subclass Awake shadows the base
    // guard and silently breaks the singleton. Same for OnSingletonDestroy vs OnDestroy.
    protected override void OnSingletonAwake() { /* init */ }
}
```

Prefer Zenject injection; reach for a singleton only when DI is genuinely unavailable.
`PublicSingleton<T>` exposes a global static `Instance` — that's a red flag and is
**prohibited unless the user explicitly asks for it**; use DI, an EventBus event, or
`PrivateSingleton<T>` instead.

### MVP

Any screen or stateful feature uses Model (plain C#, fires `OnChanged`) + View
(`IReadOnlyView<T>` / `ViewBase<T>`) + Presenter (`PresenterBase<TModel,TView>` for pure
logic, or a MonoBehaviour with `[Inject] Construct(model, view)`) + a `MonoInstaller` that
binds them. Full worked example and both presenter styles in
[references/mvp.md](references/mvp.md).

### Zenject installer

```csharp
using Zenject;

public class ScoreInstaller : MonoInstaller
{
    [SerializeReference] private ScoreModel model = new();
    [SerializeField]     private ScoreView view;

    public override void InstallBindings()
    {
        Container.Bind<ScoreModel>().FromInstance(model).AsSingle();
        Container.Bind<ScoreView>().FromInstance(view).AsSingle();
    }
}

// Consumers resolve via method injection (the house convention for MonoBehaviours):
[Inject] void Construct(ScoreModel model, ScoreView view) { /* … */ }
```

Bind interfaces, not concretes; details and binding idioms in
[references/zenject-installers.md](references/zenject-installers.md).

### IO (Save / Load)

```csharp
using MyToolz.IO;

[System.Serializable]
public class Loadout { public List<string> Weapons = new(); }

// A saver is a MonoInstaller — subclass it, drop it on a context, configure in Inspector.
public class LoadoutSaver : SaveLoadBase<Loadout> { }

// Consumers inject the abstraction, never the concrete saver:
[Inject] void Construct(ISaver<Loadout> saver) { _saver = saver; }
_saver.Save(loadout);
Loadout current = _saver.Load();   // never null — returns new Loadout() if no file
```

Default strategy is Newtonsoft JSON with atomic writes and a `.bak` fallback.
Newtonsoft.Json must be present or the project won't compile.

## Common pitfalls to avoid

- Using `Debug.Log` / `Instantiate` / `Destroy` / `FindObjectOfType` / `static Instance`
  fields you wrote by hand — all have a MyToolz replacement above.
- Reaching for `PublicSingleton<T>` (global static `Instance`) — prohibited unless the user
  explicitly asks; use DI, an EventBus event, or `PrivateSingleton<T>`.
- Registering an event binding without deregistering it (pair them).
- Overriding `Awake`/`OnDestroy` on a `Singleton<T>` subclass instead of
  `OnSingletonAwake`/`OnSingletonDestroy`.
- Raising a `PoolRequest<T>` for a prefab no installer lists — it logs "No pool found".
- Marking a save data type without `class, new()` — `SaveLoadBase<T>` requires it.
- Putting game logic in an MVP View, or injecting a concrete type where an interface exists.
- Adding Odin attributes; use `MyToolz.EditorToolz` equivalents.

When a system isn't covered here (other MVP feature packages, SceneManagement, AStar,
etc.), read an existing package under `Assets/Packages/` and mirror its structure — the
conventions above hold throughout. For a full inventory of every package and its
intended use, see the companion `mytoolz-catalog` skill.
