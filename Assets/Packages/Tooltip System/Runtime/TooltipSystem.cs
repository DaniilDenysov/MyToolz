using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.Singleton;
using MyToolz.EditorToolz;
using MyToolz.Events;
using MyToolz.UI.Events;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyToolz.UI.Events
{
    public struct ShowTooltip : IEvent
    {
        public string Description;
    }

    public struct HideTooltip : IEvent
    {

    }
}

namespace MyToolz.UI.ToolTip
{
    public class TooltipSystem : Singleton<TooltipSystem>, IEventListener
    {
        [SerializeField, Required] private RectTransform tooltipRoot;
        [SerializeField, Required] private TMP_Text descriptionText;
        [SerializeField] private float screenMargin = 8f;

        private RectTransform rectTransform;
        private string activeDescription;

        private EventBinding<ShowTooltip> showTooltipEventBinding;
        private EventBinding<HideTooltip> hideTooltipEventBinding;

        private void Start()
        {          
            rectTransform = tooltipRoot;
            Hide();
        }

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            UnregisterEvents();
        }

        private void Update()
        {
            if (!tooltipRoot.gameObject.activeSelf || rectTransform == null) return;
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Vector2 pointer = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
            Vector2 size = rectTransform.rect.size;
            Vector2 position = pointer;

            if (position.x + size.x + screenMargin > Screen.width)
                position.x = Screen.width - size.x - screenMargin;

            if (position.y - size.y - screenMargin < 0f)
                position.y = size.y + screenMargin;

            rectTransform.position = position;
        }

        private void Show(ShowTooltip @event)
        {
            string description = @event.Description;
            if (string.IsNullOrWhiteSpace(description)) return;
            if (description == activeDescription) return;

            activeDescription = description;
            descriptionText.text = description;
            rectTransform.pivot = new Vector2(0f, 0.1f);
            tooltipRoot.gameObject.SetActive(true);
            UpdatePosition();
        }

        private void Hide()
        {
            activeDescription = null;
            tooltipRoot.gameObject.SetActive(false);
        }

        public void RegisterEvents()
        {
            showTooltipEventBinding = new(Show);
            EventBus<ShowTooltip>.Register(showTooltipEventBinding);

            hideTooltipEventBinding = new(Hide);
            EventBus<HideTooltip>.Register(hideTooltipEventBinding);
        }

        public void UnregisterEvents()
        {
            EventBus<ShowTooltip>.Deregister(showTooltipEventBinding);
            EventBus<HideTooltip>.Deregister(hideTooltipEventBinding);
        }
    }
}
