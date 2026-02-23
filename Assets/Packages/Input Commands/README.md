# Input Commands

ScriptableObject-based input command definitions that bridge Unity's Input System with the Command Pipeline pattern.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |
| Command Pipeline | `com.mytoolz.commandpipeline` |

External: Unity Input System.

## Structure

```
Runtime/
├── IInputCommand.cs     Interface extending ICommand with input-specific context
└── InputCommandSO.cs    ScriptableObject base class for defining input-bound commands
```

## Usage

Create concrete `InputCommandSO` assets for each input action. Wire them to Unity Input System actions. The command pipeline handles execution and undo.
