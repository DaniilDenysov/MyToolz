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

### Activation modes

`UILayerSO.activationMode` decides what opening a screen of that layer does to the layer stack:

| Mode | Behaviour |
|---|---|
| `Override` | Exits every layer on the stack, then pushes this one. |
| `Additive` | Exits the top layer but keeps it on the stack (closing this one re-enters it), then pushes this one. |
| `Blend` | Pushes this one on top; the layers below stay open. |
| `Cover` | For loading screens. Clears the stack like `Override` but is never pushed, so screens opened under it (the next scene's menus) never close it; only its own `Close()` does. The screens it opens over stay drawn behind it until their scene unloads, or until a screen opens under it or it closes. |

When a `Cover` screen opens, focus is taken off the screens it covers so a key press cannot click one. A selection
the cover made itself (its `firstSelected`, or its default screen's) is kept.

`UIScreenBase` moves focus to `firstSelected` when a screen enters and clears a selection inside the screen when it
exits.
