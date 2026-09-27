# Scene Management

Additive, priority-batched multi-scene loading driven by the MyToolz event bus,
with aggregated progress reporting suitable for a loading screen.

## Dependencies

- `MyToolz.EventBus`
- `MyToolz.MVPLoadingScreen` (loading-screen events + `IProgressReporter<T>`)
- `MyToolz.Extensions` (`SceneExtensions`)
- `MyToolz.DebugUtility`
- UniTask (`Cysharp.Threading.Tasks`)
- Addressables (`com.unity.addressables`) - scenes can load from Addressables or the build list

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
├── SceneGroupLoaded.cs         Event: a group finished loading
└── SceneGroupLoadFailed.cs     Event: one or more scenes of a group failed to load
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

`MultiSceneLoader.minimalLoadingDuration` is part of loading: the last scene of a
group is loaded with activation held back and is only activated once that many
seconds have passed since the load started; `SceneGroupLoaded` and
`LoadingScreenHide` follow the activation. Unity can hold only one scene before
activation, so scenes loaded earlier in the group are activated as they finish,
and while the last scene waits Unity's async-operation queue (other scene loads,
asset bundles, `Resources.UnloadUnusedAssets`) is paused. A `LoadSceneGroup` or
`ReloadCurrentSceneGroup` raised while a load is running is queued and runs as soon
as that load finishes; if several arrive, only the latest is kept.

`minimalLoadingDuration` defaults to 0 (activate as soon as the group is ready).

### Where scenes load from

`SceneGroupManager.backend` picks how each scene is loaded:

| Backend | Behaviour |
|---|---|
| `Auto` (default) | A scene whose GUID is in the Addressables catalog loads through Addressables; every other scene loads by name from the build scene list. |
| `Addressables` | Every scene loads through Addressables by GUID (requires "Include GUIDs in Catalog"). |
| `BuildSettings` | Every scene loads by name from **Build Profiles > Scene List**. |

A scene that is neither Addressable nor in the build list is reported up front instead of being skipped.

### Failures

Every load is checked. When any scene of a group fails (missing from the build list, an Addressables
error, ...) the other scenes still finish, the loading screen is hidden, the error is logged, and
`SceneGroupLoadFailed { Group, Reason }` is raised **instead of** `SceneGroupLoaded`. Listen for it to tell
the player rather than leaving them on a half-loaded screen.

> Build-list scenes are unloaded when a new group loads unless they are blacklisted, the active scene, or
> owned by an Addressables handle; `SceneExtensions.IsSceneValid` is used to skip scenes that are not in the build list.
