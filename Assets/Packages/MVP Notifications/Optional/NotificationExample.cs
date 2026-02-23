using UnityEngine;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.UI.Events;
using MyToolz.UI.Notifications.Model;
using MyToolz.UI.Notifications.View;

namespace MyToolz.UI.Notifications.Examples
{
    public class NotificationExample : MonoBehaviour
    {
        public void PushKillNotification(string victimName)
        {
            EventBus<NotificationRequest>.Raise(new NotificationRequest
            {
                Key = $"kill_{victimName}",
                MessageType = typeof(KillNotification),
                Text = $"Eliminated {victimName}",
                Priority = NotificationPriority.High,
                Overflow = OverflowPolicy.DropOldest,
                Dedupe = DedupePolicy.None
            });
        }

        public void PushAmmoPickup(string ammoType, int amount)
        {
            EventBus<NotificationRequest>.Raise(new NotificationRequest
            {
                Key = $"ammo_{ammoType}",
                MessageType = typeof(AmmoNotification),
                Text = $"+{amount} {ammoType}",
                Priority = NotificationPriority.Low,
                Overflow = OverflowPolicy.DropNew,
                Dedupe = DedupePolicy.ReplaceIfSameKeyExists
            });
        }

        public void ShowPersistentWarning(string key, string message)
        {
            EventBus<NotificationRequest>.Raise(new NotificationRequest
            {
                Key = key,
                MessageType = typeof(TextNotification),
                Text = message,
                Priority = NotificationPriority.Critical,
                Overflow = OverflowPolicy.DropLowestPriority,
                Dedupe = DedupePolicy.IgnoreIfSameKeyExists
            });
        }

        public void DismissPersistentWarning(string key)
        {
            EventBus<NotificationClearRequest>.Raise(new NotificationClearRequest
            {
                Key = key,
                MessageType = typeof(TextNotification)
            });
        }
    }
}
