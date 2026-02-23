using UnityEngine;
using DG.Tweening;
using TMPro;
using System;
using MyToolz.Utilities.Debug;

namespace MyToolz.UI.Notifications.View
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class NotificationBase : MonoBehaviour
    {
        [SerializeField] private TMP_Text notificationDisplay;

        public Action OnHidden { get; set; }
        public string Message => notificationDisplay != null ? notificationDisplay.text : string.Empty;

        public void SetMessage(string message)
        {
            if (notificationDisplay == null)
            {
                DebugUtility.LogWarning(this, "NotificationBase has no TMP_Text assigned");
                return;
            }

            notificationDisplay.text = message;
        }

        protected virtual void OnEnable()
        {
            Play();
        }

        public abstract void Play();
        public abstract void Stop();

        protected void NotifyHidden()
        {
            OnHidden?.Invoke();
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

        protected virtual void OnDestroy()
        {
            Stop();
        }
    }
}
