using DG.Tweening;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.UI.Labels;
using TMPro;
using UnityEngine;

namespace MyToolz.Networking.UI
{
    public class LogLabel : Label
    {
        [SerializeField] private TMP_Text display;

        [SerializeField, Range(0, 100f)] private float lifetime = 2.75f;
        [SerializeField] private float fadeDuration = 0.5f;

        private Tween lifetimeTween;
        [SerializeField] private CanvasGroup canvasGroup;

        public void Construct(string text)
        {
            display.text = text;
            StartLifetimeTween();
        }

        private void StartLifetimeTween()
        {
            canvasGroup.alpha = 0f;

            lifetimeTween?.Kill();

            lifetimeTween = DOTween.Sequence()
                .Append(canvasGroup.DOFade(1, fadeDuration / 2))
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
            canvasGroup.alpha = 0f;
        }
    }
}
