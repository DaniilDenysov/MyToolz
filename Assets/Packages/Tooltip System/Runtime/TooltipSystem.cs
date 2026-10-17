using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.Singleton;
using MyToolz.EditorToolz;
using MyToolz.Events;
using MyToolz.UI.Events;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    public class TooltipSystem : PrivateSingleton<TooltipSystem>, IEventListener
    {
        [SerializeField, Required] private RectTransform tooltipRoot;
        [SerializeField, Required] private TMP_Text descriptionText;
        [SerializeField, Tooltip("Minimum distance from the screen edge, in screen pixels.")] private float screenMargin = 8f;
        [SerializeField, Tooltip("Tooltip pivot placed at the pointer. (0, 0.1) opens up and to the right.")]
        private Vector2 pivot = new Vector2(0f, 0.1f);

        private readonly Vector3[] corners = new Vector3[4];
        private Canvas canvas;
        private string activeDescription;
        private bool isSingleton;
        private bool eventsRegistered;

        private EventBinding<ShowTooltip> showTooltipEventBinding;
        private EventBinding<HideTooltip> hideTooltipEventBinding;

        protected override void OnSingletonAwake()
        {
            // Everything Show needs is set up before events can arrive (OnEnable runs after Awake).
            isSingleton = true;
            if (tooltipRoot != null)
            {
                canvas = tooltipRoot.GetComponentInParent<Canvas>();
            }
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

        protected override void OnSingletonDestroy()
        {
            UnregisterEvents();
        }

        private void Update()
        {
            if (tooltipRoot == null || !tooltipRoot.gameObject.activeSelf) return;
            UpdatePosition();
        }

        private Camera EventCamera =>
            canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        /// <summary>
        /// Places the tooltip's pivot at the pointer and keeps the whole rect on screen. The rect is
        /// measured in screen pixels and the result converted back into the parent's local space, so
        /// it works for scaled canvases and for Screen Space - Camera / World Space canvases.
        /// </summary>
        private void UpdatePosition()
        {
            Camera cam = EventCamera;
            Vector2 pointer = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;

            tooltipRoot.GetWorldCorners(corners);
            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            Vector2 size = new Vector2(Mathf.Abs(topRight.x - bottomLeft.x), Mathf.Abs(topRight.y - bottomLeft.y));
            Vector2 rectPivot = tooltipRoot.pivot;

            Vector2 target = pointer;
            float left = target.x - rectPivot.x * size.x;
            float right = left + size.x;
            float bottom = target.y - rectPivot.y * size.y;
            float top = bottom + size.y;

            if (right + screenMargin > Screen.width) target.x -= right + screenMargin - Screen.width;
            if (left - screenMargin < 0f) target.x += screenMargin - left;
            if (top + screenMargin > Screen.height) target.y -= top + screenMargin - Screen.height;
            if (bottom - screenMargin < 0f) target.y += screenMargin - bottom;

            if (tooltipRoot.parent is RectTransform parent &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, target, cam, out Vector2 local))
            {
                tooltipRoot.localPosition = new Vector3(local.x, local.y, tooltipRoot.localPosition.z);
            }
            else
            {
                tooltipRoot.position = target;
            }
        }

        private void Show(ShowTooltip @event)
        {
            if (tooltipRoot == null || descriptionText == null) return;

            string description = @event.Description;
            if (string.IsNullOrWhiteSpace(description)) return;
            if (description == activeDescription) return;

            activeDescription = description;
            descriptionText.text = description;
            tooltipRoot.pivot = pivot;
            tooltipRoot.gameObject.SetActive(true);

            // Measure the rect for the new text now rather than on the next layout pass.
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRoot);
            UpdatePosition();
        }

        private void Hide()
        {
            activeDescription = null;
            if (tooltipRoot != null)
            {
                tooltipRoot.gameObject.SetActive(false);
            }
        }

        public void RegisterEvents()
        {
            // A duplicate instance (destroyed by the singleton guard at the end of the frame) never listens.
            if (!isSingleton || eventsRegistered) return;

            showTooltipEventBinding = new(Show);
            EventBus<ShowTooltip>.Register(showTooltipEventBinding);

            hideTooltipEventBinding = new(Hide);
            EventBus<HideTooltip>.Register(hideTooltipEventBinding);
            eventsRegistered = true;
        }

        public void UnregisterEvents()
        {
            if (!eventsRegistered) return;

            EventBus<ShowTooltip>.Deregister(showTooltipEventBinding);
            EventBus<HideTooltip>.Deregister(hideTooltipEventBinding);
            eventsRegistered = false;
        }
    }
}
