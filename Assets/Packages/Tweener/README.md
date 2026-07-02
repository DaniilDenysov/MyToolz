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
