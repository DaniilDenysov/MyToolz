using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.InventorySystem.Views
{
    public abstract class InventoryHudView<T> : MonoBehaviour, IEventListener where T : ScriptableObject
    {
        [SerializeField] protected List<InventorySlotView<T>> slots;

        private EventBinding<InventoryInitializeEvent<T>> initBinding;
        private EventBinding<InventoryItemUpdatedEvent<T>> itemUpdatedBinding;

        public void Initialize(IReadOnlyDictionary<T, uint> items)
        {
            for (int i = 0; i < slots.Count; i++)
                slots[i].SlotIndex = i;

            foreach (var kvp in items)
                UpdateItem(kvp.Key, kvp.Value);
        }

        public void UpdateItem(T item, uint newAmount)
        {
            foreach (var slot in slots)
            {
                if (slot.AssignedItem != null && slot.AssignedItem == item)
                    slot.UpdateQuantity(newAmount);
            }
        }

        public void Open() { }
        public void Close() { }

        public void RegisterEvents()
        {
            initBinding        = new EventBinding<InventoryInitializeEvent<T>>(OnInitialize);
            itemUpdatedBinding = new EventBinding<InventoryItemUpdatedEvent<T>>(OnItemUpdated);

            EventBus<InventoryInitializeEvent<T>>.Register(initBinding);
            EventBus<InventoryItemUpdatedEvent<T>>.Register(itemUpdatedBinding);

            foreach (var slot in slots)
                slot.OnSlotAssigned += HandleSlotAssigned;
        }

        public void UnregisterEvents()
        {
            EventBus<InventoryInitializeEvent<T>>.Deregister(initBinding);
            EventBus<InventoryItemUpdatedEvent<T>>.Deregister(itemUpdatedBinding);

            foreach (var slot in slots)
                slot.OnSlotAssigned -= HandleSlotAssigned;
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

        private void OnInitialize(InventoryInitializeEvent<T> e)
        {
            Initialize(e.Items);
        }

        private void OnItemUpdated(InventoryItemUpdatedEvent<T> e)
        {
            UpdateItem(e.Item, e.Amount);
        }

        private void HandleSlotAssigned(T item, int slotIndex) { }
    }
}


