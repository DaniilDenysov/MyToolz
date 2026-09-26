# UI Management System

Package: `Assets/Packages/MVP UI ManagementSystem` · Namespace: `MyToolz.UI.Management`
(installer in `MyToolz.Installers`).

The house system for managing screens: a **layered stack** of screens where opening one can
replace, stack on, or blend with what's already shown, with per-screen input mode and
show/hide tweens. Build screens with this instead of toggling GameObjects by hand.

## The three moving parts

- **A screen** is a `UIScreen` or `UISubScreen` MonoBehaviour on a UI GameObject.
- **A layer** (`UILayerSO` asset) groups screens and defines *how* opening that group behaves
  — its `ActivationMode`.
- **`UILayerStateManager`** (bound as a singleton by `UIInstaller`) is the global authority: it
  keeps the stack of active layers and drives each screen's `OnEnter`/`OnExit`.

Screens implement `IUIState` (`IsActive`, `OnEnter()`, `OnExit()`); a root screen also
implements `IUILayer` (adds `Layer`).

## Layers and activation modes

`UILayerSO` (`[CreateAssetMenu … "MyToolz/UI/Layer"]`) carries one field — `ActivationMode`:

| Mode | Behaviour when a screen on this layer opens |
|------|---------------------------------------------|
| `Override` | Clears the entire layer stack (everything exits), then enters this layer. Use for full-screen states (main menu, gameplay HUD). |
| `Additive` | Deactivates the current layer's screens and pushes this layer on top. Use for a menu that hides the HUD but sits above it. |
| `Blend` | Pushes this layer on top **without** exiting the layer below — both stay visible. Use for popups/overlays over a live screen. |

## Screen types

### `UIScreen` — root or nested screen

`UIScreen : UIScreenBase, IUILayer, ISelfValidator`. The workhorse. Inspector config:

- `parent` (a `UIScreen`) — leave empty for a **root** screen; set it for a nested one.
- `layer` (`UILayerSO`) — **required on a root** (hidden on children; a child inherits its
  root's layer via the `Layer` property that walks up the parent chain).
- `enterOnStart` — root opens itself on `Start` (hidden on children).
- `defaultScreen` (`UIScreenBase`) — a child auto-opened when this screen enters.
- `input` (`InputModeSO`) — switched to via `InputStateManager` whenever this screen enters
  (see [input.md](input.md)).
- Base fields (`UIScreenBase`): `screenTweener` (`UITweener` for show/hide — see
  [tweening.md](tweening.md)), `firstSelected` (gamepad focus), `onEnter`/`onExit`
  `UnityEvent`s.

It receives its dependencies by injection:

```csharp
[Inject] void Construct(UILayerStateManager layerStateManager, InputStateManager inputStateManager)
```

So both must be bound (UIInstaller binds the first; an Input Management installer binds the
second). A root registers itself with the layer manager on inject and unregisters in
`OnDestroy`.

### `UISubScreen` — a leaf opened through its parent

`UISubScreen : UIScreenBase, ISelfValidator`. Simpler screen that **must** have a `parent`;
`Open()`/`Close()` route through that parent's local stack. Use for tabs/pages inside a larger
`UIScreen`.

## Opening and closing

Call `Open()` / `Close()` on the screen (e.g. from a `[Button]` or a UI button's `onClick`):

- **Root `UIScreen`** → `Open()` calls `layerStateManager.ChangeState(this)` (applies the
  layer's activation mode); `Close()` calls `layerStateManager.ExitState()`.
- **Child `UIScreen` / `UISubScreen`** → routes through the parent's *local* `UIStateManager`
  (an internal per-screen stack), so sub-screens swap within their parent without touching the
  global layer stack.

Entering a screen also enters its `defaultScreen` and switches to its `input` mode.

## Setup checklist

1. Add `UIInstaller` to a context (binds `UILayerStateManager`). Ensure an Input Management
   installer is present too, since `UIScreen` injects `InputStateManager`.
2. Create `UILayerSO` assets — one per behaviour class (e.g. `HUD` = Blend, `Menu` = Override,
   `Popup` = Additive).
3. On each **root** `UIScreen`, assign its `layer`; set `enterOnStart` on the one that should
   show first.
4. For nested screens, set `parent`; optionally `defaultScreen` and per-screen `input`.
5. Assign a `UITweener` as `screenTweener` for the show/hide animation and `firstSelected`
   for controller navigation.
6. Wire buttons to `Open()`/`Close()`.

## Validation (`ISelfValidator`)

Both screen types self-validate in the Inspector (via `MyToolz.EditorToolz.ISelfValidator`):
a root without a `layer`, a self-referencing `parent`/`defaultScreen`, parent-chain cycles,
`defaultScreen` cycles, and a `UISubScreen` without a parent all surface as inspector errors.
Heed them — each corresponds to a real runtime break (e.g. a screen that can't register, or
an infinite open loop).

## Do / Don't

- ✅ Model every screen as a `UIScreen`/`UISubScreen`; drive visibility through
  `Open()`/`Close()`, not by enabling GameObjects.
- ✅ Choose the `ActivationMode` deliberately: `Override` for exclusive states, `Additive` to
  hide-but-stack, `Blend` for overlays.
- ✅ Give each screen an `input` `InputModeSO` so focus and controls follow the UI.
- ✅ Fix every `ISelfValidator` error before play — they flag real cycles/misconfig.
- ❌ Don't give a child screen its own `layer` or `enterOnStart` — layers are defined by roots.
- ❌ Don't `new` a `UILayerStateManager`; inject the one `UIInstaller` binds so all screens
  share one stack.
