using System;
using DG.Tweening;
using MyToolz.UI.Layout;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyToolz.UI.Kit
{
    public abstract class CardCarousel<TCard> : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        where TCard : CarouselCard
    {
        [SerializeField] private TCard[] modes = Array.Empty<TCard>();

        [Tooltip("Optional button (a Play button, say) that chooses whichever card is centred, as tapping that card does.")]
        [SerializeField] private UIStrongButton chooseButton;

        [Header("Layout")]
        [Tooltip("Distance between the centres of neighbouring cards, in canvas units.")]
        [SerializeField, Min(1f)] private float spacing = 680f;

        [SerializeField, Range(0.1f, 1f)] private float sideScale = 0.8f;

        [SerializeField, Range(0f, 1f)] private float sideAlpha = 0.5f;

        [Header("Swipe")]
        [Tooltip("Drag distance, in canvas units, that moves to the next card.")]
        [SerializeField, Min(0f)] private float swipeThreshold = 90f;

        [Tooltip("How much of a drag past the first or last card still moves the row.")]
        [SerializeField, Range(0f, 1f)] private float edgeResistance = 0.3f;

        [SerializeField, Min(0f)] private float snapDuration = 0.35f;

        [SerializeField] private Ease snapEase = Ease.OutBack;

        [Header("Dots")]
        [SerializeField] private Color idleDot = new Color(0.79f, 0.77f, 0.84f, 1f);

        [SerializeField] private Vector2 idleDotSize = new Vector2(26f, 26f);

        [SerializeField] private Vector2 activeDotSize = new Vector2(64f, 26f);

        private CanvasGroup[] groups;
        private UnityAction[] clicks;
        private Canvas canvas;
        private Tween snap;
        private float position;
        private float dragOrigin;
        private float dragDistance;
        private int current;

        public int Count => modes.Length;

        public int Current => current;

        public float Position => position;

        protected TCard[] Cards => modes;

        protected virtual bool Busy => false;

        protected virtual int StartIndex => 0;

        protected abstract void Choose(int index);

        protected virtual void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            groups = new CanvasGroup[modes.Length];
            clicks = new UnityAction[modes.Length];

            for (int i = 0; i < modes.Length; i++)
            {
                TCard card = modes[i];

                if (card.Slot != null && !card.Slot.TryGetComponent(out groups[i]))
                {
                    groups[i] = card.Slot.gameObject.AddComponent<CanvasGroup>();
                }

                if (card.Card != null)
                {
                    int index = i;
                    clicks[i] = () => Select(index);
                    card.Card.Bind(clicks[i]);
                }
            }

            if (chooseButton != null)
            {
                chooseButton.Bind(ChooseCurrent, "Carousel: choose the centred card");
            }
        }

        protected virtual void OnEnable()
        {
            current = Mathf.Clamp(StartIndex, 0, Mathf.Max(0, modes.Length - 1));
            position = current;
            Apply();
            RefreshDots();
        }

        protected virtual void OnDisable()
        {
            snap?.Kill();
            snap = null;
        }

        protected virtual void OnDestroy()
        {
            for (int i = 0; i < modes.Length; i++)
            {
                if (modes[i].Card != null && clicks != null && clicks[i] != null)
                {
                    modes[i].Card.Unbind(clicks[i]);
                }
            }

            if (chooseButton != null)
            {
                chooseButton.Unbind(ChooseCurrent);
            }
        }

        public void Select(int index)
        {
            if (Busy || index < 0 || index >= modes.Length)
            {
                return;
            }

            if (index != current)
            {
                SnapTo(index);
                return;
            }

            Choose(index);
        }

        /// <summary>
        /// Chooses the centred card - or, mid-snap, the card the row is settling on - exactly as tapping it would.
        /// </summary>
        public void ChooseCurrent() => Select(current);

        public void SnapTo(int index)
        {
            if (modes.Length == 0)
            {
                return;
            }

            current = Mathf.Clamp(index, 0, modes.Length - 1);
            RefreshDots();

            snap?.Kill();

            if (snapDuration <= 0f || !isActiveAndEnabled)
            {
                position = current;
                Apply();
                return;
            }

            snap = DOTween.To(() => position, value => { position = value; Apply(); }, current, snapDuration)
                .SetEase(snapEase)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnKill(() => snap = null);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            snap?.Kill();
            dragOrigin = position;
            dragDistance = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Busy || modes.Length == 0)
            {
                return;
            }

            dragDistance += eventData.delta.x / ScaleFactor;
            position = Resist(dragOrigin - dragDistance / spacing);
            Apply();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Busy)
            {
                return;
            }

            int target = current;

            if (dragDistance <= -swipeThreshold)
            {
                target = current + 1;
            }
            else if (dragDistance >= swipeThreshold)
            {
                target = current - 1;
            }

            SnapTo(target);
        }

        private float ScaleFactor => canvas != null && canvas.rootCanvas.scaleFactor > 0f ? canvas.rootCanvas.scaleFactor : 1f;

        private float Resist(float value)
        {
            float last = modes.Length - 1;

            if (value < 0f)
            {
                return value * edgeResistance;
            }

            if (value > last)
            {
                return last + (value - last) * edgeResistance;
            }

            return value;
        }

        private void Apply()
        {
            int nearest = Mathf.Clamp(Mathf.RoundToInt(position), 0, Mathf.Max(0, modes.Length - 1));

            for (int i = 0; i < modes.Length; i++)
            {
                RectTransform slot = modes[i].Slot;

                if (slot == null)
                {
                    continue;
                }

                float offset = i - position;
                float away = Mathf.Clamp01(Mathf.Abs(offset));

                slot.anchoredPosition = new Vector2(offset * spacing, slot.anchoredPosition.y);
                slot.localScale = Vector3.one * Mathf.Lerp(1f, sideScale, away);

                if (groups[i] != null)
                {
                    groups[i].alpha = Mathf.Lerp(1f, sideAlpha, away);
                }
            }

            RectTransform front = modes.Length > 0 ? modes[nearest].Slot : null;

            if (front != null && front.GetSiblingIndex() != front.parent.childCount - 1)
            {
                front.SetAsLastSibling();
            }
        }

        private void RefreshDots()
        {
            for (int i = 0; i < modes.Length; i++)
            {
                Image dot = modes[i].Dot;

                if (dot == null)
                {
                    continue;
                }

                bool active = i == current;
                dot.color = active ? modes[i].Accent : idleDot;
                dot.rectTransform.sizeDelta = active ? activeDotSize : idleDotSize;
            }
        }
    }
}
