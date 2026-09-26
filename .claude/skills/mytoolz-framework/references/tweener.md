# Tweener (base)

Package: `Assets/Packages/Tweener` · Namespace: `MyToolz.Tweener`

The **foundation** for MyToolz's data-driven tweening: a MonoBehaviour that composes a list of
serializable *strategies* into a single DOTween `Sequence`. It's built on **DOTween**
(`DG.Tweening`, external) — the base doesn't invent an animation engine, it standardises how you
author and run one from the Inspector.

Most day-to-day work uses the ready-made **`UITweener`** (see [ui-tweener.md](ui-tweener.md)).
Reach for this base directly only to build a *new* family of tweener components (e.g. a
world-space object tweener) that isn't UI-driven.

## The two pieces

- **`AbstractTweenStrategy`** — `[Serializable]`, one abstract method `Tween GetTween()`. A
  strategy is a self-contained recipe for one tween (fade, move, delay, callback…). Subclass it
  to define reusable steps.
- **`Tweener<T> : MonoBehaviour where T : AbstractTweenStrategy`** — holds a
  `[SerializeReference] T[] tweenStrategies` (a polymorphic list you populate in the Inspector,
  each element a concrete strategy subclass) and turns a set of them into a `Sequence`.

```csharp
using MyToolz.Tweener;
using DG.Tweening;

// A concrete strategy: one reusable tween step.
[System.Serializable]
public class PunchScaleStrategy : AbstractTweenStrategy
{
    [SerializeField] private Transform target;
    [SerializeField] private float duration = 0.2f;
    public override Tween GetTween() => target.DOPunchScale(Vector3.one * 0.1f, duration);
}

// A concrete tweener component that runs strategies of that family.
public class WorldTweener : Tweener<AbstractTweenStrategy> { }
```

## What the base gives every subclass

- **`CreateSequence(List<T> strategies)`** — builds a `DOTween.Sequence`, adding each strategy's
  tween either in **parallel** (`Join`) or **serially** (`Append`) depending on the
  `paralelExecution` flag, applies the time mode, tracks it, and returns it. Null strategies /
  null tweens are skipped. Call `.Play()` on the result.
- **`paralelExecution`** (Inspector) — `true` = all strategies run at once; `false` = one after
  another.
- **`ignoreTimeScale`** (Inspector, default `true`) — runs the sequence on **unscaled time**, so
  tweens keep playing through `Time.timeScale` changes (pause, slow-mo). Toggle at runtime with
  **`SetIgnoreTimeScale(bool)`** — it re-applies to sequences already running.
- **`CancelSequence()`** (protected) — kills every tracked running tween and clears the list.

## Do / Don't

- ✅ Model each animation step as an `AbstractTweenStrategy`; compose them on a `Tweener<T>`
  rather than writing DOTween calls scattered through gameplay code.
- ✅ Keep `ignoreTimeScale` on for UI/menus that must animate while the game is paused.
- ✅ For UI, use `UITweener` — don't re-derive the base unless you need a non-UI trigger model.
- ❌ Don't `new` DOTween sequences ad-hoc in components when a strategy would be reusable and
  Inspector-authored.
- ❌ Don't forget DOTween (`DG.Tweening`) must be present — this package won't compile without it.
