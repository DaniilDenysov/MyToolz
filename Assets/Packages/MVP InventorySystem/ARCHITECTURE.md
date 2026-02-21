# Inventory System Architecture

## Overview
This refactored inventory system follows the MVP (Model-View-Presenter) pattern with complete separation of concerns using an Event Bus for all communication between layers.

## Architecture Components

### 1. Model Layer
- **InventoryModel<T>**: Core data structure maintaining inventory state
- **Responsibility**: Store and manage item quantities
- **Communication**: 
  - Raises `OnItemUpdated` events when inventory changes
  - Receives initialization data from Presenter
- **Independence**: Has NO knowledge of View or UI

### 2. View Layer
- **InventoryView<T>**: Main UI container managing inventory cells
- **InventoryCell<T>**: Individual inventory slot components
- **InventoryItemView<T>**: Item visual representation with drag handling
- **InventoryHudView<T>**: HUD display with quick slots
- **InventorySlotView<T>**: Quick slot UI elements
- **Responsibility**: 
  - Display inventory state
  - Handle user input (drag, drop, interactions)
  - Emit events for user actions
- **Communication**: 
  - Listens to Model updates via EventBus
  - Raises user action events (`InventoryItemAmountChangedEvent`, `InventoryCellDropEvent`, `InventorySlotDropEvent`)
  - NO direct references to Model or Presenter

### 3. Presenter Layer
- **InventoryPresenter<T>**: Bridge between View and Model
- **Responsibility**:
  - Initialize Model with settings data
  - Listen to View events via EventBus
  - Update Model based on user actions
  - Translate Model state changes to View-consumable events
- **Communication**:
  - Injects IInventoryModel<T> and InventorySettingsSO<T>
  - Listens to Model.OnItemUpdated events
  - Registers EventBus listeners for View events
  - Raises InventoryInitializeEvent and InventoryItemUpdatedEvent
  - NO direct View references

### 4. Settings Layer (ScriptableObject)
- **InventorySettingsSO<T>**: Configuration container
- **Holds**:
  - CellPrefab reference
  - GhostPrefab reference
  - Inventory size
  - Initial items array (used for seeding)
  - Item catalog

### 5. Installer Layer
- **InventoryInstaller<T, Model>**: Zenject DI container setup
- **Binds**:
  - IInventoryModel<T> (singleton)
  - InventorySettingsSO<T> (singleton)
  - IInventoryPresenter<T> (singleton)

## Event Flow

### Initialization
```
InventoryInstaller
    ↓
Zenject Container (binds Model, Settings, Presenter)
    ↓
InventoryPresenter.Start()
    ├→ Load from Saver OR
    └→ Call Model.Initialize(settings.InitialItems)
        ↓
    Presenter raises InventoryInitializeEvent
        ↓
    InventoryView listens and spawns cells
    InventoryHudView listens and displays items
```

### User Action (e.g., Item Drop)
```
InventoryItemView.OnEndDrag()
    ↓
InventoryCell.OnDrop()
    ↓
EventBus raises InventoryCellDropEvent
    ↓
InventoryPresenter listens to InventoryCellDropEvent
    ↓
Presenter calls Model.Add/Remove()
    ↓
Model fires OnItemUpdated
    ↓
Presenter raises InventoryItemUpdatedEvent
    ↓
InventoryView listens and updates UI
```

## Key Design Principles

1. **Complete Decoupling**: No component has direct references outside its layer
2. **Event-Driven**: All cross-layer communication through EventBus
3. **Single Responsibility**: Each component has one clear purpose
4. **Dependency Injection**: Zenject manages all dependencies
5. **Settings-Based Initialization**: Initial inventory comes from ScriptableObject settings

## Event Bus Events

### From Model → Presenter
- `OnItemUpdated(T item, uint amount)`: Item quantity changed

### From Presenter → View
- `InventoryInitializeEvent<T>`: Inventory initialized with items
- `InventoryItemUpdatedEvent<T>`: Item amount updated

### From View → Presenter
- `InventoryItemAmountChangedEvent<T>`: User consumed/used item
- `InventoryCellDropEvent<T>`: User dropped item in inventory
- `InventorySlotDropEvent<T>`: User dropped item in quick slot

### Internal View Events
- `InventoryDragBeginEvent<T>`: Drag started
- `InventoryDragUpdateEvent<T>`: Dragging
- `InventoryDragEndEvent<T>`: Drag ended
- `PoolRequest<T>`: Object pooling request

## Implementation Steps

1. Create concrete implementations in `Optional/Implementation/`
2. Configure InventorySettingsSO with initial items
3. Add InventoryInstallerImplementation to scene
4. Attach InventoryViewImplementation to UI prefab
5. All other wiring is automatic via DI

## Benefits of This Architecture

- ✅ Easy to test (each layer independently)
- ✅ Easy to extend (add new view types, model features)
- ✅ Reusable (Model and Presenter work with any View)
- ✅ Flexible (swap implementations without changing core)
- ✅ Maintainable (clear separation of concerns)
