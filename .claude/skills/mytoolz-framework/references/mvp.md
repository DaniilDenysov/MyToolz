# MVP (Model – View – Presenter)

Package: `Assets/Packages/MVP` provides the base contracts in
`MyToolz.DesignPatterns.MVP.{Model,View,Presenter}`. Feature packages (`MVP Health System`,
`MVP Clock`, `MVP Game Settings`, `MVP UI ManagementSystem`, `MVP Loading Screen`,
`MVP Inventory System`) build on them.

MVP is the house pattern for any screen or stateful feature. The split keeps game logic out
of MonoBehaviours and out of the UI:

- **Model** — plain C#. Owns state and business logic; fires an event when it changes.
- **View** — renders the model and surfaces user input. Knows nothing about logic.
- **Presenter** — wires Model ↔ View: pushes model changes into the view, turns view input
  into model calls.
- **Installer** — a `MonoInstaller` that binds Model, View (and often the Presenter) into
  Zenject so the Presenter receives them.

## Base contracts

### Model — `IModel<T>` / `ModelBase<T>`

```csharp
public interface IModel<T> where T : IModel<T>
{
    event Action<T> OnChanged;
    T Clone();
    void Reset();
}
```

Extend `ModelBase<T>` and call `NotifyChanged()` after any mutation so subscribers update:

```csharp
using MyToolz.DesignPatterns.MVP.Model;

[System.Serializable]
public class ScoreModel : ModelBase<ScoreModel>
{
    [SerializeField] private int score;
    public int Score => score;

    public void Add(int points) { score += points; NotifyChanged(); }

    public override ScoreModel Clone() => new ScoreModel { score = score };
    public override void Reset()       { score = 0; NotifyChanged(); }
}
```

Models are plain C#; mark them `[Serializable]` (and bind via `[SerializeReference]`) when you
want to author defaults in the Inspector.

### View — `IReadOnlyView<T>` / `ViewBase<T>`

```csharp
public interface IReadOnlyView<T>
{
    void Initialize(T model);
    void Show();
    void UpdateView(T model);
    void Hide();
    void Destroy(T model);
}
```

- `ViewBase<T> : MonoBehaviour` is the default implementation: `Show`/`Hide` toggle the
  GameObject, `IsVisible` reflects it, `Initialize` calls `UpdateView`. Override `UpdateView`
  (abstract) and, if needed, the rest.
- A View can also be a plain `[Serializable]` class referenced by a host component (see
  `ClockView`) — useful when the visuals live inside a larger MonoBehaviour.
- Views that raise user-input events use `IInteractableView` / `InteractableViewBase`;
  list/collection views use `ICollectionView`.

```csharp
using MyToolz.DesignPatterns.MVP.View;
using TMPro;
using UnityEngine;

public class ScoreView : ViewBase<ScoreModel>
{
    [SerializeField] private TMP_Text label;
    public override void UpdateView(ScoreModel model) => label.SetText(model.Score.ToString());
}
```

### Presenter — two sanctioned styles

**Style A — pure C# presenter (`PresenterBase<TModel, TView>`).** For logic-only presenters
resolved through DI. Constructor injection; lifecycle via `IPresenter`
(`Initialize` → `Enable` → `Disable` → `Dispose`). You implement `SubscribeEvents` /
`UnsubscribeEvents` and optional `OnInitialize` / `OnEnable` / `OnDisable` / `OnDispose`.

```csharp
using MyToolz.DesignPatterns.MVP.Presenter;

public class ScorePresenter : PresenterBase<ScoreModel, ScoreView>
{
    public ScorePresenter(ScoreModel model, ScoreView view) : base(model, view) { }

    protected override void OnInitialize() => View.Initialize(Model);
    protected override void SubscribeEvents()   => Model.OnChanged += View.UpdateView;
    protected override void UnsubscribeEvents() => Model.OnChanged -= View.UpdateView;
}
```

**Style B — MonoBehaviour presenter (`[Inject] Construct`).** For presenters that need a
GameObject, Unity lifecycle, or scene references. Receive Model/View via method injection and
implement `IEventListener`; drive `RegisterEvents`/`UnregisterEvents` from the Unity lifecycle.

```csharp
using MyToolz.Events;
using UnityEngine;
using Zenject;

public class ScorePresenter : MonoBehaviour, IEventListener
{
    ScoreModel _model; ScoreView _view;

    [Inject]
    void Construct(ScoreModel model, ScoreView view) { _model = model; _view = view; }

    void OnEnable()  => RegisterEvents();
    void OnDisable() => UnregisterEvents();

    public void RegisterEvents()   => _model.OnChanged += _view.UpdateView;
    public void UnregisterEvents() => _model.OnChanged -= _view.UpdateView;
}
```

Pick **Style A** when the presenter is pure logic; **Style B** when it must live on a
GameObject. Existing packages show both: `TodoListPresenter` (A), `HealthSystemPresenter`,
`SettingsPresenter` (B).

### Installer — `MonoInstaller`

Bind Model and View (and the Presenter, if it's a plain C# one) so injection can resolve them:

```csharp
using Zenject;

public class ScoreInstaller : MonoInstaller
{
    [SerializeReference] private ScoreModel model = new();
    [SerializeField]     private ScoreView view;

    public override void InstallBindings()
    {
        Container.Bind<ScoreModel>().FromInstance(model).AsSingle();
        Container.Bind<ScoreView>().FromInstance(view).AsSingle();
        // Style A presenter would also be bound here, e.g.
        // Container.BindInterfacesTo<ScorePresenter>().AsSingle();
    }
}
```

For a Style B presenter, the MonoBehaviour presenter sits in the scene and Zenject calls its
`[Inject] Construct` automatically — you only bind Model and View. See
[zenject-installers.md](zenject-installers.md) for binding idioms (multiple interfaces to one
instance, `container.Inject(...)`, etc.).

## Wiring rules

- Model raises `OnChanged` (or feature-specific events like `HealthChanged`, `Died`); the
  Presenter subscribes and calls `View.UpdateView`. The View never reads the Model on its own.
- Subscribe and unsubscribe in pairs — `SubscribeEvents`/`UnsubscribeEvents` (Style A) or
  `RegisterEvents`/`UnregisterEvents` (Style B). A Style B presenter should also unregister in
  `OnDestroy`.
- Cross-feature signals still go through `EventBus<T>` (see [event-bus.md](event-bus.md)); MVP
  events are for the Model↔View link within one feature.
- Notable feature to study: `MVP UI ManagementSystem` — `UILayerStateManager` manages a stack
  of `IUILayer` screens with activation modes `Override` / `Additive` / `Blend`.

## Do / Don't

- ✅ Keep all logic in the Model; keep the View dumb (render + emit input events only).
- ✅ Call `NotifyChanged()` after every Model mutation.
- ✅ One installer per feature; bind interfaces so presenters depend on abstractions.
- ❌ Don't put game logic in the View or in the Presenter's rendering code.
- ❌ Don't let the View reach into the Model or another system directly.
