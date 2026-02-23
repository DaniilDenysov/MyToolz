# Event Bus

A generic event bus system extended from [adammyhre/Unity-Event-Bus](https://github.com/adammyhre/Unity-Event-Bus). Modified to support generic event types with structured binding and automatic assembly scanning.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |

## Structure

```
Runtime/
├── EventBus.cs                Static generic event bus — EventBus<T> for any event type
├── EventBinding.cs            Strongly-typed event binding with subscribe/unsubscribe
├── Events.cs                  Base event interfaces and common event definitions
├── EventBusUtil.cs            Utility methods for bus management
└── PredefinedAssemblyUtil.cs  Assembly scanning for automatic event type registration
```

## Usage

Define an event struct, then raise and listen:

```csharp
public struct PlayerDiedEvent { public int PlayerId; }

EventBus<PlayerDiedEvent>.Register(binding);
EventBus<PlayerDiedEvent>.Raise(new PlayerDiedEvent { PlayerId = 1 });
EventBus<PlayerDiedEvent>.Deregister(binding);
```

Implement `IEventListener` on MonoBehaviours and register in `OnEnable`, deregister in `OnDisable`/`OnDestroy`.
