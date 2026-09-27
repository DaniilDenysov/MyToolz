---
name: mytoolz-catalog
description: >-
  The complete catalogue of MyToolz packages ("tools") and the intended way to use each one.
  Use this WHENEVER you need to discover what MyToolz already provides before building
  something — when the user (or you) asks "does MyToolz have a…", "which package/system do I
  use for…", "what's the MyToolz way to…", "is there a built-in for…", "list the MyToolz
  packages", or when you're about to hand-roll pooling, saving, logging, input, UI screens,
  tweening, audio, localization, scene loading, pathfinding, notifications, health, inventory,
  settings, or DI wiring. Check here first so you reuse the framework instead of reinventing
  it. For deep, idiomatic how-to on the core systems, use the companion `mytoolz-framework`
  skill; this skill is the map (what exists + entry point), that one is the manual.
---

# MyToolz package catalogue

MyToolz is a Unity 6 (6000.3) monorepo. Every reusable **tool** is a self-contained package in
`Assets/Packages/<Name>/` (each with its own `package.json`, a `Runtime/` folder, sometimes
`Editor/` and `Optional/`). Game code lives in `Assets/Scripts/`. Everything is under the
`MyToolz.*` namespace.

**Use this catalogue to pick the right existing tool before writing new code.** Each entry
gives the package, its namespace, what it is, and the *intended* entry point. When an entry
points to `mytoolz-framework → references/<file>.md`, open that file in the **mytoolz-framework**
skill for the full API, gotchas, and worked examples.

## Cross-cutting conventions (apply to all packages)

- **Log** with `DebugUtility.Log(this, …)`, never `UnityEngine.Debug`.
- **Wire dependencies** with Zenject (`MonoInstaller.InstallBindings()` + `[Inject] Construct`).
  Zenject is external but is the only DI mechanism here.
- **Talk between systems** with `EventBus<T>` events, not hard references.
- **Inspector attributes** come from `MyToolz.EditorToolz`, not Odin.
- Full rationale and idioms: the **mytoolz-framework** skill.

---

## Foundation (no inter-package dependencies)

| Package | Namespace | What it is | Intended use |
|---------|-----------|-----------|--------------|
| **EditorToolz** | `MyToolz.EditorToolz` | Inspector attributes replacing Odin. | Annotate fields/methods: `[Button]`, `[Required]`, `[ReadOnly]`, `[ShowIf]`/`[HideIf]`, `[FoldoutGroup]`/`[TitleGroup]`, `[OnValueChanged]`, `[LabelText]`, `[SuffixLabel]`, `[MinValue]`/`[MaxValue]`, `[PropertyOrder]`, `[ShowInInspector]`, `[ListDrawerSettings]`, `[RequireInterface]`. Implement `ISelfValidator.Validate` to surface setup errors as inspector HelpBoxes. |
| **DebugUtility** | `MyToolz.Utilities.Debug` | Logging wrapper with auto-tagging + per-type gating. | `DebugUtility.Log/LogWarning/LogError(this, msg)`. Passing `this` enables `LogGate` on/off gating. → `mytoolz-framework → references/debug-utility.md` |
| **Extensions** | `MyToolz.Extensions` | Small helpers. | `someDouble.ToFloat()`; `UIUtilities.ExtractSceneName(path)`; `SceneExtensions.IsSceneInBuildSettings/IsSceneValid/GetSceneNameByBuildIndex/GetBuildSceneCount`. |
| **DataStructures** | `MyToolz.DataStructures` | Data structures. | `BiDictionary<TKey,TValue>` — two-way map: `TryAdd`, `TryGetValue`, `TryGetKey`, `Remove`, `RemoveByValue`. |
| **Adapter** | `MyToolz.DesignPatterns.Adapter` | Adapter contract. | Implement `IAdapter<From,To>.Convert(from)` to translate between representations. |
| **Prototype** | `MyToolz.DesignPatterns.Prototype` | Prototype/factory contract. | Implement `IPrototype<T>.Get()` to hand out configured instances/clones. |

## Core design patterns

| Package | Namespace | What it is | Intended use |
|---------|-----------|-----------|--------------|
| **EventBus** | `MyToolz.DesignPatterns.EventBus`, `MyToolz.Events` | Type-safe static event bus. | Define `struct X : IEvent`; `EventBus<X>.Raise(e)`. Subscribers implement `IEventListener`, hold an `EventBinding<X>`, and `Register`/`Deregister` in pairs. Listeners run in registration order; a throwing listener is logged and the rest still run. → `references/event-bus.md` |
| **ObjectPool** | `MyToolz.DesignPatterns.ObjectPool`, `MyToolz.Events` | Event-driven pooling over Zenject `MemoryPool`. | Add a pool installer listing the prefab (`DefaultObjectPoolInstaller<T>`, or `AddressableObjectPoolInstaller<T>` for Addressable prefabs requested by `Key`); spawn with `EventBus<PoolRequest<T>>.Raise(...)`, return with `ReleaseRequest<T>`. Pooled objects implement `IPoolable`. Listen for `PoolInitializationFailed` to surface load failures. → `references/object-pool.md` |
| **StateMachine** | `MyToolz.DesignPatterns.StateMachine` (+ `.SimplePriorityBased`, `.PriorityBased`) | Interface-based state machines. | Implement `IState` (`OnEnter`/`OnExit`); for auto-selecting behaviours extend `SimplePriorityStateMachine` with `IPriorityState.Priority`. → `references/state-machine.md` |
| **Singleton** | `MyToolz.DesignPatterns.Singleton` | Safe MonoBehaviour singleton bases. | Default to `PrivateSingleton<T>` (uniqueness, no global static). `PublicSingleton<T>` (global `Instance`) is a red flag — prohibited unless explicitly requested. Override `OnSingletonAwake`/`OnSingletonDestroy`, never `Awake`/`OnDestroy`. → `references/singleton.md` |
| **Command Pipeline** | `MyToolz.DesignPatterns.Command` | Generic command queue. | Implement `ICommand.Execute()`; `CommandPipeline<T>` queues (`queueSize`) and runs up to `callStackSize` concurrently via `Enqueue`/`Update`. Input builds on this — see Input Command Pipeline. → `references/input.md` |
| **MVP** | `MyToolz.DesignPatterns.MVP.{Model,View,Presenter}` | Model–View–Presenter base contracts. | `ModelBase<T>` (call `NotifyChanged()`), `ViewBase<T>`/`IReadOnlyView<T>`, `PresenterBase<TModel,TView>` (pure C#) or a MonoBehaviour presenter with `[Inject] Construct`. Bind in a `MonoInstaller`. → `references/mvp.md` |

## Persistence & diagnostics

| Package | Namespace | What it is | Intended use |
|---------|-----------|-----------|--------------|
| **IO** | `MyToolz.IO` | Save/load with swappable serialization/storage/encryption. | Subclass `SaveLoadBase<T> where T : class,new()` (it *is* a `MonoInstaller`), drop on a context, inject `ISaver<T>` and call `Save`/`Load` (never null). Default: Newtonsoft JSON, atomic writes + `.bak`. `AesEncryptionStrategy` = random IV + HMAC (tamper-evident; set a project key), `XorEncryptionStrategy` = obfuscation only. Requires `com.unity.nuget.newtonsoft-json`. → `references/io.md` |
| **AutoLogger** | `MyToolz.Utilities.AutoLogger` | Automatic build-only session logger. | Zero code — active only in builds (`#if !UNITY_EDITOR`), auto-hooks `Application.logMessageReceived`. Configure `LogFileWriterPreferences` (ScriptableObject in `Resources/`, Project Settings > Log File Writer): `enabled`, `enabledInReleaseBuilds`, `enabledOnWebGL` (off by default). Captures FPS, scene history, bounded message-frequency stats, writes a summary on quit. |

## Dependency injection

**Zenject** (external) is the only DI mechanism. Write a `MonoInstaller`, `Container.Bind<…>()`
in `InstallBindings()`, resolve with `[Inject] void Construct(…)`. Contexts: Project (global),
Scene, GameObject (per-instance). Note that `SaveLoadBase<T>` and the pool/input installers are
themselves installers. → `mytoolz-framework → references/zenject-installers.md`

## Input

| Package | Namespace | What it is | Intended use |
|---------|-----------|-----------|--------------|
| **Input Commands** | `MyToolz.InputManagement.Commands` | One asset per action, over Unity Input System. | Author an `InputCommandSO` per action (wraps an `InputActionReference`); consume via its events (`OnPerformed`, `OnPressed`, …) or polling (`WasPressedThisFrame`, `ReadValue<T>`). → `references/input.md` |
| **Input Management** | `MyToolz.InputManagement` | Input modes + device tracking. | `InputModeSO` per context (enables its commands, sets cursor); switch with injected `InputStateManager.ChangeState(mode)`. `InputDeviceTracker.OnInputDeviceChanged` for KBM↔gamepad UI. Wire via `InputStateManagementInstaller`. → `references/input.md` |
| **Input Command Pipeline** | `MyToolz.InputManagement.Commands.Pipeline` | Buffered/time-based input commands. | For combos/charged/buffered actions: `IInputCommand` (adds `Update`/`IsFinished`); `InputCommandPipeline` + its installer. Pump `Update()` each frame. → `references/input.md` |
| **Free Camera** | `MyToolz.FreeCamera` | Debug/spectator fly-camera. | Add `FreeCameraController` (requires a `Camera`); assign a `FreeCameraSO` and the `InputCommandSO`s (toggle/move/look/vertical/scroll/boost). Toggle at runtime; `OnToggled` event. |

## UI

| Package | Namespace | What it is | Intended use |
|---------|-----------|-----------|--------------|
| **MVP UI ManagementSystem** | `MyToolz.UI.Management` | Layered screen stack. | Model screens as `UIScreen`/`UISubScreen`; group with `UILayerSO` (`Override`/`Additive`/`Blend`); open/close via `Open()`/`Close()` through the injected `UILayerStateManager`. Add `UIInstaller`. → `references/ui-management-system.md` |
| **UI Layout System** | `MyToolz.UI.Layout` | Layout & robust UI widgets. | `FlexLayoutGroup` + `FlexChild` (flexbox-style grow/shrink/basis); `SafeAreaFitter` (notch-safe RectTransform); `UIStrongButton` (a `Button` that loudly reports broken `onClick` bindings instead of failing silently, with optional click/hover/press/disable `AudioClipSO`s). |
| **UI Kit** | `MyToolz.UI.Kit` | Ready-made uGUI widgets. | `ArcLayout` (children on an arch), `CardCarousel<TCard>` (swipe/snap carousel; implement `Choose`; optional `chooseButton` such as a Play button calls `ChooseCurrent`), `TextBoxFitter`, `FitContentToHeight`, `UIShine`, `UISparkleBurst`/`WorldSparkleBurst`, `UISpinner`, `UIStrongToggle` (bound to a `BoolSettingSO`) and `SwitchButton` (cycles an `IntSettingSO` via a `SwitchButtonStrategy`). |
| **Tweener** | `MyToolz.Tweener` | DOTween sequence base. | Extend `Tweener<T>` with `AbstractTweenStrategy` subclasses; strategies compose into a `Sequence` (parallel or serial), `ignoreTimeScale` for pause-proof tweens. → `mytoolz-framework → references/tweener.md` |
| **UI Tweener** | `MyToolz.Tweener.UI` | Trigger-driven UI tweens. | Put `UITweener` on a UI object; add tween strategies (Fade/Move/Scale/Rotate/Size/Offset/Pulsate/…) each bound to an `ActivationTrigger` (Awake/Start/Enable/Disable/OnClick/OnEnter/OnExit/Manual); drive with `SetActive(bool)`. This is the `screenTweener` used by UI screens. → `mytoolz-framework → references/ui-tweener.md` |
| **Tooltip System** | `MyToolz.UI.ToolTip` | Cursor-following tooltips. | Add one `TooltipSystem` (assign root + text); add `TooltipArea` (with a description) to any UI Graphic. Hover raises `ShowTooltip`/`HideTooltip` (`MyToolz.UI.Events`) over the EventBus. |
| **MVP Game Settings** | `MyToolz.GameSettings`, `MyToolz.ScriptableObjects.GameSettings` | Settings screen + persisted settings. | Author setting SOs (`FloatSettingSO`, `BoolSettingSO`, `ResolutionSettingSO`, `QualitySettingSO`, `AudioSettingSO`, …) from `SettingSOAbstract`; `SettingsPresenter` binds them to views (slider/dropdown/toggle/carousel) and persists via IO, batching writes (`saveDelaySeconds`, flushed on pause/quit; `Flush()`/`Save()` to force). |
| **MVP Notifications** | `MyToolz.UI.Notifications`, events in `MyToolz.UI.Events` | Queued on-screen notifications. | Add `NotificationInstaller` + a `PlayerNotificationView`; raise `NotificationRequest` (with `NotificationData`: priority, overflow, dedupe) to show one, `NotificationClearRequest` to clear. Views are pooled; extend `NotificationBase` for new kinds. |
| **MVP Loading Screen** | `MyToolz.UI.LoadingScreen`, events in `MyToolz.Events` | Scene-loading screen (a.k.a. SceneLoader). | Add `SceneLoaderInstaller`; raise `LoadScene`, watch `SceneLoading`/`SceneLoaded`. `SceneLoaderPresenter` drives an `IProgressBar` (slider or `IndeterminateLoadingRing`) from `ISceneLoaderModel` progress. `minimalLoadingDuration` defaults to 0. |
| **MVP Clock** | `MyToolz.Clock` (`.Interfaces`) | Timer / stopwatch feature. | `IClockPresenter` (`Start`/`Stop`/`Pause`/`Resume` + `Elapsed`/`Paused`/… events) over `IClockModel` (`ClockMode` count-up/down) and `IClockView : IReadOnlyView<float>`. |
| **MVP Inventory System** | `MyToolz.InventorySystem` (`.Models`, `.Installers`, …) | Grid inventory with drag/drop + save. | Extend the generic `InventoryInstaller<T,Model>` / `InventoryModel<T>` with your `ItemSO` type; drag-drop views, object-pooled item views, `InventorySaver`. See the package's `Optional/Implementation/` for a concrete worked set. |
| **MVP Health System** | `MyToolz.HealthSystem` (`.Interfaces`, `.Installers`) | Damage / heal / death. | Add `HealthSystemInstaller`; model implements `IHealthModel : IDamagable<IDamageArgs>, IHealable` (+ `IKillable`), fires `HealthChanged`/`Died`. Presenters (`HitBoxPresenter`, `HealableHitBoxPresenter`) apply damage/heal; `HealthbarView` renders. |
| **Localization** | `MyToolz.Localization` | Multi-language text (CSV-backed). | Add a `LocalizationManager` (assign a `LocalizationDatabaseSO`); use `LocalizationText` (a `TextMeshProUGUI`) bound to a `LocalizationBindingSO`; switch via `ChangeLanguageRequest`, react to `LanguageChanged`. `[LocalizationKey]` marks key fields. (Note: this package uses `PublicSingleton` internally.) → `mytoolz-framework → references/localization.md` |

## Audio

| Package | Namespace | What it is | Intended use |
|---------|-----------|-----------|--------------|
| **Audio** | `MyToolz.Audio` (events in `MyToolz.Audio.Events`) | SFX + adaptive music. | SFX: raise `EventBus<PlayAudioClipSO>.Raise(new PlayAudioClipSO{ AudioClipSO=clip, Position=p })`; one `AudioManager` (pooled `AudioSourceWrapper`s, per-clip min-interval throttle) plays it. Music: `MXManager` with `PlaySong`/`StopSong`/`SetIntensity` events (UniTask blending, looping, intensity layers). Author `AudioClipSO`/`SongSO`/`AudioSourceConfigSO`. → `mytoolz-framework → references/audio.md` |

## Scenes

| Package | Namespace | What it is | Intended use |
|---------|-----------|-----------|--------------|
| **SceneManagement** | `MyToolz.SceneManagement` | Additive multi-scene group loading. | Author `SceneGroupSO`s (scenes + `SceneType` + priority batches); add a `MultiSceneLoader`; raise `LoadSceneGroup`/`ReloadCurrentSceneGroup` (UniTask, progress-reporting). Scenes load from Addressables or the build list (`SceneLoadBackend.Auto`); failures raise `SceneGroupLoadFailed` instead of `SceneGroupLoaded`. For a loading-screen UI on top, combine with **MVP Loading Screen**. |

## Gameplay / algorithms

| Package | Namespace | What it is | Intended use |
|---------|-----------|-----------|--------------|
| **AStar** | `MyToolz.Algorithms.AStar` | Generic A* pathfinding. | Implement `IPathNode<TPos>`, `INeighborProvider<TPos>`, `IHeuristic<TPos>`, `INodeLookup<TPos,TNode>`; construct `AStarPathfinder<TPos,TNode>` and call `FindPath`/`FindPathAsync` → `PathResult<TPos>` (`Success`, `Positions`, `TotalCost`). Grid/graph-agnostic. |
| **Animations** | `MyToolz.Animations` | Priority-based animation states. | Extend `AnimatorState : PriorityState` (assign `AnimationClip`(s), optional randomize/loop); drive an `Animator` through the priority state machine so the highest-priority valid animation wins. |
| **Bootstrap** | `MyToolz.Bootstrap` | Boot entry object. | `Bootstrapper : PrivateSingleton<Bootstrapper>` — a single persistent bootstrap object; put first-run/boot wiring on/near it (e.g. kick off the initial scene group). |

---

## How to use this catalogue

1. **Identify the need** (log, pool, save, screen, input, tween, audio, path, notify, …).
2. **Find the package** in the table above and use its *intended entry point* — don't hand-roll
   an equivalent.
3. **For core systems, go deep** in the **mytoolz-framework** skill's `references/` (linked as
   `references/<file>.md`).
4. **If nothing fits**, add a new package under `Assets/Packages/<Name>/` (`Runtime/` +
   `package.json`, `MyToolz.*` namespace) following the same conventions.
