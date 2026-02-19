using MyToolz.EditorToolz;
using MyToolz.Utilities.Debug;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace MyToolz.UI.ToolTip
{
    [RequireComponent(typeof(Image))]
    public class TooltipArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, FoldoutGroup("Config")] private string description;
        [SerializeField, Required, FoldoutGroup("Config")] private Tooltip tooltip;
        private ITooltipPresenter tooltipPresenter;

        [Inject]
        private void Construct(ITooltipPresenter tooltipPresenter)
        {
            this.tooltipPresenter = tooltipPresenter;
            DebugUtility.Log(this, $"[TooltipArea] injected!");
        }

        public void Initialize(string description)
        {
            if (string.IsNullOrEmpty(description) || string.IsNullOrWhiteSpace(description))
            {
                DebugUtility.LogError(this, $"[TooltipArea] unable to initialize, description is null!");
                return;
            }
            this.description = description;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(description) || string.IsNullOrWhiteSpace(description)) return;
            tooltipPresenter?.Show(description);
            DebugUtility.Log(this, $"[TooltipArea] entered!");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltipPresenter?.Hide();
            DebugUtility.Log(this, $"[TooltipArea] exited!");
        }
    }
}
