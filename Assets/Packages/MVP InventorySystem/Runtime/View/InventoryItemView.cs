using MyToolz.DesignPatterns.EventBus;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyToolz.InventorySystem.Views
{
    public interface IInventoryItemView<ItemSO> where ItemSO : ScriptableObject
    {
        public event Action<ItemSO, uint> OnValueChanged;
        public void Initialize(ItemSO inventoryItemSO, uint amount = 1);
    }

    public abstract class InventoryItemView<ItemSO> : MonoBehaviour, IInventoryItemView<ItemSO>,
        IBeginDragHandler, IDragHandler, IEndDragHandler where ItemSO : ScriptableObject
    {
        public event Action<ItemSO, uint> OnValueChanged;

        [SerializeField] protected TMP_Text quantityDisplay;
        [SerializeField] protected TMP_Text nameDisplay;
        [SerializeField] protected Image icon;

        protected ItemSO inventoryItemSO;
        protected uint currentAmount;

        public ItemSO Item => inventoryItemSO;
        public uint Amount => currentAmount;

        public virtual void Initialize(ItemSO inventoryItemSO, uint amount = 1)
        {
            this.inventoryItemSO = inventoryItemSO;
            currentAmount = amount;
            nameDisplay.text = $"{inventoryItemSO.name}";
            quantityDisplay.text = $"{amount}";
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            InventoryDragState<ItemSO>.BeginDrag(inventoryItemSO, currentAmount);

            var parentCell = GetComponentInParent<InventoryCell<ItemSO>>();
            if (parentCell != null)
                InventoryDragState<ItemSO>.SetSourceCell(parentCell);

            EventBus<InventoryDragBeginEvent<ItemSO>>.Raise(new InventoryDragBeginEvent<ItemSO>
            {
                Item = inventoryItemSO,
                Amount = currentAmount,
                SourceTransform = GetComponent<RectTransform>(),
                PointerData = eventData
            });
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            EventBus<InventoryDragUpdateEvent<ItemSO>>.Raise(new InventoryDragUpdateEvent<ItemSO>
            {
                PointerData = eventData
            });
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            InventoryDragState<ItemSO>.EndDrag();
            EventBus<InventoryDragEndEvent<ItemSO>>.Raise(new InventoryDragEndEvent<ItemSO>
            {
                Item = inventoryItemSO,
                Amount = currentAmount,
                PointerData = eventData
            });
        }
    }
}
