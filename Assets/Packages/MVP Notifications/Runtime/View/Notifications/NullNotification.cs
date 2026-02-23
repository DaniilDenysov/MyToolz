namespace MyToolz.UI.Notifications.View
{
    public class NullNotification : NotificationBase
    {
        public override void Play() { }
        public override void Stop() { NotifyHidden(); }
    }
}
