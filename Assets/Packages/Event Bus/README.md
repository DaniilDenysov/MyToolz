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
public struct PlayerDiedEvent : IEvent { public int PlayerId; }

EventBus<PlayerDiedEvent>.Register(binding);
EventBus<PlayerDiedEvent>.Raise(new PlayerDiedEvent { PlayerId = 1 });
EventBus<PlayerDiedEvent>.Deregister(binding);
```

Implement `IEventListener` on MonoBehaviours and register in `OnEnable`, deregister in `OnDisable`/`OnDestroy`.

Events must implement `IEvent`. Raising, registering, or deregistering from inside an event handler is safe — nested requests are queued and processed after the current dispatch finishes.

### Delivery guarantees

- Listeners are called in **registration order** (re-registering moves a binding to the end).
- A listener that **throws is logged and skipped**; every other listener still receives the event and the
  pending queue always drains, so one faulty listener cannot stall a bus.
- A binding deregistered from inside a handler still receives the event currently being dispatched and
  stops receiving from the next raise on.
- `Raise` does not allocate once the bus has warmed up (operations are queued as structs, not closures).
