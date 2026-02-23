# Command Pipeline

A simple command pattern implementation providing `ICommand` and `ICommandPipeline` interfaces for sequential command execution.

## Dependencies

None.

## Structure

```
Runtime/
├── ICommand.cs            Command contract with Execute and Undo
├── ICommandPipeline.cs    Pipeline contract for queuing and executing commands
└── CommandPipeline.cs     Concrete pipeline implementation
```

## Usage

Implement `ICommand` for each discrete action. Use `CommandPipeline` to queue and execute commands in order. Supports undo by reversing the command stack.
