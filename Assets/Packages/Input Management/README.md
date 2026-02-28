# Input Management

Core input state management layer providing device detection, mode-based input state switching, and a player input state interface. `InputModeSO` ScriptableObjects define which actions are enabled and cursor behavior per mode.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |

External: Unity Input System.

## Structure

```
Runtime/
├── IPlayerInputState.cs     Interface with OnEnter/OnExit for input state transitions
├── InputModeSO.cs           ScriptableObject implementing IPlayerInputState with cursor settings and enabled action lists
├── InputDeviceTracker.cs    Tracks active input device changes across action maps
└── InputStateManager.cs     Manages and dispatches input state transitions
```

## Usage

Create `InputModeSO` assets via **Create → MyToolz → InputManagement → InputModeSO** for each input context (gameplay, menu, dialogue). Configure cursor visibility, lock mode, and which `InputActionReference` entries are enabled. Use `InputStateManager.ChangeState()` to switch between modes at runtime. Use `InputDeviceTracker` to detect keyboard/gamepad switches.
