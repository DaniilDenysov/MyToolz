# UI Tweener

Package: `Assets/Packages/UI Tweener` · Namespace: `MyToolz.Tweener.UI`

The concrete, trigger-driven tweener for UI. `UITweener : Tweener<TweenStrategy>` (see
[tweener.md](tweener.md)) runs a list of DOTween strategies **grouped by an activation trigger**
— Enable, Disable, OnClick, hover, etc. — so a single component animates a panel in/out, reacts
to pointer events, and gates interaction during the transition. This is the `screenTweener` the
UI Management System drives on every screen (see [ui-management-system.md](ui-management-system.md)).

## How it works

Put a `UITweener` on a UI GameObject and fill its `tweenStrategies` list (a `[SerializeReference]`
polymorphic list — each element is a concrete `TweenStrategy` you choose in the Inspector). Every
strategy carries a **`trigger`** (`ActivationTrigger`); on `Awake`, `UITweener` buckets the
strategies by trigger, and each trigger fires its bucket as one sequence.

`ActivationTrigger` values:

| Trigger | Fires when |
|---------|-----------|
| `Awake` / `Start` / `Enable` / `Disable` | The matching Unity lifecycle moment. |
| `OnClick` / `OnEnter` / `OnExit` | Pointer click / hover-in / hover-out (`IPointer*Handler`). |
| `Manual` | Only when you call it in code. |

### Driving it from code

```csharp
using MyToolz.Tweener.UI;

[SerializeField] private UITweener panelTweener;

panelTweener.SetActive(true);   // plays the Enable bucket, then activates the GameObject
panelTweener.SetActive(false);  // plays the Disable bucket, THEN deactivates on complete
```

`SetActive(false)` waits for the `Disable` sequence to finish before deactivating the GameObject
(if there's no `Disable` strategy it deactivates immediately). This is exactly how a UI screen
hides itself with an out-animation.

### Interaction gating

Assign a `CanvasGroup`; with `blockInteractionDuringTween` on (default), the tweener sets
`interactable`/`blocksRaycasts` to `false` while a sequence runs and restores them on
complete/kill — so users can't click a panel mid-transition. Starting a new trigger cancels the
currently running sequence first.

## The strategy toolbox

Each strategy is a `TweenStrategy` (adds `trigger` + `inverseIfReached`). Visual strategies pull
their timing/values from a **data ScriptableObject** (author once, reuse everywhere) and
reference the target component:

| Strategy | Data SO (`MyToolz/UITweener/…`) | Animates |
|----------|----------------------------------|----------|
| `FadeTweenStrategy` | `FadeTweenSO` (from/to alpha, duration, ease) | a `CanvasGroup`'s alpha |
| `MoveTweenStrategy` | `MoveTweenSO` | a `RectTransform` between `from`/`to` anchors |
| `OffsetTweenStrategy` | `OffsetTweenSO` | anchored offset min/max |
| `ScaleTweenStrategy` | `ScaleTweenSO` | `localScale` |
| `RotateTweenStrategy` | `RotateTweenSO` | rotation |
| `SizeTweenStrategy` | `SizeTweenSO` (or inline size) | `sizeDelta` |
| `PulsateTweenStrategy` | `PulsateTweenSO` (`Scale`/`SizeDelta`/`Fade` mode, loops) | looping pulse |
| `PulsateFadeTweenStrategy` | — | looping fade pulse |

Control-flow / composite strategies (no data SO):

- **`DelayTweenStrategy`** — inserts a delay.
- **`LoopTweenStrategy`** — sets loop count/`LoopType` on the sequence.
- **`MergeTweenStrategy`** — nests its own `innerStrategies`, run `parallel` or serial, as one
  step (only the *parent's* trigger matters).
- **`JoinUITweenerStrategy`** — plays *another* `UITweener`'s sequence for the same trigger, so
  one tweener can orchestrate others.
- **`OnCompleteCallbackTweenStrategy`** — invokes a `UnityEvent` at that point in the sequence.
- **`SelectObjectTweenStrategy`** — sets the `EventSystem` selected object (controller focus).
- `PlaySFXTweenStrategy` — present but commented out (re-enable once the Audio package is wired
  in; prefer raising `PlayAudioClipSO` — see [audio.md](audio.md)).

`inverseIfReached` flips a strategy's from/to each time it completes, so repeated triggers
ping-pong (e.g. a fade that alternates in/out on each activation).

## Authoring checklist

1. Add `UITweener` to the UI object; assign its `CanvasGroup` if you want interaction gating.
2. Add strategies to `tweenStrategies`, picking a concrete type per element and its `trigger`.
3. For visual strategies, create/assign the matching data SO (`FadeTweenSO`, `MoveTweenSO`, …)
   and the target component (CanvasGroup/RectTransform).
4. Play automatically via lifecycle/pointer triggers, or in code via `SetActive` / a `Manual`
   trigger. For screens, hand this `UITweener` to the `UIScreen`'s `screenTweener`.

## Do / Don't

- ✅ Group animations by `ActivationTrigger`; let Enable/Disable buckets be the panel's in/out.
- ✅ Reuse tuned values through the data SOs instead of re-typing durations per object.
- ✅ Use `SetActive(false)` (not `gameObject.SetActive(false)`) so the out-animation plays.
- ✅ Assign a `CanvasGroup` to block clicks during transitions.
- ❌ Don't hand-write DOTween sequences on UI when a strategy + trigger expresses it declaratively.
- ❌ Don't deactivate a tweened GameObject directly — you'll skip its `Disable` animation.
