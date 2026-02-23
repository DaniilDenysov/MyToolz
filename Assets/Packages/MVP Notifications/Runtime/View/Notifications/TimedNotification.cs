using UnityEngine;
using DG.Tweening;
using MyToolz.Utilities.Debug;

namespace MyToolz.UI.Notifications.View
{
    public abstract class TimedNotification : NotificationBase
    {
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField, Range(0.01f, 10f)] protected float fadeSpeed = 0.1f;
        [SerializeField, Range(0.01f, 10f)] protected float displayDuration = 2f;
        [SerializeField] protected RectTransform rectTransform;
        [SerializeField, Range(1f, 3f)] protected float startScaleMultiplier = 1.5f;

        private Sequence sequence;
        private Vector2 originalSize;

        public override void Play()
        {
            KillSequence(ref sequence);

            if (canvasGroup == null)
            {
                DebugUtility.LogError(this, $"{GetType().Name}: CanvasGroup not assigned");
                return;
            }

            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 0.3f;

            if (rectTransform != null)
            {
                originalSize = rectTransform.sizeDelta;
                rectTransform.sizeDelta = originalSize * startScaleMultiplier;
            }

            sequence = DOTween.Sequence();

            sequence.Append(canvasGroup.DOFade(1f, fadeSpeed).SetEase(Ease.Flash))
                    .SetUpdate(true);

            if (rectTransform != null)
            {
                sequence.Join(rectTransform.DOSizeDelta(originalSize, fadeSpeed).SetEase(Ease.Linear).SetUpdate(true));
            }

            sequence.AppendInterval(displayDuration);
            sequence.Append(canvasGroup.DOFade(0f, fadeSpeed).SetEase(Ease.OutQuad).SetUpdate(true));

            sequence.SetAutoKill(true)
                    .OnComplete(() =>
                    {
                        canvasGroup.gameObject.SetActive(false);
                        NotifyHidden();
                    });
        }

        public override void Stop()
        {
            KillSequence(ref sequence);

            if (canvasGroup == null)
            {
                NotifyHidden();
                return;
            }

            if (!canvasGroup.gameObject.activeSelf)
            {
                NotifyHidden();
                return;
            }

            sequence = DOTween.Sequence()
                .Append(canvasGroup.DOFade(0f, fadeSpeed).SetEase(Ease.OutQuad).SetUpdate(true));

            if (rectTransform != null)
            {
                sequence.Join(rectTransform
                    .DOSizeDelta(rectTransform.sizeDelta, 0f)
                    .SetUpdate(true));
            }

            sequence.SetAutoKill(true)
                    .OnComplete(() =>
                    {
                        if (rectTransform != null)
                            rectTransform.sizeDelta = originalSize;

                        canvasGroup.gameObject.SetActive(false);
                        NotifyHidden();
                    });
        }
    }
}
