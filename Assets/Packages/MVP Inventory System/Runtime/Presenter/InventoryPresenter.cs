using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.InventorySystem.Models;
using MyToolz.InventorySystem.Persistance;
using MyToolz.InventorySystem.Settings;
using MyToolz.InventorySystem.Views;
using MyToolz.IO;
using MyToolz.Utilities.Debug;
using UnityEngine;
using Zenject;

namespace MyToolz.InventorySystem.Presenters
{
    public interface IInventoryPresenter<T> where T : ScriptableObject
    {
        public void Add(T inventoryItemSO, uint amount = 1);
        public void Remove(T inventoryItemSO, uint amount = 1);
    }

    public abstract class InventoryPresenter<T> : MonoBehaviour, IEventListener, IInventoryPresenter<T> where T : ScriptableObject
    {
        protected IInventoryModel<T> model;
        protected IInventorySaver<T> saver;
        protected InventorySettingsSO<T> settings;

        private EventBinding<InventoryItemAmountChangedEvent<T>> itemAmountChangedBinding;
        private EventBinding<InventorySlotDropEvent<T>> slotDropBinding;
        private EventBinding<InventoryCellDropEvent<T>> cellDropBinding;

        private bool eventsRegistered;

        [Inject]
        private void Construct(IInventoryModel<T> model,[InjectOptional] IInventorySaver<T> saver,InventorySettingsSO<T> settings)
        {
            UnregisterEvents();
            this.model = model;
            this.saver = saver;
            this.settings = settings;
            if (isActiveAndEnabled) RegisterEvents();
        }

        private void Start()
        {
            if (model == null)
            {
                DebugUtility.LogError(this, "Inventory model was not injected; the inventory cannot initialize.");
                return;
            }

            // A decoded save wins even when it is empty: an empty inventory is valid progress and must
            // not be refilled with starter items. Missing (and unreadable) saves fall back to them; the
            // saver refuses to overwrite an unreadable save unless configured to.
            if (saver != null && saver.TryRestoreModel())
            {
                DebugUtility.Log(this, $"Restored inventory with {model.InventoryItems.Count} stacks.");
            }
            else
            {
                if (saver != null && saver.LastLoadStatus == LoadStatus.Unreadable)
                    DebugUtility.LogError(this, "Inventory save is unreadable; starting from the initial items.");

                model.Initialize(settings?.InitialItems);
                DebugUtility.Log(this, $"Initialized inventory with size: {settings?.InitialItems?.Length ?? 0}");
            }

            EventBus<InventoryInitializeEvent<T>>.Raise(new InventoryInitializeEvent<T>
            {
                Items = model.InventoryItems,
                CellPositions = saver != null ? saver.GetCellPositions() : null
            });
        }

        protected virtual void OnEnable()
        {
            RegisterEvents();
        }

        protected virtual void OnDisable()
        {
            UnregisterEvents();
        }

        protected virtual void OnDestroy()
        {
            UnregisterEvents();
        }

        private void OnModelItemUpdated(T itemSO, uint amount)
        {
            EventBus<InventoryItemUpdatedEvent<T>>.Raise(new InventoryItemUpdatedEvent<T>
            {
                Item = itemSO,
                Amount = amount
            });
        }

        private void OnItemAmountChangedFromView(InventoryItemAmountChangedEvent<T> e)
        {
            model.Remove(e.Item, e.Amount);
        }

        private void OnSlotDropFromView(InventorySlotDropEvent<T> e)
        {
            if (e.Item != null)
                model.Add(e.Item, e.Amount);
        }

        private void OnCellDropFromView(InventoryCellDropEvent<T> e)
        {
        }

        public void Add(T inventoryItemSO, uint amount = 1)
        {
            model.Add(inventoryItemSO, amount);
        }

        public void Remove(T inventoryItemSO, uint amount = 1)
        {
            model.Remove(inventoryItemSO, amount);
        }

        public void RegisterEvents()
        {
            // Idempotent: OnEnable, injection and manual calls may all request registration.
            if (eventsRegistered || model == null) return;

            model.OnItemUpdated += OnModelItemUpdated;

            itemAmountChangedBinding = new EventBinding<InventoryItemAmountChangedEvent<T>>(OnItemAmountChangedFromView);
            EventBus<InventoryItemAmountChangedEvent<T>>.Register(itemAmountChangedBinding);

            slotDropBinding = new EventBinding<InventorySlotDropEvent<T>>(OnSlotDropFromView);
            EventBus<InventorySlotDropEvent<T>>.Register(slotDropBinding);

            cellDropBinding = new EventBinding<InventoryCellDropEvent<T>>(OnCellDropFromView);
            EventBus<InventoryCellDropEvent<T>>.Register(cellDropBinding);

            eventsRegistered = true;
        }

        public void UnregisterEvents()
        {
            if (!eventsRegistered) return;

            if (model != null) model.OnItemUpdated -= OnModelItemUpdated;

            EventBus<InventoryItemAmountChangedEvent<T>>.Deregister(itemAmountChangedBinding);
            EventBus<InventorySlotDropEvent<T>>.Deregister(slotDropBinding);
            EventBus<InventoryCellDropEvent<T>>.Deregister(cellDropBinding);

            eventsRegistered = false;
        }
    }
}
