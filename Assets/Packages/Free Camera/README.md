# Free Camera

A lightweight free-fly camera controller for Unity using InputCommandSO-based input bindings.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |
| Event Bus | `com.mytoolz.eventbus` |
| Input Commands | `com.mytoolz.inputcommands` |

External: Unity Input System 1.7.0+.

## Requirements

- Unity 2022.3+

## Setup

1. Create a `FreeCameraSO` asset via **Create → MyToolz → FreeCamera → FreeCameraSO**.
2. Create `InputCommandSO` assets for Move (Vector2), Look (Vector2), Vertical (Float), Scroll (Vector2), Toggle (Button), and Boost (Button).
3. Add the `FreeCameraController` component to a GameObject with a `Camera`.
4. Assign the `FreeCameraSO` asset and `InputCommandSO` fields in the Inspector.

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

## Structure

```
Runtime/
├── FreeCameraController.cs   MonoBehaviour with smooth movement, mouse look, scroll speed, and boost
└── FreeCameraSO.cs           ScriptableObject holding all camera configuration values
Optional/
├── FreeCam.asset             Example InputCommandSO assets and scene
└── FreeCamera.unity          Demo scene
```

## Public API

```csharp
bool IsActive { get; }
event Action<bool> OnToggled;

void SetSettings(FreeCameraSO newSettings);
void ResetSpeed();
void TeleportTo(Vector3 position, Quaternion rotation);
```

## License

MIT
