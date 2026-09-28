using UnityEngine;
using Zenject;
using MyToolz.EditorToolz;
using MyToolz.UI.Notifications.Model;
using MyToolz.UI.Notifications.Presenter;
using MyToolz.UI.Notifications.View;

namespace MyToolz.UI.Notifications.Installers
{
    public class NotificationInstaller : MonoInstaller
    {
        [FoldoutGroup("Config"), SerializeField, Range(1, 10)] private int maxActive = 2;
        [FoldoutGroup("Config"), SerializeField, Range(0, 256), Tooltip("Queued notifications kept while every active slot is used. When full, a new one only displaces a lower-priority queued entry.")]
        private int maxPending = NotificationQueueModel.DefaultMaxPending;
        [FoldoutGroup("View"), SerializeField] private PlayerNotificationView view;

        public override void InstallBindings()
        {
            Container
                .Bind<NotificationQueueModel>()
                .FromInstance(new NotificationQueueModel(maxActive, maxPending))
                .AsSingle();

            Container
                .Bind<INotificationView>()
                .FromInstance(view)
                .AsSingle();

            Container
                .BindInterfacesAndSelfTo<NotificationPresenter>()
                .AsSingle()
                .NonLazy();
        }
    }
}
