using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.UI.Kit
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MyToolz/UI Kit/UI Shine")]
    public class UIShine : MonoBehaviour
    {
        [Tooltip("The streak that travels across the plate. Clipped by the mask on this object.")]
        [SerializeField] private RectTransform streak;

        [SerializeField] private Graphic graphic;

        [Tooltip("Seconds for one pass.")]
        [SerializeField, Min(0.1f)] private float sweep = 1.05f;

        [Tooltip("Pause between passes.")]
        [SerializeField, Min(0f)] private float gap = 1.7f;

        [Tooltip("Peak opacity of the streak.")]
        [SerializeField, Range(0f, 1f)] private float peakAlpha = 0.45f;

        [Tooltip("Share of the pass spent fading in, and again fading out.")]
        [SerializeField, Range(0.05f, 0.5f)] private float fadeShare = 0.25f;

        private RectTransform self;
        private Sequence tween;
        private bool running;

        private void Awake() => Cache();

        private void OnEnable()
        {
            if (running)
            {
                Play();
            }
        }

        private void OnDisable() => Kill();

        private void OnDestroy() => Kill();

        public void SetActive(bool active)
        {
            running = active;

            if (active && isActiveAndEnabled)
            {
                Play();
                return;
            }

            Kill();
            SetAlpha(0f);
        }

        private void Cache()
        {
            if (self == null)
            {
                self = (RectTransform)transform;
            }

            if (graphic == null && streak != null)
            {
                graphic = streak.GetComponent<Graphic>();
            }
        }

        private void Play()
        {
            Cache();

            if (streak == null || self == null)
            {
                return;
            }

            Kill();

            float travel = self.rect.width * 0.5f + streak.rect.width;
            float fade = sweep * fadeShare;

            tween = DOTween.Sequence().SetLoops(-1, LoopType.Restart).SetUpdate(true);
            tween.AppendCallback(() =>
            {
                streak.anchoredPosition = new Vector2(-travel, 0f);
                SetAlpha(0f);
            });
            tween.Append(streak.DOAnchorPosX(travel, sweep).SetEase(Ease.InOutSine));
            tween.Join(DOTween.To(GetAlpha, SetAlpha, peakAlpha, fade));
            tween.Insert(sweep - fade, DOTween.To(GetAlpha, SetAlpha, 0f, fade));
            tween.AppendInterval(gap);
        }

        private void Kill()
        {
            tween?.Kill();
            tween = null;
        }

        private float GetAlpha() => graphic != null ? graphic.color.a : 0f;

        private void SetAlpha(float value)
        {
            if (graphic == null)
            {
                return;
            }

            Color color = graphic.color;
            color.a = value;
            graphic.color = color;
        }
    }
}
