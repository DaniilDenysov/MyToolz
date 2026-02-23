using System;
using MyToolz.DesignPatterns.MVP.View;
using MyToolz.UI.Notifications.Model;

namespace MyToolz.UI.Notifications.View
{
    public interface INotificationView : IReadOnlyView<NotificationQueueModel>
    {
        bool HasPrefabForType(Type messageType);
        void HandleAdded(AddOutcome outcome, NotificationData data, Action<int> onHidden);
        void HandleCleared(int id);
        void HandleEvicted(int id);
        void Reorder(NotificationQueueModel model);
        void ReleaseAll();
    }
}
