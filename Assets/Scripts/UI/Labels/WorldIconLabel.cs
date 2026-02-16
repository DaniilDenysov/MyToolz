using DG.Tweening;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.UI.Labels;
using UnityEngine;

namespace NoSaints.UI.Labels
{
    public class WorldIconLabel : Label
    {
        [SerializeField, Range(0, 100f)] private float lifetime = 3f;
        [SerializeField] private float fadeDuration = 0.5f;

        private Tween lifetimeTween;
        [SerializeField] private CanvasGroup canvasGroup;

        public void Construct()
        {
            StartLifetimeTween();
        }

        private void StartLifetimeTween()
        {
            canvasGroup.alpha = 1f;

            lifetimeTween?.Kill();

            lifetimeTween = DOTween.Sequence()
                .AppendInterval(lifetime)
                .Append(canvasGroup.DOFade(0, fadeDuration))
                .OnComplete(() =>
                {
                    EventBus<ReleaseRequest<Label>>.Raise(new ReleaseRequest<Label>()
                    {
                        PoolObject = this
                    });
                });
        }

        public void OnDisable()
        {
            lifetimeTween?.Kill();
            canvasGroup.alpha = 1f;
        }
    }
}
