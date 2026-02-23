using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.InventorySystem.Views
{
    public abstract class InventoryDragController<T> : MonoBehaviour, IEventListener where T : ScriptableObject
    {
        [SerializeField] private Canvas rootCanvas;

        private RectTransform draggedTransform;
        private Transform originalParent;
        private CanvasGroup draggedCanvasGroup;

        private EventBinding<InventoryDragBeginEvent<T>> beginBinding;
        private EventBinding<InventoryDragUpdateEvent<T>> updateBinding;
        private EventBinding<InventoryDragEndEvent<T>> endBinding;

        public void RegisterEvents()
        {
            beginBinding  = new EventBinding<InventoryDragBeginEvent<T>>(OnDragBegin);
            updateBinding = new EventBinding<InventoryDragUpdateEvent<T>>(OnDragUpdate);
            endBinding    = new EventBinding<InventoryDragEndEvent<T>>(OnDragEnd);
            EventBus<InventoryDragBeginEvent<T>>.Register(beginBinding);
            EventBus<InventoryDragUpdateEvent<T>>.Register(updateBinding);
            EventBus<InventoryDragEndEvent<T>>.Register(endBinding);
        }

        public void UnregisterEvents()
        {
            EventBus<InventoryDragBeginEvent<T>>.Deregister(beginBinding);
            EventBus<InventoryDragUpdateEvent<T>>.Deregister(updateBinding);
            EventBus<InventoryDragEndEvent<T>>.Deregister(endBinding);
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

        private void OnDragBegin(InventoryDragBeginEvent<T> e)
        {
            if (e.SourceTransform == null || rootCanvas == null) return;

            draggedTransform = e.SourceTransform;
            originalParent = draggedTransform.parent;

            draggedCanvasGroup = draggedTransform.GetComponent<CanvasGroup>();
            if (draggedCanvasGroup != null)
            {
                draggedCanvasGroup.alpha = 0.6f;
                draggedCanvasGroup.blocksRaycasts = false;
            }

            draggedTransform.SetParent(rootCanvas.transform, true);
            MoveToPointer(e.PointerData.position, e.PointerData.pressEventCamera);
        }

        private void OnDragUpdate(InventoryDragUpdateEvent<T> e)
        {
            if (draggedTransform == null) return;
            MoveToPointer(e.PointerData.position, e.PointerData.pressEventCamera);
        }

        private void OnDragEnd(InventoryDragEndEvent<T> e)
        {
            if (draggedTransform == null) return;

            if (draggedCanvasGroup != null)
            {
                draggedCanvasGroup.alpha = 1f;
                draggedCanvasGroup.blocksRaycasts = true;
            }

            if (InventoryDragState<T>.DropHandled)
            {
                draggedTransform.gameObject.SetActive(false);
            }
            else
            {
                draggedTransform.SetParent(originalParent, false);
                draggedTransform.localPosition = Vector3.zero;
            }

            draggedTransform = null;
            originalParent = null;
            draggedCanvasGroup = null;
        }

        private void MoveToPointer(Vector2 screenPos, Camera cam)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.GetComponent<RectTransform>(),
                screenPos, cam,
                out Vector2 localPoint);
            draggedTransform.localPosition = localPoint;
        }
    }
}

