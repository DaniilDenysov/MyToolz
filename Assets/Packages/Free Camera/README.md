# Free Camera

A lightweight free-fly camera controller for Unity using the Input System.

## Requirements

- Unity 2022.3+
- Unity Input System 1.7.0+

## Installation

Add via Unity Package Manager using the git URL or copy the `Free Camera` folder into your project's `Packages` directory.

## Setup

1. Create a `FreeCameraSO` asset via **Create → MyToolz → FreeCamera → FreeCameraSO**.
2. Create an Input Actions asset with actions for Move (Vector2), Look (Vector2), Vertical (Float), Scroll (Vector2), Toggle (Button), and Boost (Button).
3. Add the `FreeCameraController` component to a GameObject with a `Camera`.
4. Assign the `FreeCameraSO` asset and `InputActionReference` fields in the Inspector.

## Controls

| Action   | Default Binding     |
|----------|---------------------|
| Move     | WASD                |
| Look     | Mouse Delta         |
| Vertical | Q / E               |
| Scroll   | Mouse Scroll Wheel  |
| Boost    | Left Shift          |
| Toggle   | F1 (or custom)      |

## ScriptableObject Settings

| Field             | Description                          | Default |
|-------------------|--------------------------------------|---------|
| Move Speed        | Base movement speed                  | 10      |
| Min Speed         | Minimum scroll-adjusted speed        | 0.5     |
| Max Speed         | Maximum scroll-adjusted speed        | 50      |
| Boost Multiplier  | Speed multiplier when boosting       | 3       |
| Look Sensitivity  | Mouse look sensitivity               | 200     |
| Scroll Sensitivity| Speed adjustment per scroll tick      | 0.5     |
| Move Smooth Time  | Position smoothing duration          | 0.15    |
| Look Smooth Time  | Rotation smoothing duration          | 0.05    |

## Public API

```csharp
bool IsActive { get; }
event Action<bool> OnToggled;

void SetSettings(FreeCameraSO newSettings);
void ResetSpeed();
void TeleportTo(Vector3 position, Quaternion rotation);
```

## Changes from v1

- Removed Zenject, EditorToolz, InputCommands, DebugUtility, and UI Management dependencies.
- Uses Unity `InputActionReference` directly instead of custom InputCommandSO wrappers.
- Uses `Time.unscaledDeltaTime` for movement so the camera works while the game is paused.
- Uses `Quaternion.Euler` instead of setting `eulerAngles` directly to avoid gimbal artifacts.
- Added cursor state save/restore on toggle.
- Added `OnToggled` event, `TeleportTo`, `ResetSpeed`, and `SetSettings` public API.
- Added assembly definition file (`MyToolz.FreeCamera.asmdef`).
- Fixed pitch initialization for angles above 180°.
- Boost input is now configurable via `InputActionReference` instead of hardcoded to `Keyboard.current.leftShiftKey`.

## License

MIT
