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
        [FoldoutGroup("View"), SerializeField] private PlayerNotificationView view;

        public override void InstallBindings()
        {
            Container
                .Bind<NotificationQueueModel>()
                .AsSingle()
                .WithArguments(maxActive);

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
