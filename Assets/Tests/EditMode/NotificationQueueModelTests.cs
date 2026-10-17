using MyToolz.UI.Notifications.Model;
using NUnit.Framework;

namespace MyToolz.Tests.EditMode
{
    public class NotificationQueueModelTests
    {
        private sealed class Msg { }

        private static NotificationData Data(string key, NotificationPriority priority = NotificationPriority.Normal,
            OverflowPolicy overflow = OverflowPolicy.None, DedupePolicy dedupe = DedupePolicy.None) => new NotificationData
        {
            Key = key,
            MessageType = typeof(Msg),
            Priority = priority,
            Overflow = overflow,
            Dedupe = dedupe
        };

        [Test]
        public void DedupeNone_AllowsSharedKeys_AndRemovingOneKeepsTheKeyKnown()
        {
            var model = new NotificationQueueModel(2);
            var first = model.TryAdd(Data("k"));
            model.TryAdd(Data("k"));

            model.RemoveActiveById(first.SpawnedId, out _);

            Assert.IsTrue(model.HasKey("k"), "one entry with the key is still active");
            Assert.AreEqual(AddResult.Dropped, model.TryAdd(Data("k", dedupe: DedupePolicy.IgnoreIfSameKeyExists)).Result);
        }

        [Test]
        public void DropNew_DropsWhenFull()
        {
            var model = new NotificationQueueModel(1);
            model.TryAdd(Data("a"));

            var outcome = model.TryAdd(Data("b", overflow: OverflowPolicy.DropNew));

            Assert.AreEqual(AddResult.Dropped, outcome.Result);
            Assert.AreEqual(0, model.PendingCount);
        }

        [Test]
        public void DropOldest_ReportsTheReplacedNotification()
        {
            var model = new NotificationQueueModel(1);
            var first = model.TryAdd(Data("a"));

            var outcome = model.TryAdd(Data("b", overflow: OverflowPolicy.DropOldest));

            Assert.AreEqual(AddResult.ReplacedActive, outcome.Result);
            Assert.AreEqual(first.SpawnedId, outcome.ReplacedId);
            Assert.IsFalse(model.HasKey("a"));
        }

        [Test]
        public void DropLowestPriority_NeverEvictsAHigherPriorityNotification()
        {
            var model = new NotificationQueueModel(1);
            model.TryAdd(Data("critical", NotificationPriority.Critical));

            var outcome = model.TryAdd(Data("low", NotificationPriority.Low, OverflowPolicy.DropLowestPriority));

            Assert.AreEqual(AddResult.Dropped, outcome.Result);
            Assert.IsTrue(model.HasKey("critical"));
        }

        [Test]
        public void PendingQueue_IsBounded_AndKeepsHigherPriorities()
        {
            var model = new NotificationQueueModel(1, 2);
            model.TryAdd(Data("active"));
            model.TryAdd(Data("low1", NotificationPriority.Low));
            model.TryAdd(Data("low2", NotificationPriority.Low));

            Assert.AreEqual(AddResult.Dropped, model.TryAdd(Data("low3", NotificationPriority.Low)).Result, "a full queue drops equal priority");
            Assert.AreEqual(AddResult.Enqueued, model.TryAdd(Data("high", NotificationPriority.High)).Result, "a higher priority displaces the last queued");
            Assert.AreEqual(2, model.PendingCount);
            Assert.AreEqual("high", model.Pending[0].Key);
            Assert.IsFalse(model.HasKey("low2"));
        }

        [Test]
        public void ReplaceIfSameKeyExists_ReplacesAQueuedEntry_InsteadOfAddingAnother()
        {
            var model = new NotificationQueueModel(1);
            model.TryAdd(Data("active"));
            model.TryAdd(Data("queued"));

            var outcome = model.TryAdd(Data("queued", dedupe: DedupePolicy.ReplaceIfSameKeyExists));

            Assert.AreEqual(AddResult.Enqueued, outcome.Result);
            Assert.AreEqual(1, model.PendingCount);
        }

        [Test]
        public void Promotion_MovesQueuedEntryWithoutRejectingItself()
        {
            var model = new NotificationQueueModel(1);
            var first = model.TryAdd(Data("k"));
            model.TryAdd(Data("k", dedupe: DedupePolicy.None));
            model.RemoveActiveById(first.SpawnedId, out _);

            Assert.IsTrue(model.TryPromotePending(out var promoted, out var outcome));
            Assert.AreEqual("k", promoted.Key);
            Assert.AreEqual(AddResult.Spawned, outcome.Result);
            Assert.AreEqual(1, model.ActiveCount);
            Assert.AreEqual(0, model.PendingCount);
            Assert.IsTrue(model.HasKey("k"));
        }
    }
}
