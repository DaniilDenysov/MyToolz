# UI Kit

Ready-made uGUI widgets extracted from FindMe. Every component is self-contained and has no game-specific
dependencies.

## Dependencies

| Package | ID |
|---|---|
| Audio | `com.mytoolz.audio` |
| Editor Toolz | `com.mytoolz.editortoolz` |
| Event Bus | `com.mytoolz.eventbus` |
| MVP Game Settings | `com.mytoolz.mvpgamesettings` |
| Tweener / UI Tweener | `com.mytoolz.tweener`, `com.mytoolz.uitweener` |
| UI Layout System | `com.mytoolz.uilayoutsystem` |

External: DOTween (+ its UI/Sprite modules), TextMeshPro, SerializeReferenceExtensions.

## Structure

```
Runtime/
├── ArcLayout.cs             Lays children out on an arch (optional tilt, centre boost, invert)
├── CardCarousel.cs          Abstract swipe/snap carousel of cards with page dots and choose callback
├── CarouselCard.cs          Serializable card slot description used by CardCarousel
├── TextBoxFitter.cs         Fits TMP text in a box: shrinks the font (readable -> smallest) and sizes the box between min/max height
├── FitContentToHeight.cs    Scales content down to fit short screens (never below a minimum)
├── UIShine.cs               Periodic light streak sweeping across a Graphic
├── UISparkleBurst.cs        One-shot sparkle burst for UI (RectTransform) targets
├── WorldSparkleBurst.cs     One-shot sparkle burst for world-space (SpriteRenderer) targets
├── UISpinner.cs             Continuous or stepped spinner on unscaled time
├── UIStrongToggle.cs        Toggle bound to a BoolSettingSO with sprite skin, audio and tween hooks
├── SwitchButton.cs          UIStrongButton that cycles an IntSettingSO through a pluggable strategy
└── SwitchButtonStrategy.cs  Base class for SwitchButton behaviours ([SerializeReference] + SubclassSelector)
Editor/
├── UIStrongToggleEditor.cs  Inspector for UIStrongToggle (setting, icon skin, audio)
└── SwitchButtonEditor.cs    Inspector for SwitchButton on top of UIStrongButtonEditor
```

The indeterminate loading ring that came with this kit lives in the MVP Loading Screen package
(`MyToolz.UI.LoadingScreen.IndeterminateLoadingRing`) because it implements that package's `IProgressBar`.

## Usage

- Add components from **Add Component > MyToolz > UI Kit**.
- `CardCarousel<TCard>` is abstract: derive a concrete carousel for your card type, fill its `modes` array,
  and implement `Choose(int index)` (called when the centred card is tapped). Override `StartIndex` to pick the
  first centred card and `Busy` to block input while something else is running. `Select`/`SnapTo` move it from code.
  Assign the optional `chooseButton` (a Play button, say) to choose the centred card from outside the row as well;
  it calls `ChooseCurrent()`, which you can also call from code.
- `SwitchButton` needs a `SwitchButtonStrategy` subclass (choose it in the inspector). A strategy exposes the
  icons to cycle, the current index, `Select(index)`, and change notifications; the optional `IntSettingSO`
  persists the choice through MVP Game Settings.
- `UIStrongToggle` mirrors a `BoolSettingSO` both ways and swaps between the on/off sprites; the optional
  `AudioClipSO` fields play through the Audio package's `PlayAudioClipSO` event.

These scripts keep the GUIDs they had in FindMe, so scenes and prefabs that used the FindMe copies keep
their references after switching to this package.
