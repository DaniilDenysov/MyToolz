# State Machine

State machine design pattern with multiple implementations: a simple priority-based variant, a full priority-based variant, and a multi-threaded priority-based variant.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |
| Editor Toolz | `com.mytoolz.editortoolz` |

External: Zenject (priority and multi-thread variants), UniTask (multi-thread variant).

## Structure

```
Runtime/
├── IState.cs                                          State contract
├── IStateMachine.cs                                   State machine contract
├── Priority/
│   ├── SimplePriorityStateMachine.cs                  Lightweight priority-based state machine
│   └── PriorityStateMachine.cs                        Full priority-based state machine with Zenject
└── MultiThreadPriority/
    └── PriorityStateMachine_MultiThread.cs            Thread-safe priority state machine using UniTask
```

## Notes

- `PriorityStateMachine_MultiThread` evaluates state conditions on the thread pool via UniTask at a configurable rate (`evaluationRateHz`) and applies transitions on the main thread. The worker stops on disable and restarts automatically on re-enable. `IsConditionFulfilled()` implementations must therefore be thread-safe.
