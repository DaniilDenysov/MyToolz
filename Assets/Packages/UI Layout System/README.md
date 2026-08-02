# UI Layout System (WIP)

**Work in progress — version 0.0.1.**

This package currently contains only the ready, reusable **UI elements** extracted from the larger UI Layout System. The full authoring/orchestration layer (layout baking, style/element templates, layout service, screen definitions) is still under development and is deliberately **not** included here yet.

## Included elements

- **UIStrongButton** — a drop-in `Button` subclass that turns silent `onClick` failures (missing target, renamed/removed method, nothing bound) into loud, clickable errors at edit time and runtime. It also queues the click event on top of a `UITweener` OnClick animation when present, so the event fires after the button's press animation instead of instantly.
- **FlexLayoutGroup** — a flexible layout group.
- **SafeAreaFitter** — fits a `RectTransform` to the device safe area.

## Dependencies

- MyToolz.UITweener (and MyToolz.Tweener)
- UniTask
- DOTween
