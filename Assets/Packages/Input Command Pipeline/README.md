# Input Command Pipeline

Bridges Input Commands with the Command Pipeline, providing a Zenject installer and runtime wiring for input-driven command execution.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |
| Command Pipeline | `com.mytoolz.commandpipeline` |
| Input Commands | `com.mytoolz.inputcommands` |

External: Unity Input System, Zenject.

## Structure

```
Runtime/
├── InputCommandPipeline.cs            Consumes InputCommandSO assets and feeds them into the CommandPipeline
└── InputCommandPipelineInstaller.cs   Zenject MonoInstaller for binding the pipeline
```

## Setup

Add `InputCommandPipelineInstaller` to your SceneContext. Assign the list of `InputCommandSO` assets to process. The installer binds `InputCommandPipeline` into the container and begins listening for input.
