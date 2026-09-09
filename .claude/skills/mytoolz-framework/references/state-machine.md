# State Machine

Package: `Assets/Packages/State Machine` · Namespace:
`MyToolz.DesignPatterns.StateMachine` (priority variants in the
`.SimplePriorityBased` sub-namespace).

Small, interface-first FSM primitives. Use them to model mutually-exclusive behaviour
(input modes, AI behaviours, UI phases) instead of piles of bool flags.

## Core interfaces

```csharp
public interface IState
{
    void OnEnter();
    void OnExit();
}

public interface IStateMachine<T> where T : IState
{
    void ChangeState(T state);
}
```

A plain state:

```csharp
using MyToolz.DesignPatterns.StateMachine;

public class ReloadingState : IState
{
    public void OnEnter() { /* play anim, lock firing */ }
    public void OnExit()  { /* unlock firing */ }
}
```

The canonical hand-driven example is `InputStateManager`
(`Assets/Packages/Input Management/Runtime/InputStateManager.cs`) — read it for a full
`IStateMachine<T>` implementation that swaps input states.

## SimplePriorityStateMachine (auto-select by priority)

For behaviour components where several states live as children and the machine should run
the highest-priority one, extend `SimplePriorityStateMachine` (a `MonoBehaviour`).

```csharp
using MyToolz.DesignPatterns.StateMachine.SimplePriorityBased;

public interface IPriorityState : IState
{
    uint Priority { get; }
    void Initialize();
}
```

Behaviour:
- `Awake` collects all `IPriorityState` in children (including inactive).
- `Start` sorts them by `Priority` descending and enters the highest.
- `ChangeState(state)` exits the current state and enters the new one (skips if it's already
  current); logs the transition via `DebugUtility`.
- Helpers: `IsExecuting(state)`, `TryGetCurrentState(out state)`.

### Priority strategies (`[SerializeReference]` in the Inspector)

Assign a `PriorityStrategy` to decide when a candidate outranks the current state:

- `HigherPriorityStrategy` — strictly greater priority wins (**default**).
- `HigherEqualPriorityStrategy` — greater-or-equal wins.
- `IgnorePriorityStrategy` — ignores priority (`chooseFirst` toggle).

```csharp
public class EnemyBrain : SimplePriorityStateMachine
{
    // Child components implementing IPriorityState are discovered automatically.
    // Override Awake/Start/ChangeState only if you need custom transition rules —
    // and call base first.
}
```

## Other variants

`Priority/PriorityStateMachine` and `MultiThreadPriority/PriorityStateMachine_MultiThread`
exist for more advanced cases. Start with `IState` + `SimplePriorityStateMachine`; reach for
the others only when their specific behaviour is required, and read the source first.

## Do / Don't

- ✅ One responsibility per state; put entry/exit side effects in `OnEnter`/`OnExit`.
- ✅ When overriding `Awake`/`Start`/`ChangeState` on a priority machine, call `base`.
- ❌ Don't scatter `isReloading`/`isAiming` bools where exclusive states belong.
