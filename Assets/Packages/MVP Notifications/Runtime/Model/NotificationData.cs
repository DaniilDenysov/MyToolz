using System;

namespace MyToolz.UI.Notifications.Model
{
    public enum NotificationPriority
    {
        Low = 0,
        Normal = 10,
        High = 20,
        Critical = 30
    }

    public enum OverflowPolicy
    {
        None,
        DropNew,
        DropOldest,
        DropLowestPriority,
        ReplaceSameKeyOrDropNew
    }

    public enum DedupePolicy
    {
        None,
        IgnoreIfSameKeyExists,
        ReplaceIfSameKeyExists
    }

    public struct NotificationData
    {
        public string Key;
        public Type MessageType;
        public string Text;
        public NotificationPriority Priority;
        public OverflowPolicy Overflow;
        public DedupePolicy Dedupe;
    }
}
