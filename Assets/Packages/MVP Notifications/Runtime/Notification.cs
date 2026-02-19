using UnityEngine;
using DG.Tweening;
using TMPro;
using System;
using MyToolz.Utilities.Debug;

namespace MyToolz.UI.Notifications
{
    [System.Serializable]
    public abstract class NotificationStrategy
    {
        [NonSerialized] private Notification owner;
        protected Notification Owner => owner;

        [NonSerialized] private bool initialized;

        public void Initialize(Notification notificationOwner)
        {
            if (notificationOwner == null)
            {
                DebugUtility.LogError(this, "NotificationStrategy.Initialize called with null owner");
                return;
            }

            owner = notificationOwner;

            if (initialized)
                return;

            initialized = true;
            OnInitialized();
        }

        protected virtual void OnInitialized() { }

        public void Start() => OnStart();
        public void Stop() => OnStop();

        protected abstract void OnStart();
        protected abstract void OnStop();

        protected void NotifyHidden()
        {
            owner?.Hidden();
        }

        protected void KillTween(ref Tween tween)
        {
            if (tween != null && tween.IsActive())
                tween.Kill();

            tween = null;
        }

        protected void KillSequence(ref Sequence sequence)
        {
            if (sequence != null && sequence.IsActive())
                sequence.Kill();

            sequence = null;
        }
    }


    [System.Serializable]
    public class TextNotificationStrategy : NotificationStrategy
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Range(0.01f, 5f)] private float fadeInSpeed = 0.1f;
        [SerializeField, Range(0.01f, 5f)] private float fadeOutSpeed = 0.1f;

        private Tween fadeTween;

        protected override void OnStart()
        {
            KillTween(ref fadeTween);

            if (canvasGroup == null)
            {
                DebugUtility.LogError(this, "TextNotificationStrategy: CanvasGroup not assigned");
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

        protected override void OnStop()
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


    [System.Serializable]
    public abstract class TimedNotificationStrategy : NotificationStrategy
    {
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField, Range(0.01f, 10f)] protected float fadeSpeed = 0.1f;
        [SerializeField, Range(0.01f, 10f)] protected float displayDuration = 2f;

        [SerializeField] protected RectTransform rectTransform;
        [SerializeField, Range(1f, 3f)] protected float startScaleMultiplier = 1.5f;

        private Sequence sequence;
        private Vector2 originalSize;

        protected override void OnStart()
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

        protected override void OnStop()
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

    [System.Serializable]
    public class KillNotificationStrategy : TimedNotificationStrategy
    {

    }

    [System.Serializable]
    public class AmmoNotificationStrategy : TimedNotificationStrategy
    {

    }

    [System.Serializable]
    public class NullNotificationStrategy : NotificationStrategy
    {
        protected override void OnStart() { }
        protected override void OnStop() { NotifyHidden(); }
    }


    [RequireComponent(typeof(CanvasGroup))]
    public class Notification : MonoBehaviour
    {
        [SerializeField] private TMP_Text notificationDisplay;
        [SerializeReference, SubclassSelector] private NotificationStrategy strategy;

        public Action? OnHidden { get; set; }
        public string Message => notificationDisplay != null ? notificationDisplay.text : string.Empty;

        private bool initialized;

        private void Awake()
        {
            InitializeStrategyIfNeeded();
        }

        private void OnEnable()
        {
            InitializeStrategyIfNeeded();
            strategy?.Start();
        }

        private void InitializeStrategyIfNeeded()
        {
            if (initialized)
                return;

            if (strategy == null)
            {
                DebugUtility.LogError(this, "Notification has no strategy assigned");
                return;
            }

            strategy.Initialize(this);
            initialized = true;
        }

        public void SetMessage(string message)
        {
            if (notificationDisplay == null)
            {
                DebugUtility.LogWarning(this, "Notification has no TMP_Text assigned");
                return;
            }

            notificationDisplay.text = message;
        }

        public void Stop()
        {
            strategy?.Stop();
        }


        public void Hidden()
        {
            OnHidden?.Invoke();
        }

        public Type GetMessageType() => strategy?.GetType();

        private void OnDestroy()
        {
            strategy?.Stop();
        }
    }
}
