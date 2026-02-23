# MVP InventorySystem

A generic, type-safe inventory package built on the MVP pattern. All communication between Presenter, View, and HudView flows exclusively through the EventBus — there are no direct method calls across layer boundaries. Item definitions are ScriptableObjects. The model owns all data. A settings SO centralises all non-scene configuration. An IO-backed saver persists inventory state across scene loads.

---

## Dependencies

| Package | Role |
|---|---|
| `com.mytoolz.debugutility` | Structured logging |
| `com.mytoolz.eventbus` | All cross-layer communication |
| `com.mytoolz.mvpuimanagementsystem` | `UIScreenBase` for open/close |
| `com.mytoolz.io` | File-based save / load via `SaveLoadBase` |
| Zenject | Dependency injection |
| TextMeshPro | UI labels |

---

## Architecture overview

```
InventorySettingsSO<T>      (ScriptableObject — all non-scene config)
        │ bound by InventoryInstaller
        ▼
InventoryModel<T>           (plain C#, owns dictionary)
        │ OnItemUpdated (C# event — internal to presenter only)
        ▼
InventoryPresenter<T>       (MonoBehaviour, Zenject-wired)
        │
        │ raises ────────────────────────────────────────────────────────────────┐
        │   InventoryInitializeEvent<T>   (once on Start, carries full snapshot) │
        │   InventoryItemUpdatedEvent<T>  (every model change)                   │
        │                                                                         ▼
        │ listens ──────────────────────────────────────────────────────────────┐ │
        │   InventoryItemAmountChangedEvent<T>  (from view, e.g. consume)       │ │
        │                                                                        │ │
        ▼                                                                        │ │
InventoryView<T>      (grid, pooled InventoryCell, listens to bus)  ◄───────────┘ │
        │                                                                          │
        └──► InventoryHudView<T>   (hotbar, serialized field, listens to bus)  ◄──┘

Cell prefab:
  InventoryCell<T>          (drop target, drag-begin bubbling)
    └─ InventoryItemView<T> (display, drag source)

Drag pipeline (EventBus):
  InventoryItemView ──BeginDrag──► InventoryDragController (show ghost)
  InventoryItemView ──Drag──────► InventoryDragController (move ghost)
  InventoryItemView ──EndDrag───► InventoryDragController (hide ghost)

Cell-reorder (EventBus):
  InventoryCell.OnDrop ──► InventoryCellDropEvent ──► InventoryView (swap siblings)
                                                   ──► InventorySaver (update index map)

HUD-slot pipeline:
  InventorySlotView.OnDrop ──► InventorySlotDropEvent + OnSlotAssigned

Save pipeline:
  model.OnItemUpdated ──► InventorySaver rebuilds cache
  InventoryCellDropEvent ──► InventorySaver updates index map
  OnDestroy / Application.quitting ──► InventorySaver.Save() writes JSON
  Scene load ──► InventoryPresenter.Start() ──► saver.LoadIntoModel()
             └──► InventoryInitializeEvent raised after seeding
```

---

## EventBus event reference

| Event | Raised by | Consumed by |
|---|---|---|
| `InventoryInitializeEvent<T>` | `InventoryPresenter` (Start) | `InventoryView`, `InventoryHudView`, `InventorySaver` |
| `InventoryItemUpdatedEvent<T>` | `InventoryPresenter` (model change) | `InventoryView`, `InventoryHudView` |
| `InventoryItemAmountChangedEvent<T>` | `InventoryView` (cell value changed) | `InventoryPresenter` → `model.Remove` |
| `PoolRequest<InventoryCell<T>>` | `InventoryView.SpawnEmptyCells` | ObjectPool |
| `ReleaseRequest<InventoryCell<T>>` | *(unused — cells remain in grid, cleared in place)* | — |
| `InventoryDragBeginEvent<T>` | `InventoryItemView.OnBeginDrag` | `InventoryDragController` |
| `InventoryDragUpdateEvent<T>` | `InventoryItemView.OnDrag` | `InventoryDragController` |
| `InventoryDragEndEvent<T>` | `InventoryItemView.OnEndDrag` | `InventoryDragController` |
| `InventoryCellDropEvent<T>` | `InventoryCell.OnDrop` | `InventoryView` (swap), `InventorySaver` (index) |
| `InventorySlotDropEvent<T>` | `InventorySlotView.OnDrop` | Any listener |

---

## Initialization sequence

```
SceneContext.Awake
  └─ Zenject injects all MonoBehaviours
       └─ InventoryPresenter.Construct() ← model, saver, settings
       └─ InventoryView.Construct()      ← settings (cellPrefab, inventorySize)

MonoBehaviour.Start (all objects)
  └─ InventoryPresenter.Start()
       1. RegisterEvents()               ← subscribe model.OnItemUpdated, bus InventoryItemAmountChangedEvent
       2. Seed model (save / SO / Initialize)
       3. Raise InventoryInitializeEvent { Items = model.InventoryItems }

  └─ InventoryView (OnEnable already registered bus bindings)
       Receives InventoryInitializeEvent
       └─ StartCoroutine(InitializeRoutine)
            Frame 0: SpawnEmptyCells()  ← PoolRequest × inventorySize
            yield return null
            Frame 1: PlaceItemInCell()  ← fills cells from snapshot
                     hudView?.Initialize()

  └─ model.OnItemUpdated (any subsequent Add/Remove)
       └─ InventoryPresenter.OnModelItemUpdated
            └─ Raise InventoryItemUpdatedEvent
                 └─ InventoryView.OnItemUpdated → PlaceItemInCell / ClearCell
                 └─ InventoryHudView.OnItemUpdated → UpdateQuantity
```

The one-frame `yield` guarantees all `PoolRequest` callbacks have completed and `cells` is fully populated before any `PlaceItemInCell` call runs.

---

## Event Listener pattern

All MonoBehaviours that subscribe to events implement `IEventListener`:

| Method | Action |
|---|---|
| `OnEnable` | `RegisterEvents()` |
| `OnDisable` | `UnregisterEvents()` |
| `OnDestroy` | `UnregisterEvents()` |

`InventoryPresenter` additionally uses an `eventsRegistered` guard to prevent double-subscription.

---

## Settings SO — `InventorySettingsSO<T>`

Create one via `Assets → Create → MyToolz → Inventory → Settings`.

| Field | Type | Purpose |
|---|---|---|
| `cellPrefab` | `InventoryCell<T>` | Prefab pooled to fill the fixed-size grid |
| `ghostPrefab` | `GameObject` | Prefab with `Image` — used by drag controller |
| `inventorySize` | `int` | Number of empty cells spawned on initialize |
| `initialItems` | `InitialItem<T>[]` | Items added on first run (skipped when save data exists) |
| `itemCatalog` | `T[]` | All possible items — required for save/load name resolution |

**Item names in `itemCatalog` must be unique.**

---

## Seeding priority

`InventoryPresenter.Start()` seeds the model in this order:

1. **Save data exists** — restore from file.
2. **SO has `initialItems`** — add each entry to the model.
3. **Fallback** — call `model.Initialize()`.

After seeding, `InventoryInitializeEvent` is raised with the full model snapshot.

---

## Layer by layer

### Model — `InventoryModel<T>`

Plain C# serializable class. Owns `Dictionary<T, uint>`. Fires `OnItemUpdated` (C# event, consumed only by `InventoryPresenter`).

```csharp
[Serializable]
public class WeaponInventoryModel : InventoryModel<WeaponItemSO> { }
```

---

### Presenter — `InventoryPresenter<T>`

The only layer that touches the model. Translates model events to bus events and bus events to model calls. Never holds a reference to the view.

```csharp
public class WeaponPresenter : InventoryPresenter<WeaponItemSO> { }
```

---

### Grid view — `InventoryView<T>`

Implements `IEventListener`. Listens to `InventoryInitializeEvent` and `InventoryItemUpdatedEvent`. Raises `InventoryItemAmountChangedEvent` when a cell value changes. Holds `InventoryHudView<T>` as a serialized field; the HudView registers its own bus listeners independently.

```csharp
public class WeaponInventoryView : InventoryView<WeaponItemSO> { }
```

---

### Grid cell — `InventoryCell<T>`

Pooled. Holds a child `InventoryItemView<T>`. Raises `InventoryCellDropEvent` on drop.

```csharp
public class WeaponCell : InventoryCell<WeaponItemSO> { }
```

---

### Item view — `InventoryItemView<T>`

Drag source. Raises `InventoryDragBeginEvent`, `InventoryDragUpdateEvent`, `InventoryDragEndEvent`. Fires `OnValueChanged` C# event (consumed by `InventoryView` which re-raises as `InventoryItemAmountChangedEvent`).

```csharp
public class WeaponItemView : InventoryItemView<WeaponItemSO>
{
    public override void Initialize(WeaponItemSO so, uint amount = 1)
    {
        base.Initialize(so, amount);
        icon.sprite = so.Icon;
    }
}
```

---

### HUD hotbar — `InventoryHudView<T>`

Implements `IEventListener`. Listens to `InventoryInitializeEvent` and `InventoryItemUpdatedEvent` independently — does not need to be driven by `InventoryView`. Serialized as a field on `InventoryView` for lifecycle management, but subscribes to the bus directly.

```csharp
public class WeaponHudView : InventoryHudView<WeaponItemSO> { }
```

---

### Drag controller — `InventoryDragController<T>`

Implements `IEventListener`. Owns a single ghost `Image`. Subscribes to drag events on `OnEnable`, unsubscribes on `OnDisable`/`OnDestroy`.

```csharp
public class WeaponDragController : InventoryDragController<WeaponItemSO> { }
```

---

### Saver — `InventorySaver<T>`

Implements `IEventListener`. Maintains its own `Dictionary<T, int> cellIndexMap` updated via `InventoryInitializeEvent` and `InventoryCellDropEvent` — no dependency on `InventoryView`. Subscribes to `model.OnItemUpdated` to keep the cache current.

```csharp
public class WeaponInventorySaver : InventorySaver<WeaponItemSO> { }
```

---

### Installer — `InventoryInstaller<T, Model>`

Binds model, settings SO, and presenter. The view is a scene MonoBehaviour that self-registers on the EventBus — it does not need to be registered in the DI container.

---

## Step-by-step setup

### 1. Create the item SO

```csharp
[CreateAssetMenu(menuName = "Inventory/Weapon")]
public class WeaponItemSO : ScriptableObject { public Sprite Icon; }
```

### 2. Create the settings SO asset

`Assets → Create → MyToolz → Inventory → Settings`. Configure all fields including `itemCatalog`.

### 3. Subclass all types

```csharp
[Serializable]
public class WeaponInventoryModel    : InventoryModel<WeaponItemSO>            { }
public class WeaponItemView          : InventoryItemView<WeaponItemSO>
{
    public override void Initialize(WeaponItemSO so, uint amount = 1)
    { base.Initialize(so, amount); icon.sprite = so.Icon; }
}
public class WeaponCell              : InventoryCell<WeaponItemSO>              { }
public class WeaponInventoryView     : InventoryView<WeaponItemSO>              { }
public class WeaponHudView           : InventoryHudView<WeaponItemSO>           { }
public class WeaponSlotView          : InventorySlotView<WeaponItemSO>          { }
public class WeaponDragController    : InventoryDragController<WeaponItemSO>    { }
public class WeaponPresenter         : InventoryPresenter<WeaponItemSO>         { }
public class WeaponCellPool          : DefaultObjectPoolInstaller<InventoryCell<WeaponItemSO>> { }
public class WeaponInventorySaver    : InventorySaver<WeaponItemSO>             { }
public class WeaponInventoryInstaller: InventoryInstaller<WeaponItemSO, WeaponInventoryModel> { }
```

### 4. Scene setup

SceneContext MonoInstallers (order matters):

1. `WeaponInventoryInstaller` — assign `settings` SO, configure `model`, assign `presenter`.
2. `WeaponCellPool` — cell object pool.
3. `WeaponInventorySaver` — configure save path/strategy.

Other scene objects (no DI registration required):
- `WeaponInventoryView` — assign `container`, optional `window`, optional `hudView` (serialized field). Settings SO is injected automatically.
- `WeaponHudView` — wire `slots` list. Subscribes to bus independently.
- `WeaponDragController` — assign `rootCanvas`.

### 5. Runtime usage

```csharp
[Inject] private IInventoryPresenter<WeaponItemSO> inventory;

void PickUp(WeaponItemSO w) => inventory.Add(w, 1);
void Drop(WeaponItemSO w)   => inventory.Remove(w, 1);
```
