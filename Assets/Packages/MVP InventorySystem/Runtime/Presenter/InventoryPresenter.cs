using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.InventorySystem.Models;
using MyToolz.InventorySystem.Persistance;
using MyToolz.InventorySystem.Settings;
using MyToolz.InventorySystem.Views;
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

        [Inject]
        private void Construct(IInventoryModel<T> model,[InjectOptional] IInventorySaver<T> saver,InventorySettingsSO<T> settings)
        {
            this.model = model;
            this.saver = saver;
            this.settings = settings;
        }

        private void Start()
        {
            RegisterEvents();

            if (saver != null && saver.HasSaveData())
            {
                saver.LoadIntoModel();
            }
            else
            {
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
            if (model == null) return;

            model.OnItemUpdated += OnModelItemUpdated;

            itemAmountChangedBinding = new EventBinding<InventoryItemAmountChangedEvent<T>>(OnItemAmountChangedFromView);
            EventBus<InventoryItemAmountChangedEvent<T>>.Register(itemAmountChangedBinding);

            slotDropBinding = new EventBinding<InventorySlotDropEvent<T>>(OnSlotDropFromView);
            EventBus<InventorySlotDropEvent<T>>.Register(slotDropBinding);

            cellDropBinding = new EventBinding<InventoryCellDropEvent<T>>(OnCellDropFromView);
            EventBus<InventoryCellDropEvent<T>>.Register(cellDropBinding);
        }

        public void UnregisterEvents()
        {
            if (model == null) return;

            model.OnItemUpdated -= OnModelItemUpdated;

            EventBus<InventoryItemAmountChangedEvent<T>>.Deregister(itemAmountChangedBinding);
            EventBus<InventorySlotDropEvent<T>>.Deregister(slotDropBinding);
            EventBus<InventoryCellDropEvent<T>>.Deregister(cellDropBinding);
        }
    }
}

