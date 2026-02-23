using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.MVP.Presenter;
using MyToolz.UI.Events;
using MyToolz.UI.Notifications.Model;
using MyToolz.UI.Notifications.View;
using MyToolz.Utilities.Debug;
using Zenject;

namespace MyToolz.UI.Notifications.Presenter
{
    public class NotificationPresenter : PresenterBase<NotificationQueueModel, INotificationView>, IInitializable
    {
        private EventBinding<NotificationRequest> notificationBinding;
        private EventBinding<NotificationClearRequest> clearBinding;

        public NotificationPresenter(NotificationQueueModel model, INotificationView view)
            : base(model, view) { }

        protected override void OnInitialize()
        {
            View.Initialize(Model);
        }

        protected override void SubscribeEvents()
        {
            notificationBinding = new EventBinding<NotificationRequest>(OnNotificationRequested);
            EventBus<NotificationRequest>.Register(notificationBinding);
            clearBinding = new EventBinding<NotificationClearRequest>(OnClearRequested);
            EventBus<NotificationClearRequest>.Register(clearBinding);
        }

        protected override void UnsubscribeEvents()
        {
            EventBus<NotificationRequest>.Deregister(notificationBinding);
            EventBus<NotificationClearRequest>.Deregister(clearBinding);
        }

        protected override void OnDispose()
        {
            View.ReleaseAll();
            Model.Reset();
        }

        private void OnNotificationRequested(NotificationRequest e)
        {
            if (e.MessageType == null)
            {
                DebugUtility.LogError(this, "NotificationRequest.MessageType is null");
                return;
            }

            if (!View.HasPrefabForType(e.MessageType))
            {
                DebugUtility.LogWarning(this, $"No notification prefab mapped for type: {e.MessageType}");
                return;
            }

            var data = e.ToData();
            var outcome = Model.TryAdd(data);

            if (outcome.Result == AddResult.Dropped || outcome.Result == AddResult.Enqueued)
                return;

            View.HandleAdded(outcome, data, OnNotificationHidden);
            View.Reorder(Model);
        }

        private void OnClearRequested(NotificationClearRequest e)
        {
            if (string.IsNullOrEmpty(e.Key))
            {
                DebugUtility.LogWarning(this, "NotificationClear.Key is null/empty");
                return;
            }

            int id = Model.RemoveByKey(e.Key, e.MessageType);
            if (id >= 0)
                View.HandleCleared(id);
        }

        private void OnNotificationHidden(int id)
        {
            if (!Model.RemoveActiveById(id, out _))
                return;

            View.HandleEvicted(id);
            TryPromotePending();
        }

        private void TryPromotePending()
        {
            while (Model.HasActiveCapacity() && Model.PendingCount > 0)
            {
                var next = Model.DequeuePending();
                if (next == null) break;

                var pending = next.Value;
                var outcome = Model.TryAdd(pending.Request);

                if (outcome.Result == AddResult.Spawned)
                {
                    View.HandleAdded(outcome, pending.Request, OnNotificationHidden);
                    View.Reorder(Model);
                }
            }
        }
    }
}
