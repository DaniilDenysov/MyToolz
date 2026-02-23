# MVP Loading Screen

A plug-and-play loading screen solution with async scene loading, progress tracking, and MVP architecture. Uses the EventBus for scene load events and the Singleton pattern for global access.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |
| Event Bus | `com.mytoolz.eventbus` |
| Singleton | `com.mytoolz.singleton` |
| Editor Toolz | `com.mytoolz.editortoolz` |
| Extensions | `com.mytoolz.extensions` |
| MVP | `com.mytoolz.mvp` |
| MVP UI Management System | `com.mytoolz.mvpuimanagementsystem` |

External: Zenject, UniTask (`Cysharp.Threading.Tasks`).

## Structure

```
Runtime/
├── ISceneLoaderModel.cs          Model contract for scene load state
├── SceneLoaderModel.cs           Concrete model tracking load progress
├── SceneLoaderPresenter.cs       Presenter orchestrating load flow
├── SceneLoaderView.cs            View rendering progress bar and status
├── SceneLoader.cs                Async scene loader using UniTask
├── SceneLoaderEvents.cs          EventBus events for load started/completed/failed
└── SceneLoaderInstaller.cs       Zenject installer
Optional/
└── ExampleSceneLoading.cs        Demo usage
```

## Setup

Add `SceneLoaderInstaller` to your SceneContext. Place the `SceneLoaderView` prefab in your scene. Trigger scene loads through the `SceneLoader` API or by raising `SceneLoadRequestEvent` on the EventBus.
