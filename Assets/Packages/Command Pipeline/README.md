# Command Pipeline

A simple command pattern implementation providing `ICommand` and `ICommandPipeline<T>` for queued command execution with a bounded pending queue and a configurable concurrent execution limit.

## Dependencies

External: Unity Input System (`com.unity.inputsystem`) — referenced by the runtime assembly for input-driven pipelines (see the Input Commands and Input Command Pipeline packages).

## Structure

```
Runtime/
├── ICommand.cs            Command contract (Execute)
├── ICommandPipeline.cs    Pipeline contract for queuing and executing commands
└── CommandPipeline.cs     Concrete pipeline implementation
```

## Usage

Implement `ICommand` for each discrete action, then enqueue instances into a `CommandPipeline<T>`:

- `Enqueue(command)` adds the command to the pending queue and immediately tries to execute it. The queue is bounded by `queueSize` — when full, the oldest pending command is discarded.
- Up to `callStackSize` commands may be executing at once. `Update()` promotes pending commands into execution until that limit is reached.
- Subclasses call `RemoveFinishedCommand(command)` when a command completes, freeing an execution slot for the next `Update()`.
- `Clear()` drops all pending and executing commands.
