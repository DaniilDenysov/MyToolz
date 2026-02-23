# MVP UI Management System

A screen lifecycle management system for Unity UI. Manages opening, closing, layering, and input blocking of UI screens with support for sub-screens and animated transitions via UI Tweener.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |
| Editor Toolz | `com.mytoolz.editortoolz` |
| Input Management | `com.mytoolz.inputmanagement` |
| UI Tweener | `com.mytoolz.uitweener` |

External: Zenject.

## Structure

```
Runtime/
├── Installers/
│   └── UIInstaller.cs              Zenject installer for binding UIStateManager
├── Model/
│   └── UILayerSO.cs                ScriptableObject defining a UI layer and its stacking rules
├── Presenter/
│   ├── UIStateManager.cs           Manages global screen open/close state and input blocking
│   └── UILayerStateManager.cs      Per-layer state tracking for screen stacking
└── View/
    ├── UIScreenBase.cs             Abstract base for all UI screens with lifecycle hooks
    ├── UIScreen.cs                 Concrete full-screen UI panel
    └── UISubScreen.cs              Nested screen that lives inside a parent UIScreen
```

## Usage

Create `UILayerSO` assets for each UI layer (e.g., HUD, Popups, Modals). Add `UIInstaller` to your SceneContext. Have screen MonoBehaviours extend `UIScreen` or `UISubScreen` and assign their layer. Call `Open()` and `Close()` to manage screen lifecycle.
