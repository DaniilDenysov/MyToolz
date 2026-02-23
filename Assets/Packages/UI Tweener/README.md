# UI Tweener

Strategy-based UI tweening system built on DOTween. Provides ScriptableObject-driven tween configurations and a composable strategy pattern for fade, scale, move, offset, size, pulsate, loop, delay, merge, join, and callback animations.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |
| Editor Toolz | `com.mytoolz.editortoolz` |
| Extensions | `com.mytoolz.extensions` |
| Tweener | `com.mytoolz.tweener` |

External: DOTween (`DG.Tweening`).

## Structure

```
Runtime/
├── UITweener.cs                        Core MonoBehaviour that sequences tween strategies
├── ScriptableObjects/
│   ├── FadeTweenSO.cs                  Fade alpha configuration
│   ├── ScaleTweenSO.cs                 Scale configuration
│   ├── MoveTweenSO.cs                  Position move configuration
│   ├── OffsetTweenSO.cs                RectTransform offset configuration
│   ├── SizeTweenSO.cs                  Size delta configuration
│   └── PulsateTweenSO.cs               Pulsate animation configuration
└── Tweens/
    ├── FadeTweenStrategy.cs            Fade alpha in/out
    ├── ScaleTweenStrategy.cs           Scale transform
    ├── MoveTweenStrategy.cs            Move position
    ├── OffsetTweenStrategy.cs          Animate RectTransform offsets
    ├── SizeTweenStrategy.cs            Animate size delta
    ├── PulsateTweenStrategy.cs         Repeating scale pulse
    ├── PulsateFadeTweenStrategy.cs     Repeating fade pulse
    ├── LoopTweenStrategy.cs            Loop wrapper for any strategy
    ├── DelayTweenStrategy.cs           Insert delay between strategies
    ├── MergeTweenStrategy.cs           Run strategies in parallel
    ├── JoinUITweenerStrategy.cs        Join another UITweener's sequence
    ├── SelectObjectTweenStrategy.cs    Select a GameObject during sequence
    ├── PlaySFXTweenStrategy.cs         Play a sound effect during sequence
    └── OnCompleteCallbackTweenStrategy.cs  Fire a UnityEvent on completion
```

## Usage

Add `UITweener` to a UI GameObject. Assign tween strategies in the inspector list. Call `Play()` to execute the sequence. Strategies are executed in order; use `MergeTweenStrategy` for parallel execution.
