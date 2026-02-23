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

        public void OnPointerEnter(PointerEventData eventData)
        {
            EventBus<ShowTooltip>.Raise(new ShowTooltip() { Description = description } );
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            EventBus<HideTooltip>.Raise(new HideTooltip());
        }
    }
}
