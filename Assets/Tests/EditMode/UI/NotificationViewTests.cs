using System.Reflection;
using DG.Tweening;
using MyToolz.UI.Notifications.View;
using NUnit.Framework;
using UnityEngine;

namespace MyToolz.Tests.EditMode.UI
{
    public class NotificationViewTests
    {
        private sealed class TestTimedNotification : TimedNotification
        {
            public void Wire(CanvasGroup group, RectTransform rect)
            {
                canvasGroup = group;
                rectTransform = rect;
                startScaleMultiplier = 1.5f;
            }
        }

        private GameObject go;

        [TearDown]
        public void TearDown()
        {
            DOTween.KillAll();
            if (go != null) Object.DestroyImmediate(go);
        }

        [Test]
        public void TimedNotification_ReplayedMidPopIn_KeepsItsRestingSize()
        {
            go = new GameObject("Notification", typeof(RectTransform), typeof(CanvasGroup));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(100f, 40f);
            var notification = go.AddComponent<TestTimedNotification>();
            notification.Wire(go.GetComponent<CanvasGroup>(), rect);

            notification.Play();
            Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(150f, 60f)), "pop-in starts enlarged");

            // Played again before the pop-in shrank it back: the enlarged size must not become the resting size.
            notification.Play();
            Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(150f, 60f)));
        }

        [Test]
        public void PlayerNotificationView_FitsInstancesAtUnitScale()
        {
            go = new GameObject("Notification", typeof(RectTransform), typeof(CanvasGroup));
            var notification = go.AddComponent<TestTimedNotification>();
            go.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
            go.transform.localPosition = new Vector3(12f, 7f, -40f);

            typeof(PlayerNotificationView)
                .GetMethod("Fit", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { notification });

            Assert.That(go.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(go.transform.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(go.transform.localPosition, Is.EqualTo(new Vector3(12f, 7f, 0f)));
        }
    }
}
