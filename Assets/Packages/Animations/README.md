# Animations

Priority-based animator state machine built on the State Machine package. Provides an abstract `AnimatorState` that maps serialized `AnimationClip` references to hash IDs, and an `AnimatorStateMachine` that drives an `Animator` component through priority-evaluated crossfade transitions.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |

Requires (via code references): State Machine (`com.mytoolz.statemachine`), Editor Toolz (`com.mytoolz.editortoolz`).

External: Zenject.

## Structure

```
Runtime/
└── AnimatorStateMachine.cs
    ├── AnimatorState            Abstract serializable state with AnimationClip hashing, randomization, and loop detection
    ├── IAnimatorStateMachine<T> Interface extending IStateMachine with animation duration query
    └── AnimatorStateMachine<T>  Abstract MonoBehaviour driving an Animator via PriorityStateMachine
```

## Usage

Subclass `AnimatorState` for each animation state. Implement `IsConditionFullfilled()` and set priority/interruptibility. Subclass `AnimatorStateMachine<T>` and assign the `Animator` reference.

```csharp
[Serializable]
public class IdleState : AnimatorState
{
    public override bool IsConditionFullfilled() => true;
}

public class CharacterAnimator : AnimatorStateMachine<CharacterState> { }
```

The state machine evaluates states by priority each frame. When a higher-priority state becomes valid, the animator crossfades to its animation hash. Randomized states select from an array of clips on each entry.
