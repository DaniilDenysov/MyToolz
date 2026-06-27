# Scene Management

Additive, priority-batched multi-scene loading driven by the MyToolz event bus,
with aggregated progress reporting suitable for a loading screen.

## Dependencies

- `MyToolz.EventBus`
- `MyToolz.MVPLoadingScreen` (loading-screen events + `IProgressReporter<T>`)
- `MyToolz.Extensions` (`SceneExtensions`)
- `MyToolz.DebugUtility`
- UniTask (`Cysharp.Threading.Tasks`)

This package ships a self-contained, editor-driven `SceneReference` and does
**not** depend on Eflatun.SceneReference or Odin Inspector.

## Structure

```
Runtime/
├── SceneReference.cs           Build-safe serializable scene reference (SceneAsset -> path/name)
├── SceneType.cs                Scene role enum (ActiveScene, MainMenu, Gameplay, ...)
├── SceneData.cs                A scene + its type + load priority
├── SceneGroupSO.cs             ScriptableObject describing a group of scenes
├── SceneGroupManager.cs        Loads/unloads a group additively, batched by priority
├── AsyncOperationGroup.cs      Aggregates a batch of AsyncOperations + progress
├── MultiSceneLoader.cs         MonoBehaviour entry point; listens for load/reload events
├── MultiLoadingProgress.cs     Aggregates child LoadingProgress into a single 0..1 value
├── LoadSceneGroup.cs           Event: load a specific group (+ optional extra steps)
├── ReloadCurrentSceneGroup.cs  Event: reload the active group
├── SceneGroupLoading.cs        Event: a group started loading
└── SceneGroupLoaded.cs         Event: a group finished loading
```

## Usage

1. Create a **SceneGroupSO** asset (right-click -> `Create > MyToolz > BootStrapper >
   SceneGroupSO`). Assign each `SceneData` a scene (via the `SceneReference` field),
   a `SceneType`, and a load `Priority` (lower priorities load first; equal
   priorities load in parallel).
2. Add a **MultiSceneLoader** component to your bootstrap scene and assign the
   `SceneGroupSO[]`. The first group loads automatically on `Start`.
3. Trigger loads from anywhere via the event bus:

```csharp
EventBus<LoadSceneGroup>.Raise(new LoadSceneGroup { Group = myGroup });
EventBus<ReloadCurrentSceneGroup>.Raise(new ReloadCurrentSceneGroup());
```

`MultiSceneLoader` raises `LoadingScreenShow`/`LoadingScreenHide` (carrying an
`IProgressReporter<float>`) plus `SceneGroupLoading`/`SceneGroupLoaded`, so a
loading screen can react to progress and lifecycle.

> All scenes referenced by a group must be added to **Build Settings**;
> `SceneExtensions.IsSceneValid` is used when unloading to skip non-build scenes.
