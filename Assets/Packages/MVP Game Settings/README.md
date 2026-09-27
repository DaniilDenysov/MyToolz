# MVP Game Settings

A ScriptableObject-driven game settings system with save/load persistence and MVP views. Supports audio, resolution, quality, fullscreen, and custom settings through typed ScriptableObject definitions and matching UI views.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |
| Editor Toolz | `com.mytoolz.editortoolz` |
| Extensions | `com.mytoolz.extensions` |
| IO | `com.mytoolz.io` |

External: Zenject, TextMeshPro.

## Structure

```
Runtime/
├── Model/
│   ├── SettingSOAbstract.cs           Base for all setting ScriptableObjects
│   ├── SettingSOGeneric.cs            Generic typed setting base
│   ├── AudioSettingSO.cs              Audio mixer volume setting
│   ├── BoolSettingSO.cs               Boolean toggle setting
│   ├── FloatSettingSO.cs              Float slider setting
│   ├── IntSettingSO.cs                Integer setting
│   ├── StringSettingSO.cs             String input setting
│   ├── FullscreenSettingSO.cs         Fullscreen mode setting
│   ├── ResolutionSettingSO.cs         Screen resolution setting
│   ├── QualitySettingSO.cs            Quality level setting
│   └── MultipleOptionSettingSO.cs     Multi-option dropdown setting
├── Presenter/
│   ├── SettingsPresenter.cs           Orchestrates setting apply/revert/save
│   └── SettingsSaveLoad.cs            Persistence layer using IO package
└── View/
    ├── SettingView.cs                 Abstract base view for a single setting
    ├── SliderSettingView.cs           Float/int slider UI
    ├── ToggleSettingView.cs           Boolean toggle UI
    ├── DropdownSettingView.cs         Dropdown selector UI
    ├── DropdownSettingViewAbstract.cs Abstract dropdown base
    ├── ResolutionDropdownSettingView.cs Resolution-specific dropdown
    └── InputFieldSettingView.cs       Text input field UI
```

## Setup

Create setting SO assets for each configurable option. Add `SettingsPresenter` to your scene and assign the settings list. Create matching views for each setting type in your settings UI panel.

## Saving

`SettingsPresenter` batches writes instead of saving the whole file on every change:

- A change marks the settings dirty. The file is written `saveDelaySeconds` (default 1 s, unscaled) after the
  last change, and never later than `maxSaveDelaySeconds` (default 5 s) after the first unsaved one, so a
  slider drag or a value that changes every frame costs one write.
- Pending changes are written immediately when the app is paused, loses focus, quits, or the presenter is
  disabled/destroyed. Call `Flush()` to write pending changes now, or `Save()` to force a write.
- A setting's twin copies (e.g. the same asset loaded again from a bundle) mirror each other's values; their
  mirrored updates are coalesced into the same write.
- Saving is refused until the stored values have loaded, so defaults never overwrite a save.
