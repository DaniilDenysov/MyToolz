using MyToolz.DesignPatterns.EventBus;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MyToolz.InventorySystem.Views
{
    public struct InventoryDragBeginEvent<T> : IEvent where T : ScriptableObject
    {
        public T Item;
        public uint Amount;
        public RectTransform SourceTransform;
        public PointerEventData PointerData;
    }

    public struct InventoryDragUpdateEvent<T> : IEvent where T : ScriptableObject
    {
        public PointerEventData PointerData;
    }

    public struct InventoryDragEndEvent<T> : IEvent where T : ScriptableObject
    {
        public T Item;
        public uint Amount;
        public PointerEventData PointerData;
    }

    public struct InventorySlotDropEvent<T> : IEvent where T : ScriptableObject
    {
        public T Item;
        public uint Amount;
        public int SlotIndex;
    }

    public struct InventoryCellDropEvent<T> : IEvent where T : ScriptableObject
    {
        public T DroppedItem;
        public uint DroppedAmount;
        public T TargetItem;
        public uint TargetAmount;
        public InventoryCell<T> SourceCell;
        public InventoryCell<T> TargetCell;
    }

    public struct InventoryInitializeEvent<T> : IEvent where T : ScriptableObject
    {
        public IReadOnlyDictionary<T, uint> Items;
        public IReadOnlyDictionary<T, int> CellPositions;
    }

    public struct InventoryItemUpdatedEvent<T> : IEvent where T : ScriptableObject
    {
        public T Item;
        public uint Amount;
    }

    public struct InventoryItemAmountChangedEvent<T> : IEvent where T : ScriptableObject
    {
        public T Item;
        public uint Amount;
    }

    public static class InventoryDragState<T> where T : ScriptableObject
    {
        public static T CurrentItem { get; private set; }
        public static uint CurrentAmount { get; private set; }
        public static bool IsDragging { get; private set; }
        public static InventoryCell<T> SourceCell { get; private set; }
        public static bool DropHandled { get; set; }

        public static void BeginDrag(T item, uint amount)
        {
            CurrentItem = item;
            CurrentAmount = amount;
            IsDragging = true;
            DropHandled = false;
        }

        public static void SetSourceCell(InventoryCell<T> cell) => SourceCell = cell;

        public static void EndDrag()
        {
            CurrentItem = null;
            CurrentAmount = 0;
            SourceCell = null;
            IsDragging = false;
            DropHandled = false;
        }
    }
}
