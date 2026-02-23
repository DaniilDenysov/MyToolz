# MVP Design Pattern for Unity

A Model-View-Presenter framework providing core interfaces, abstract base classes, and concrete examples for building testable, decoupled UI architectures in Unity.

## Installation

Add the package to your Unity project via the Package Manager using the git URL or by copying the `MVP` folder into your project's `Packages` directory.

## Architecture Overview

```
┌─────────┐       ┌───────────┐       ┌──────┐
│  Model   │◄──────│ Presenter │──────►│ View │
│          │       │           │       │      │
│ Data     │       │ Logic     │       │ UI   │
│ State    │       │ Mediation │       │ I/O  │
│ Notifies │       │ Orchestr. │       │      │
└─────────┘       └───────────┘       └──────┘
```

The **Model** holds data and notifies when it changes. The **View** renders data and forwards user actions. The **Presenter** sits between them — it listens to model changes and updates the view, and listens to view events and updates the model. The view and model never reference each other directly.

## Package Structure

```
Runtime/
├── Model/
│   ├── IModel.cs              # Core model contract with change notification
│   ├── IValidatable.cs        # Validation support for models
│   └── ModelBase.cs           # Abstract base with built-in change events
├── View/
│   ├── IReadOnlyView.cs       # Display-only view contract
│   ├── IInteractableView.cs   # View with user input events
│   ├── ICollectionView.cs     # List/collection rendering contract
│   ├── ViewBase.cs            # MonoBehaviour base for read-only views
│   └── InteractableViewBase.cs# MonoBehaviour base for interactive views
├── Presenter/
│   ├── IPresenter.cs          # Presenter lifecycle contract
│   └── PresenterBase.cs       # Abstract base with enable/disable/dispose
└── Examples/
    ├── TodoApp/               # Todo list example implementation
    │   ├── Model/
    │   │   └── TodoItemModel.cs
    │   ├── View/
    │   │   ├── ITodoListView.cs
    │   │   └── ITodoFormView.cs
    │   └── Presenter/
    │       └── TodoListPresenter.cs
    └── UserProfile/           # User profile example implementation
        ├── Model/
        │   └── UserProfileModel.cs
        ├── View/
        │   ├── IUserProfileView.cs
        │   └── IUserProfileEditView.cs
        └── Presenter/
            ├── UserProfilePresenter.cs
            └── UserProfileEditPresenter.cs
```

## Core API

### Model Layer

**`IModel<T>`** — Base contract for all models. Exposes an `OnChanged` event, a `Clone()` method for creating snapshots, and `Reset()` for returning to defaults.

**`IValidatable`** — Optional interface for models that need input validation. Provides `IsValid()` and `GetValidationErrors()`.

**`ModelBase<T>`** — Abstract class implementing `IModel<T>`. Subclasses call `NotifyChanged()` after mutating state to fire the change event.

```csharp
public class HealthModel : ModelBase<HealthModel>
{
    public int Current { get; private set; }
    public int Max { get; private set; }

    public HealthModel(int max)
    {
        Max = max;
        Current = max;
    }

    public void TakeDamage(int amount)
    {
        Current = Mathf.Max(0, Current - amount);
        NotifyChanged();
    }

    public override HealthModel Clone() =>
        new HealthModel(Max) { Current = Current };

    public override void Reset()
    {
        Current = Max;
        NotifyChanged();
    }
}
```

### View Layer

**`IReadOnlyView<T>`** — For views that only display data. Methods: `Initialize`, `UpdateView`, `Show`, `Hide`, `Destroy`. Exposes `IsVisible`.

**`IInteractableView<T>`** — Extends `IReadOnlyView<T>` with user input events: `OnUserInput`, `OnSubmit`, `OnCancel`, and `SetInteractable`.

**`ICollectionView<T>`** — For list-based UIs. Events: `OnItemSelected`, `OnItemRemoved`. Methods: `PopulateList`, `AddItem`, `RemoveItemAt`, `Clear`.

**`ViewBase<T>`** — Abstract MonoBehaviour implementing `IReadOnlyView<T>`. Manages visibility via `gameObject.SetActive`. Subclass and override `UpdateView`.

**`InteractableViewBase<T>`** — Abstract MonoBehaviour implementing `IInteractableView<T>`. Provides `RaiseUserInput`, `RaiseSubmit`, `RaiseCancel` helpers and requires `BindUIEvents`/`UnbindUIEvents` overrides.

```csharp
public class HealthBarView : ViewBase<HealthModel>
{
    [SerializeField] private Image _fillImage;
    [SerializeField] private TMP_Text _label;

    public override void UpdateView(HealthModel model)
    {
        _fillImage.fillAmount = (float)model.Current / model.Max;
        _label.text = $"{model.Current} / {model.Max}";
    }
}
```

### Presenter Layer

**`IPresenter`** — Lifecycle contract: `Initialize`, `Enable`, `Disable`, and `IDisposable`. Generic variant `IPresenter<TModel, TView>` exposes `Model` and `View`.

**`PresenterBase<TModel, TView>`** — Abstract class handling the full lifecycle. Subclasses override `SubscribeEvents` and `UnsubscribeEvents` for wiring, plus optional hooks `OnInitialize`, `OnEnable`, `OnDisable`, `OnDispose`. Enable/Disable are idempotent. Dispose can only be called once.

```csharp
public class HealthPresenter : PresenterBase<HealthModel, HealthBarView>
{
    public HealthPresenter(HealthModel model, HealthBarView view)
        : base(model, view) { }

    protected override void OnInitialize()
    {
        View.Initialize(Model);
    }

    protected override void SubscribeEvents()
    {
        Model.OnChanged += HandleModelChanged;
    }

    protected override void UnsubscribeEvents()
    {
        Model.OnChanged -= HandleModelChanged;
    }

    private void HandleModelChanged(HealthModel model)
    {
        View.UpdateView(model);
    }
}
```

## Usage

1. **Define a model** by extending `ModelBase<T>`. Add properties, mutation methods that call `NotifyChanged()`, and implement `Clone`/`Reset`.

2. **Define a view interface** extending `IReadOnlyView<T>` or `IInteractableView<T>` with any screen-specific methods.

3. **Implement the view** using `ViewBase<T>` or `InteractableViewBase<T>` as a MonoBehaviour.

4. **Create a presenter** extending `PresenterBase<TModel, TView>`. Wire model events to view updates in `SubscribeEvents`, and handle view events to update the model.

5. **Bootstrap** by creating the model, getting the view reference, constructing the presenter, and calling `Initialize()`.

```csharp
public class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private HealthBarView _healthBarView;

    private HealthPresenter _presenter;

    private void Start()
    {
        var model = new HealthModel(100);
        _presenter = new HealthPresenter(model, _healthBarView);
        _presenter.Initialize();
    }

    private void OnDestroy()
    {
        _presenter?.Dispose();
    }
}
```

## Included Examples

### TodoApp

Demonstrates a list-based workflow with add, remove, and toggle-complete operations. Shows `ICollectionView<T>` usage and model validation via `IValidatable`.

### UserProfile

Demonstrates a read-only display view and a separate edit form with draft-based editing. The edit presenter clones the model into a draft, validates on submit, and only applies changes back to the source model when validation passes. Cancel reverts to the last saved state.

## Design Decisions

- **Views are interfaces first.** Concrete MonoBehaviour implementations depend on interfaces, making presenters unit-testable with mocks.
- **Presenters own the lifecycle.** `Enable`/`Disable` are idempotent and `Dispose` is one-shot, preventing double-subscribe bugs and leaked event handlers.
- **Models push changes.** The `OnChanged` event avoids polling and keeps the presenter reactive.
- **Validation lives on the model.** `IValidatable` keeps business rules with the data they govern, not in the presenter or view.

## Requirements

- Unity 2022.3 or later
- TextMeshPro (for example view implementations)

## License

MIT
