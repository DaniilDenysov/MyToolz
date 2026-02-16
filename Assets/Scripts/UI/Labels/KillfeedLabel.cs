using DG.Tweening;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.UI.Labels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.Networking.UI.Labels
{
    public class KillfeedLabel : Label
    {
        [SerializeField] private TMP_Text display;
        [SerializeField] private TMP_Text killerDisplay, victimDisplay;
        [SerializeField] private Image weaponIcon;

        [SerializeField, Range(0, 100f)] private float lifetime = 2.75f;
        [SerializeField] private float fadeDuration = 0.5f;

        private Tween lifetimeTween;
        [SerializeField] private CanvasGroup canvasGroup;

        public void Construct(string killer, string phrase, string victim)
        {
            display.text = $"{killer} {phrase} {victim}";
            StartLifetimeTween();
        }

        public void Construct(string killer, Sprite icon, string victim)
        {
            killerDisplay.text = killer;
            weaponIcon.sprite = icon;
            victimDisplay.text = victim;
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
