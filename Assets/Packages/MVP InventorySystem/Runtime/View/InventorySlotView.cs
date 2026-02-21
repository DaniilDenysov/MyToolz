using MyToolz.DesignPatterns.EventBus;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyToolz.InventorySystem.Views
{
    public abstract class InventorySlotView<T> : MonoBehaviour, IDropHandler where T : ScriptableObject
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private Image highlightOverlay;

        public int SlotIndex { get; set; }
        public T AssignedItem { get; private set; }

        public event Action<T, int> OnSlotAssigned;

        public void OnDrop(PointerEventData eventData)
        {
            if (!InventoryDragState<T>.IsDragging) return;

            T item = InventoryDragState<T>.CurrentItem;
            uint amount = InventoryDragState<T>.CurrentAmount;

            AssignedItem = item;
            if (quantityText != null)
                quantityText.text = $"{amount}";

            EventBus<InventorySlotDropEvent<T>>.Raise(new InventorySlotDropEvent<T>
            {
                Item = item,
                Amount = amount,
                SlotIndex = SlotIndex
            });

            OnSlotAssigned?.Invoke(item, SlotIndex);
        }

        public void UpdateQuantity(uint amount)
        {
            if (quantityText != null)
                quantityText.text = $"{amount}";
            if (amount == 0)
                ClearSlot();
        }

        public void ClearSlot()
        {
            AssignedItem = null;
            if (icon != null)
            {
                icon.sprite = null;
                icon.enabled = false;
            }
            if (quantityText != null)
                quantityText.text = string.Empty;
        }

        protected virtual void SetIcon(Sprite sprite)
        {
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }
        }
    }
}
