using MyToolz.DesignPatterns.EventBus;
using MyToolz.Utilities.Debug;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyToolz.InventorySystem.Views
{
    public abstract class InventoryCell<T> : MonoBehaviour, IBeginDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler where T : ScriptableObject
    {
        [SerializeField] private Transform placeholder;
        [SerializeField] private Image highlightOverlay;
        [SerializeField] private GameObject emptyStateVisual;

        public T AssignedItem => itemView != null ? itemView.Item : null;
        public bool IsEmpty => AssignedItem == null;
        public IInventoryItemView<T> ItemView => itemView;

        private InventoryItemView<T> itemView;

        public void Initialize(InventoryItemView<T> itemView,T item, uint amount)
        {
            if (itemView == null)
            {
                DebugUtility.LogError("Unable to place null item view in a cell!");
                return;
            }
            this.itemView = itemView;
            itemView.transform.SetParent(placeholder);
            itemView.Initialize(item, amount);
            if (emptyStateVisual != null) emptyStateVisual.SetActive(item == null);
        }

        public void ClearCell()
        {
            if (itemView != null)
            {
                itemView.gameObject.SetActive(false);
                itemView = null;
            }
            if (emptyStateVisual != null) emptyStateVisual.SetActive(true);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            InventoryDragState<T>.SetSourceCell(this);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!InventoryDragState<T>.IsDragging) return;
            if (highlightOverlay != null) highlightOverlay.enabled = false;

            EventBus<InventoryCellDropEvent<T>>.Raise(new InventoryCellDropEvent<T>
            {
                DroppedItem = InventoryDragState<T>.CurrentItem,
                DroppedAmount = InventoryDragState<T>.CurrentAmount,
                TargetItem = AssignedItem,
                TargetAmount = itemView != null ? itemView.Amount : 0,
                SourceCell = InventoryDragState<T>.SourceCell,
                TargetCell = this
            });
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (highlightOverlay != null && InventoryDragState<T>.IsDragging)
                highlightOverlay.enabled = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (highlightOverlay != null)
                highlightOverlay.enabled = false;
        }
    }
}
