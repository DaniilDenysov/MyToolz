# Zenject & Installers

**Zenject is the only dependency-injection mechanism in this project.** No service locators,
no `FindObjectOfType`, no hand-rolled global statics (the `Singleton<T>` bases are the single
exception, and even those are constrained — see [singleton.md](singleton.md)). If a type needs
a collaborator, bind it in an installer and inject it.

## The three places bindings live (contexts)

Zenject resolves against a container built by a *context*. You rarely write these — you attach
installers to them:

- **ProjectContext** — one per project, created from `Resources/ProjectContext`. Bindings here
  live for the whole run (survive scene loads). Use for truly global services.
- **SceneContext** — one per scene. Bindings live for that scene. The common home for feature
  installers.
- **GameObjectContext** — scopes a container to one GameObject subtree (e.g. a single enemy or
  screen), so each instance gets its own Model/View/Presenter. Use when a feature is
  instantiated many times.

## Writing an installer

The house default is `MonoInstaller` (add it to a context's *Installers* list, or drop it on a
GameObjectContext). Override `InstallBindings()`.

```csharp
using Zenject;
using UnityEngine;
using MyToolz.EditorToolz;

public class EnemyCombatInstaller : MonoInstaller
{
    [SerializeField, Required] private EnemyCombatSO combatSO;
    [SerializeField, Required] private EnemyCombatPresenter presenter; // MonoBehaviour in scene
    [SerializeReference]       private EnemyModel model = new();

    public override void InstallBindings()
    {
        model.Construct(combatSO);

        // One instance, exposed under several interfaces:
        Container.Bind<IEnemyModel>().FromInstance(model).AsSingle();
        Container.Bind<IReadOnlyEnemyModel>().FromInstance(model).AsSingle();

        Container.Bind<IEnemyCombatPresenter>().FromInstance(presenter).AsSingle();
    }
}
```

Note the repo conventions here: serialized dependencies use `[SerializeField, Required]` (the
`[Required]` attribute is from `MyToolz.EditorToolz`), plain-C# models are authored with
`[SerializeReference]`, and the same instance is bound under both its writable and read-only
interfaces so consumers can depend on the narrowest one.

## Binding idioms used in this codebase

```csharp
// Bind an interface to a serialized/instantiated instance (most common here):
Container.Bind<IHealthModel>().FromInstance(healthModel).AsSingle();

// Bind a fresh instance:
Container.Bind<UILayerStateManager>().FromInstance(new UILayerStateManager()).AsSingle();

// Bind interface -> concrete, Zenject constructs it:
Container.Bind<IFoo>().To<Foo>().AsSingle();

// Bind a class under all its interfaces (good for PresenterBase / IInitializable / IDisposable):
Container.BindInterfacesTo<ScorePresenter>().AsSingle();
Container.BindInterfacesAndSelfTo<ScorePresenter>().AsSingle();

// Object pools (see object-pool.md — you usually don't write this by hand):
Container.BindMemoryPool<T, Pool<T>>() /* … */ ;
```

- `AsSingle()` — one shared instance (the default choice here). `AsTransient()` — a new one per
  request. `AsCached()` — one per binding.
- To inject into an object you created **outside** DI (e.g. a serialized instance), call
  `container.Inject(instance)`. `HealthSystemPresenter` does this for its model and view.

## Resolving dependencies

**MonoBehaviours use method injection** — the strong convention in this repo. Add a `Construct`
method marked `[Inject]`; Zenject calls it after the object is created:

```csharp
public class SettingsPresenter : MonoBehaviour
{
    ISaver<SavableData> _saver;

    [Inject]
    void Construct(ISaver<SavableData> saver) => _saver = saver;
}
```

- Prefer method injection over `[Inject]` fields — it's explicit and testable.
- **Plain C# classes** can use constructor injection (see `PresenterBase` subclasses); Zenject
  supplies the arguments when it builds them.
- Depend on the **interface** (`ISaver<T>`, `IHealthModel`), never the concrete implementation.

## Special installers in MyToolz

- `SaveLoadBase<T>` **is itself a `MonoInstaller`** — subclassing it and adding it to a context
  both configures the saver and binds `ISaver<T>` to it. See [io.md](io.md).
- The Object Pool installers (`DefaultObjectPoolInstaller<T>`) bind Zenject `MemoryPool`s per
  prefab. See [object-pool.md](object-pool.md).

## Do / Don't

- ✅ One installer per feature; bind interfaces, resolve via `[Inject] Construct`.
- ✅ Use `[SerializeField, Required]` for scene/asset references an installer needs.
- ✅ Scope with GameObjectContext when a feature is instantiated multiple times.
- ❌ No `FindObjectOfType`, service locators, or manual singletons for things DI can provide.
- ❌ Don't inject concrete types where an interface exists.
- ❌ Don't `new` up a dependency that should be bound and injected.
