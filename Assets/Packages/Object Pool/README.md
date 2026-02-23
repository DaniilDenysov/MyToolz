# Object Pool

Event-driven object pooling system using the EventBus for pool requests and releases. Integrates with Zenject for installer-based setup.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |
| Event Bus | `com.mytoolz.eventbus` |

External: Zenject.

## Structure

```
Runtime/
├── Events.cs                       PoolRequest<T> and ReleaseRequest<T> event definitions
├── ObjectPoolInstaller.cs          Abstract Zenject MonoInstaller for configuring a typed pool
└── DefaultObjectPoolInstaller.cs   Concrete generic installer for common pooling scenarios
```

## Usage

Subclass `DefaultObjectPoolInstaller<T>` for each pooled type. Add the installer to your SceneContext. Request and release objects through the EventBus:

```csharp
EventBus<PoolRequest<MyPrefab>>.Raise(new PoolRequest<MyPrefab> { ... });
EventBus<ReleaseRequest<MyPrefab>>.Raise(new ReleaseRequest<MyPrefab> { Instance = obj });
```
