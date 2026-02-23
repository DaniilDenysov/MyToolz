using UnityEngine;
using DG.Tweening;
using MyToolz.Utilities.Debug;

namespace MyToolz.UI.Notifications.View
{
    public class TextNotification : NotificationBase
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Range(0.01f, 5f)] private float fadeInSpeed = 0.1f;
        [SerializeField, Range(0.01f, 5f)] private float fadeOutSpeed = 0.1f;

        private Tween fadeTween;

        public override void Play()
        {
            KillTween(ref fadeTween);

            if (canvasGroup == null)
            {
                DebugUtility.LogError(this, "TextNotification: CanvasGroup not assigned");
                return;
            }

            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 0f;

            fadeTween = canvasGroup
                .DOFade(1f, fadeInSpeed)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .SetAutoKill(true);
        }

        public override void Stop()
        {
            KillTween(ref fadeTween);

            if (canvasGroup == null)
                return;

            fadeTween = canvasGroup
                .DOFade(0f, fadeOutSpeed)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetAutoKill(true)
                .OnComplete(() =>
                {
                    canvasGroup.gameObject.SetActive(false);
                    NotifyHidden();
                });
        }
    }
}
