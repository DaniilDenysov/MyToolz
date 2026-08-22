# Tweener

Base tweening abstraction layer built on DOTween. Provides the foundation that UI Tweener and other tweening packages extend.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |

External: DOTween (`DG.Tweening`).

## Structure

```
Runtime/
└── Tweener.cs   AbstractTweenStrategy + Tweener<T> base class wrapping DOTween sequences with lifecycle management
```

## Usage

Subclass `AbstractTweenStrategy` to produce a `Tween`, then subclass `Tweener<T>` to compose strategies:

- `CreateSequence(strategies)` builds a single DOTween `Sequence` from the strategies — appended one after another, or joined to run in parallel when `paralelExecution` is enabled. Null strategies and null tweens are skipped.
- Created sequences are tracked in `runningTweens`; `CancelSequence()` kills every active tracked tween.

## Time scale

`ignoreTimeScale` (serialized, **true by default**) makes every created sequence run on unscaled time via DOTween's `SetUpdate(true)`, so tweens keep playing regardless of `Time.timeScale` (pauses, slow-motion, hit-stop, etc.). Call `SetIgnoreTimeScale(bool)` to change the mode at runtime — it applies to future sequences and re-applies to any that are already running. Subclasses that build their own sequences should route them through `ApplyTimeMode(sequence)` before returning.
