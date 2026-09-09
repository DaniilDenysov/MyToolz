# EventBus

Package: `Assets/Packages/Event Bus` · Namespaces: `MyToolz.DesignPatterns.EventBus`
(the bus) and `MyToolz.Events` (the `IEventListener` interface and most concrete events).

A type-safe, static, generic event bus. It's the default way systems talk to each other
without holding hard references. Prefer it over direct method calls between systems and
over ad-hoc `event Action`/`UnityEvent` wiring.

## Concepts

- **Event**: a type implementing the marker `IEvent`. The codebase uses **structs** for
  events (cheap, no GC): `public struct PlayerDied : IEvent { public int PlayerId; }`.
- **Bus**: `EventBus<T> where T : IEvent` — one static bus per event type.
- **Binding**: `EventBinding<T>` wraps the handler(s). You must keep a reference so you can
  deregister later.
- **Listener**: a class implementing `IEventListener` (`RegisterEvents()` /
  `UnregisterEvents()`).

## API

```csharp
EventBus<T>.Register(EventBinding<T> binding);
EventBus<T>.Deregister(EventBinding<T> binding);
EventBus<T>.Raise(T @event);

new EventBinding<T>(Action<T> handler);   // handler receives the event
new EventBinding<T>(Action handler);      // handler ignores the payload
binding.Add(Action<T>);  binding.Remove(Action<T>);   // add/remove more handlers
binding.Add(Action);     binding.Remove(Action);
```

## Canonical pattern

```csharp
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using UnityEngine;

public struct PlayerDied : IEvent { public int PlayerId; }

public class DeathCounter : MonoBehaviour, IEventListener
{
    EventBinding<PlayerDied> _died;

    void OnEnable()  => RegisterEvents();
    void OnDisable() => UnregisterEvents();

    public void RegisterEvents()
    {
        _died = new EventBinding<PlayerDied>(OnPlayerDied);
        EventBus<PlayerDied>.Register(_died);
    }

    public void UnregisterEvents() => EventBus<PlayerDied>.Deregister(_died);

    void OnPlayerDied(PlayerDied e) => DebugUtility.Log(this, $"Player {e.PlayerId} died");
}

// Somewhere else:
EventBus<PlayerDied>.Raise(new PlayerDied { PlayerId = 3 });
```

Non-MonoBehaviour listeners (e.g. `ScoreboardBackend` in the game code) implement the same
`IEventListener` pair and are driven by their owner's `OnEnable`/`OnDisable`.

## Guarantees worth knowing

- **Reentrancy-safe.** `Register`/`Deregister`/`Raise` are queued and drained by a single
  resolver, so raising an event *inside* a handler (or registering during a raise) is safe
  and won't throw "collection modified".
- **Auto-clear on Play-mode exit.** In the Editor, `EventBusUtil.ClearAllBuses()` runs when
  leaving Play mode, so static bindings don't survive into the next session. You still must
  deregister at runtime to avoid dead handlers within a session.
- Handlers fire in registration order; both the `Action<T>` and the no-arg `Action` of each
  binding are invoked.

## Do / Don't

- ✅ Pair every `Register` with a `Deregister`; store the binding in a field.
- ✅ Use structs implementing `IEvent` for payloads.
- ❌ Don't `new EventBinding<T>(...)` inline inside `Register` without keeping the reference —
  you'll never be able to deregister it.
- ❌ Don't reach across systems with direct references when an event fits.
