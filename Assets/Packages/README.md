# MyToolz Framework Documentation

MyToolz is a modular Unity framework distributed as a collection of UPM (Unity Package Manager) packages under the `com.mytoolz.*` namespace. Each package is self-contained with explicit dependencies, follows consistent conventions, and targets Unity 2022.3+. The framework provides design pattern implementations, MVP-based UI systems, input management, audio, pathfinding, editor extensions, and game-ready subsystems that compose together through a shared EventBus and dependency injection layer.

---

## Architecture

### Design Principles

The framework is built around three core ideas. First, every package declares its dependencies in `package.json` and never references packages outside that list. Second, cross-layer and cross-system communication flows through the EventBus rather than direct method calls, keeping systems decoupled. Third, runtime configuration is driven by ScriptableObjects, making behavior tweakable without code changes or scene edits.

### Dependency Injection

Systems that require wiring (presenters, pools, state managers) use Zenject. Each package that needs DI provides a MonoInstaller subclass. Add these installers to your `SceneContext` in the order specified by each package's documentation.

### Package Categories

The packages fall into four groups:

**Foundation** packages have zero or minimal internal dependencies and provide primitives used across the framework: Adapter, Prototype, Singleton, Debug Utility, Editor Toolz, Extensions, Event Bus, Command Pipeline, Tweener, and IO.

**Infrastructure** packages build on the foundation to provide system-level services: Object Pool, State Machine, Input Management, Input Commands, Input Command Pipeline, Auto Logger, Audio, Animations, UI Tweener, and Free Camera.

**MVP Core** provides the Model-View-Presenter pattern implementation and the UI Management System that all game-facing MVP packages extend.

**MVP Game Systems** are ready-to-use game subsystems: MVP Clock, MVP Game Settings, MVP Health System, MVP Inventory System, MVP Loading Screen, MVP Notifications, and Tooltip System.

### Dependency Graph

```
Adapter ─────────────────────────────────────────────────────── (no deps)
Prototype ───────────────────────────────────────────────────── (no deps)
Singleton ───────────────────────────────────────────────────── (no deps)
Command Pipeline ────────────────────────────────────────────── (no deps)

Debug Utility ───────────────────────────────────────────────── (no deps)
  ├── Editor Toolz
  ├── Extensions ─────────────── (SceneExtensions, general utils)
  ├── Event Bus
  ├── Tweener ────────────────── (DOTween)
  ├── Auto Logger ────────────── (UniTask)
  ├── Input Management ───────── (Unity Input System)
  │
  ├── Object Pool ────────────── Event Bus, Zenject, [Addressables]
  ├── State Machine ──────────── Editor Toolz, Zenject, [UniTask]
  ├── Animations ─────────────── State Machine, Editor Toolz
  ├── IO ─────────────────────── Editor Toolz, Newtonsoft.Json
  │
  ├── Input Commands ─────────── Event Bus, Command Pipeline, Unity Input System
  │   └── Input Command Pipeline ── Command Pipeline, Input Commands, Zenject
  │
  ├── Audio ──────────────────── Editor Toolz, DOTween, UniTask, [Event Bus, Object Pool]
  ├── UI Tweener ─────────────── Editor Toolz, Extensions, Tweener, DOTween
  │
  ├── MVP ────────────────────── (standalone)
  ├── MVP UI Management System ─ Editor Toolz, Input Management, UI Tweener, Zenject
  │   ├── MVP Clock ──────────── MVP, Prototype, UniTask, TextMeshPro
  │   ├── MVP Game Settings ──── Extensions, IO, Zenject, TextMeshPro
  │   ├── MVP Health System ──── Event Bus, DOTween, Zenject
  │   ├── MVP Inventory System ─ Event Bus, Object Pool, IO, Zenject, TextMeshPro
  │   ├── MVP Loading Screen ─── Event Bus, Singleton, Extensions, MVP, UniTask, Zenject
  │   └── MVP Notifications ──── MVP, Event Bus, Object Pool, Editor Toolz, DOTween, TextMeshPro
  │
  ├── Free Camera ────────────── Event Bus, Input Commands, Unity Input System
  ├── Tooltip System ─────────── Event Bus, Singleton, Editor Toolz, Unity Input System, TextMeshPro
  └── A* Pathfinder ──────────── (no internal deps, engine-agnostic)
```

### External Dependencies

| Dependency | Required by |
|---|---|
| Zenject | Object Pool, State Machine, Input Command Pipeline, MVP UI Management System, MVP Game Settings, MVP Health System, MVP Inventory System, MVP Loading Screen |
| DOTween | Tweener, UI Tweener, Audio, MVP Health System, MVP Notifications |
| UniTask | Auto Logger, MVP Clock, MVP Loading Screen, State Machine (multi-thread), Audio |
| Unity Input System | Input Management, Input Commands, Input Command Pipeline, Free Camera, Tooltip System |
| TextMeshPro | MVP Clock, MVP Game Settings, MVP Inventory System, MVP Notifications |
| Newtonsoft.Json | IO |
| Unity Addressables | Object Pool (optional, AddressableObjectPoolInstaller) |

---

## Foundation Packages

### Adapter

Lightweight adapter pattern implementation. Implement `IAdapter<T>` on a MonoBehaviour or ScriptableObject to expose a non-serializable reference through an inspector-friendly wrapper, bridging Unity's serialization system and types that cannot be directly serialized.

### Prototype

Clone pattern interface. Implement `IPrototype<T>` on any class that needs to produce independent deep copies of itself. Used by MVP Clock and other systems that require snapshot/restore behavior.

### Singleton

Generic `Singleton<T> : MonoBehaviour` base class. Inspector fields control `DontDestroyOnLoad` persistence and whether duplicate GameObjects are destroyed entirely or just the component.

```csharp
public class GameManager : Singleton<GameManager>
{
    public override void Awake()
    {
        base.Awake();
    }
}
```

### Debug Utility

Structured logging framework with namespace-based tagging, log styling, and editor gating. Replace `Debug.Log` calls with `DebugUtility.Log(this, "message")` to gain per-namespace enable/disable through `LogGateSettingsSO` assets. The editor provides a Logging Hierarchy window for managing log gates. Virtually every other package depends on this.

### Editor Toolz

Custom inspector attributes replicating a subset of Odin Inspector functionality:

| Attribute | Purpose |
|---|---|
| `[ReadOnly]` | Non-editable field |
| `[Button]` | Method as clickable button |
| `[ShowHideIf]` | Conditional visibility |
| `[FoldoutGroup]` | Collapsible grouping |
| `[OnValueChanged]` | Inspector change callback |
| `[Required]` | Reference validation |
| `[ShowInInspector]` | Expose non-serialized properties |

### Extensions

Shared utility extension methods for common Unity and C# types plus scene management utilities (`SceneExtensions.IsSceneValid`, `IsSceneInBuildSettings`, `GetSceneNameByBuildIndex`).

### Event Bus

Generic static event bus (`EventBus<T>`) with strongly-typed bindings and automatic assembly scanning. All cross-system communication in the framework flows through the bus.

```csharp
public struct PlayerDiedEvent { public int PlayerId; }

var binding = new EventBinding<PlayerDiedEvent>(OnPlayerDied);
EventBus<PlayerDiedEvent>.Register(binding);
EventBus<PlayerDiedEvent>.Raise(new PlayerDiedEvent { PlayerId = 1 });
EventBus<PlayerDiedEvent>.Deregister(binding);
```

Implement `IEventListener` on MonoBehaviours: register in `OnEnable`, deregister in `OnDisable`/`OnDestroy`.

### Command Pipeline

Command pattern implementation with `ICommand` (Execute/Undo) and `ICommandPipeline` for sequential command execution with undo support.

### Tweener

Base tweening abstraction layer wrapping DOTween sequences with lifecycle management. Extended by UI Tweener.

### IO

File-based save/load system with pluggable serialization strategies. Ships with `NewtonsoftJsonStrategy` (default, full-featured) and `UnityJsonStrategy` (fallback). Subclass `SaveLoadBase<T>` and select a strategy in the inspector. Custom strategies can be added by subclassing `SerializationStrategy<T>`.

Requires `com.unity.nuget.newtonsoft-json` in your manifest.

---

## Infrastructure Packages

### Object Pool

Event-driven object pooling via EventBus and Zenject. Supports both direct prefab references (`DefaultObjectPoolInstaller<T>`) and Addressable assets (`AddressableObjectPoolInstaller<T>`). Pooled objects can implement `IPoolable` for spawn/despawn callbacks.

```csharp
EventBus<PoolRequest<Bullet>>.Raise(new PoolRequest<Bullet>
{
    Prefab = bulletPrefab,
    Position = spawnPoint,
    Rotation = Quaternion.identity,
    Callback = bullet => bullet.Fire()
});
```

### State Machine

Three variants of the state machine pattern, all driven by `IState` and `IStateMachine`:

| Variant | Class | Notes |
|---|---|---|
| Simple Priority | `SimplePriorityStateMachine` | Lightweight, no DI |
| Full Priority | `PriorityStateMachine` | Zenject-integrated |
| Multi-threaded Priority | `PriorityStateMachine_MultiThread` | Thread-safe via UniTask |

### Animations

Priority-based animator state machine extending `PriorityStateMachine`. `AnimatorState` maps serialized `AnimationClip` references to hash IDs with randomization support. `AnimatorStateMachine<T>` drives an `Animator` through priority-evaluated crossfade transitions, handling loop detection, interruption, and completion checks.

### Input Management

Mode-based input state system. `InputModeSO` ScriptableObjects define which Input System actions are enabled and cursor behavior per mode. `InputStateManager` switches between modes. `InputDeviceTracker` detects keyboard/gamepad changes.

### Input Commands

ScriptableObject-based input command definitions bridging Unity's Input System with the Command Pipeline. `InputCommandSO` assets represent input-bound commands. `InputPhase` enum tracks lifecycle (Pressed, Released, Canceled, Performed, Started).

### Input Command Pipeline

Bridges Input Commands with the Command Pipeline via a Zenject installer. `InputCommandPipeline` consumes `InputCommandSO` assets and feeds them into the `CommandPipeline` for execution.

### Auto Logger

Hooks into Unity's log callback and writes all console output to a file. Configure via **Edit → Preferences → Auto Logger**. Uses UniTask for async file writing.

### Audio

ScriptableObject-driven audio system covering the full playback lifecycle:

`AudioClipSO` defines clips with randomization, per-clip config overrides, and cooldown intervals. `AudioSourceConfigSO` defines mixer group, volume/pitch ranges, spatial blend, and bypass settings. Extension methods on `AudioSource` provide `Configure`, `Play`, `PlayWithCooldown`, `PlayLoop`, `FadeOut`, `FadeIn`, and `CrossFade`. For fire-and-forget spatial audio, raise `PlayAudioClipSO` events to trigger pooled `AudioSourceWrapper` instances via `AudioSourceObjectPool`.

### UI Tweener

Strategy-based UI animation system built on DOTween. `UITweener` sequences a list of tween strategies in order. Available strategies: FadeTween, ScaleTween, MoveTween, OffsetTween, SizeTween, PulsateTween, PulsateFadeTween, LoopTween, DelayTween, MergeTween (parallel), JoinUITweener, SelectObject, PlaySFX, and OnCompleteCallback. Configured via ScriptableObjects.

### Free Camera

Free-fly camera controller using `InputCommandSO` bindings. Provides smooth WASD movement, mouse look, scroll-wheel speed control, and configurable boost. Settings are stored in a `FreeCameraSO` asset. Includes `TeleportTo`, `ResetSpeed`, and `OnToggled` API.

---

## MVP Pattern

### Core (`com.mytoolz.mvp`)

The MVP package provides the Model-View-Presenter architecture used by all game-facing systems.

**Model layer** — `IModel<T>` exposes `OnChanged`, `Clone()`, and `Reset()`. `ModelBase<T>` provides a base implementation. Call `NotifyChanged()` after mutations. `IValidatable` adds `IsValid()` and `GetValidationErrors()`.

**View layer** — `IReadOnlyView<T>` for display-only views. `IInteractableView<T>` adds user input events (`OnUserInput`, `OnSubmit`, `OnCancel`). `ICollectionView<T>` for lists. `ViewBase<T>` and `InteractableViewBase<T>` are MonoBehaviour bases.

**Presenter layer** — `IPresenter` defines Initialize/Enable/Disable/Dispose lifecycle. `PresenterBase<TModel, TView>` handles subscription management with idempotent enable/disable and one-shot dispose.

```csharp
public class HealthModel : ModelBase<HealthModel>
{
    public int Current { get; private set; }
    public int Max { get; private set; }

    public void TakeDamage(int amount)
    {
        Current = Mathf.Max(0, Current - amount);
        NotifyChanged();
    }

    public override HealthModel Clone() => new HealthModel(Max) { Current = Current };
    public override void Reset() { Current = Max; NotifyChanged(); }
}
```

### MVP UI Management System

Screen lifecycle management with layered stacking, input blocking, and animated transitions. `UILayerSO` assets define layer stacking rules. `UIStateManager` manages global open/close state. Screens extend `UIScreen` or `UISubScreen` and call `Open()`/`Close()`.

---

## MVP Game Systems

### MVP Clock

Stopwatch and timer with `ClockModel`, `ClockPresenter`, and `ClockView` (TextMeshPro). Integrates with UI Management System for screen lifecycle.

### MVP Game Settings

ScriptableObject-driven settings with typed setting definitions (`AudioSettingSO`, `BoolSettingSO`, `FloatSettingSO`, `IntSettingSO`, `StringSettingSO`, `FullscreenSettingSO`, `ResolutionSettingSO`, `QualitySettingSO`, `MultipleOptionSettingSO`) and matching UI views (slider, toggle, dropdown, input field). `SettingsPresenter` orchestrates apply/revert/save via the IO package.

### MVP Health System

Health model with current/max HP, damage types, hit box presenters (damage and heal), health bar views with DOTween animations. Communicates via EventBus. Integrates with UI Management System.

### MVP Inventory System

Generic, type-safe inventory with full EventBus-driven communication between all layers. Features grid-based UI with drag-and-drop, HUD hotbar slots, object-pooled cells, JSON save/load persistence, and configurable settings via ScriptableObject. All concrete types are created by subclassing generics:

```csharp
[Serializable]
public class WeaponInventoryModel : InventoryModel<WeaponItemSO> { }
public class WeaponPresenter : InventoryPresenter<WeaponItemSO> { }
public class WeaponInventoryView : InventoryView<WeaponItemSO> { }
```

Seeding priority: saved data → SO initial items → empty initialization.

### MVP Loading Screen

Async scene loading with progress tracking. `SceneLoader` uses UniTask for non-blocking loads. `SceneLoaderPresenter` orchestrates the flow. Progress is displayed via `SceneLoaderView`. Scene load events flow through the EventBus.

### MVP Notifications

Priority-driven notification queue with configurable overflow and deduplication policies. Notification types (`TextNotification`, `TimedNotification`, `KillNotification`, `AmmoNotification`) are MonoBehaviour subclasses pooled via Object Pool. The presenter is a thin orchestrator: model owns queue logic, view owns visual orchestration.

```csharp
EventBus<NotificationRequest>.Raise(new NotificationRequest
{
    Key = "player_kill",
    MessageType = typeof(KillNotification),
    Text = "Headshot!",
    Priority = NotificationPriority.High,
    Overflow = OverflowPolicy.DropLowestPriority
});
```

### Tooltip System

Event-driven tooltips using EventBus and a Singleton controller. `TooltipArea` components detect pointer enter/exit and raise events. `TooltipSystem` manages tooltip UI positioning and display.

---

## Standalone Packages

### A* Pathfinder

Generic, thread-safe A* pathfinding library with zero engine dependencies. The algorithm operates on abstract `TPos`/`TNode` types. Consumers implement four interfaces:

| Interface | Purpose |
|---|---|
| `IPathNode<TPos>` | Node with Position, Walkable, TraversalCost |
| `INodeLookup<TPos, TNode>` | Position → node resolution |
| `INeighborProvider<TPos>` | Neighbor computation on demand |
| `IHeuristic<TPos>` | Cost estimation |

Uses an array-backed binary min-heap for the open set. Supports both synchronous and async (thread pool) execution with cancellation.

```csharp
var pathfinder = new AStarPathfinder<GridPosition, GridNode>(lookup, neighbors, heuristic);
PathResult<GridPosition> result = await pathfinder.FindPathAsync(start, goal, ct);
```

---

## Conventions

### Event Listener Pattern

All MonoBehaviours subscribing to EventBus events implement `IEventListener` with registration in `OnEnable` and deregistration in both `OnDisable` and `OnDestroy`.

### ScriptableObject Configuration

Runtime-configurable settings are stored in ScriptableObjects created via the **Create** menu under **MyToolz**. This applies to audio clips, camera settings, UI layers, input modes, inventory settings, health bar configs, and debug preferences.

### Assembly Definitions

Every package ships with `.asmdef` files under the `MyToolz.*` namespace. Editor-only code uses separate `MyToolz.*.Editor` assemblies.

### Optional Folders

Packages with demo content place it in `Optional/` or `Optionals/` folders containing example scenes, prefabs, and implementation subclasses. These are not required for production use.

### Installer Order

When multiple Zenject installers are required, add them to `SceneContext` in the order specified by each package. Typically: core system installers first, then pool installers, then saver/persistence installers.

---

## Quick Start

1. Import the packages you need via UPM. Dependencies are resolved automatically through `package.json`.
2. Install external dependencies: Zenject, DOTween, UniTask, and `com.unity.nuget.newtonsoft-json` as needed.
3. Create a `SceneContext` GameObject in your scene and add the required MonoInstallers.
4. Create ScriptableObject assets for configuration (debug gates, UI layers, input modes, audio clips, etc.).
5. Add MonoBehaviour components to scene objects (views, presenters, pools, etc.).
6. Wire references in the inspector and press Play.

All packages target Unity 2022.3+ and are licensed under MIT.
