# Singleton

Package: `Assets/Packages/Singleton` · Namespace: `MyToolz.DesignPatterns.Singleton`

A small, safe MonoBehaviour singleton base. **Zenject DI is the default for sharing
dependencies** — reach for a singleton only for enforcing single-instance on objects that DI
can't own (bootstrap objects, things that must exist before any installer runs, systems that
are single *by design*). See [zenject-installers.md](zenject-installers.md) first.

## Two flavours — and the policy for each

There are two derived bases, and they are **not** interchangeable:

### `PrivateSingleton<T>` — the sanctioned one

Enforces "there is exactly one of these" **without exposing a public static `Instance`**. This
is a *must* for systems that are single by design — the Object Pool installers
(`ObjectPoolInstaller<T, P>`) derive from it, for example. It guarantees uniqueness (duplicates
are culled in `Awake`) while keeping the instance private, so nothing can reach in through a
global accessor. Callers still interact with it the right way — through DI, events, or the
component reference they were given — not through a static handle.

```csharp
using MyToolz.DesignPatterns.Singleton;

// Single by design, no global access point. This is the default choice.
public class ProjectilePool : PrivateSingleton<ProjectilePool>
{
    protected override void OnSingletonAwake() { /* init */ }
}
```

### `PublicSingleton<T>` — a red flag, prohibited by default

Exposes `public static T Instance`, i.e. a global access point. That is a design smell in this
codebase: it hides dependencies, defeats DI, and couples callers to a concrete type. **Do not
introduce a `PublicSingleton<T>` unless the user has explicitly asked for global static
access.** When you think you need one, you almost always want DI (`[Inject]`), an
`EventBus<T>` event, or a `PrivateSingleton<T>` instead.

```csharp
// ❌ Avoid — global static access. Only when explicitly requested and justified.
public class AudioRouter : PublicSingleton<AudioRouter>
{
    protected override void OnSingletonAwake() { /* init */ }
}
// AudioRouter.Instance.Play(...);   // the coupling this creates is the problem
```

If a task seems to call for `PublicSingleton<T>`, prefer in this order:
1. **DI** — bind the service in an installer, inject it where needed.
2. **EventBus** — if callers only need to notify/subscribe, not hold a reference.
3. **PrivateSingleton** — if you only need single-instance enforcement, not global access.

Use `PublicSingleton<T>` only when the user states they want a public global instance.

## Shared base behaviour

Both derive from the abstract `Singleton : MonoBehaviour`, which handles the lifecycle:
duplicate culling, registration, and `DontDestroyOnLoad`. Inspector fields (from the base):

- `dontDestroyOnLoad` — survive scene loads.
- `destroyGameObjectOnDuplicate` — when a duplicate appears, destroy the whole GameObject
  (`true`) vs. just the component (`false`, default).

## ⚠️ The critical gotcha: never override `Awake`/`OnDestroy`

The base implements `Awake` and `OnDestroy` to run the duplicate guard and register the
instance. Unity dispatches only the **most-derived** magic method, so if your subclass declares
its own `Awake` (or `OnDestroy`), it **shadows the base one and silently disables the entire
singleton guard** — duplicates won't be culled and the instance won't register.

Override these hooks instead:

```csharp
// ❌ WRONG — breaks the singleton silently
void Awake() { /* ... */ }

// ✅ RIGHT
protected override void OnSingletonAwake() { /* ... */ }   // runs once, on the surviving instance
protected override void OnSingletonDestroy() { /* ... */ } // teardown for that instance
```

## Do / Don't

- ✅ Default to DI; use a singleton only to enforce single-instance where DI can't reach.
- ✅ Prefer `PrivateSingleton<T>` — uniqueness without public exposure.
- ✅ Override `OnSingletonAwake`/`OnSingletonDestroy`.
- ❌ Don't add a `PublicSingleton<T>` unless the user explicitly asks for global static access.
- ❌ Don't declare `Awake`/`OnDestroy` on a subclass.
- ❌ Don't hand-roll a `static Instance` field on a plain MonoBehaviour — use these bases.
