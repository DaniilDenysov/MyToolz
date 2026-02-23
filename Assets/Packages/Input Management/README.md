# Input Management

Core input state management layer providing input phase tracking, device detection, and a player input state interface.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |

## Structure

```
Runtime/
├── InputPhase.cs           Enum defining input lifecycle phases
├── InputDeviceTracker.cs   Tracks active input devices (keyboard, gamepad, etc.)
├── IPlayerInputState.cs    Interface for querying current player input state
└── InputStateManager.cs    Manages and dispatches input state changes
```
