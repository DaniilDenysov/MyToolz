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
