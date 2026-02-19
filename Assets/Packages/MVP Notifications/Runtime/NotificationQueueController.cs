using MyToolz.EditorToolz;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace NoSaints.UI.Notifications
{
    public interface INotificationQueueController<T> where T : INotification
    {
        public void AddNotification(T notification);
    }

    public abstract class NotificationQueueController<T> : INotificationQueueController<T> where T: INotification
    {
        [SerializeField, Required] protected Transform root;
        protected Queue<T> queue = new Queue<T>();
        protected INotificationQueueView<T> view;

        [Inject]
        private void Construct(INotificationQueueView<T> view)
        {
            this.view = view;
        }

        public void AddNotification(T notification)
        {

        }

        private IEnumerator Start()
        {
            while (true)
            {
                yield return new WaitUntil(()=>queue.TryPeek(out T notification));

            }
        }
    }
}
