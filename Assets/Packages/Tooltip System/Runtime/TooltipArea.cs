using MyToolz.DesignPatterns.EventBus;
using MyToolz.UI.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyToolz.UI.ToolTip
{
    [RequireComponent(typeof(Graphic))]
    public class TooltipArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string description;

        private bool hovered;

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            EventBus<ShowTooltip>.Raise(new ShowTooltip() { Description = description } );
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            EventBus<HideTooltip>.Raise(new HideTooltip());
        }

        // Pointer-exit never arrives for an area that is disabled or destroyed while hovered.
        private void OnDisable()
        {
            if (!hovered) return;
            hovered = false;
            EventBus<HideTooltip>.Raise(new HideTooltip());
        }
    }
}
