using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoSaints.UI.Notifications
{
    public interface INotification
    {
        public float Duration { get; set; }
        public string ToStr();
    }

    public interface INotificationQueueView<T> where T : INotification
    {
        public void Display(T notification);
    }

    public class NotificationQueueView : MonoBehaviour
    {

    }
}
