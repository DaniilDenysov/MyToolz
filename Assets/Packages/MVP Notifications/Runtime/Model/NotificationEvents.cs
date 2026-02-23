using System;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.UI.Notifications.Model;

namespace MyToolz.UI.Events
{
    public struct NotificationRequest : IEvent
    {
        public string Key;
        public Type MessageType;
        public string Text;
        public NotificationPriority Priority;
        public OverflowPolicy Overflow;
        public DedupePolicy Dedupe;

        public NotificationData ToData()
        {
            return new NotificationData
            {
                Key = Key,
                MessageType = MessageType,
                Text = Text,
                Priority = Priority,
                Overflow = Overflow,
                Dedupe = Dedupe
            };
        }
    }

    public struct NotificationClearRequest : IEvent
    {
        public string Key;
        public Type MessageType;
    }
}
