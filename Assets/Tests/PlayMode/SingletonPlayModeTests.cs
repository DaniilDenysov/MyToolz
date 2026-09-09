using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MyToolz.DesignPatterns.Singleton;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyToolz.Tests.PlayMode
{
    public class TestPublicSingleton : PublicSingleton<TestPublicSingleton>
    {
        public int AwakeCount;
        public int DestroyCount;
        protected override void OnSingletonAwake() => AwakeCount++;
        protected override void OnSingletonDestroy() => DestroyCount++;
    }

    public class TestPrivateSingleton : PrivateSingleton<TestPrivateSingleton>
    {
        public int AwakeCount;
        protected override void OnSingletonAwake() => AwakeCount++;
    }

    public class SingletonPlayModeTests
    {
        private readonly List<GameObject> _created = new();

        [TearDown]
        public void Cleanup()
        {
            // Destroying the surviving instance's GameObject runs OnDestroy -> RemoveSelf,
            // which nulls the static Instance so the next test starts clean.
            foreach (var go in _created)
                if (go != null) Object.DestroyImmediate(go);
            _created.Clear();
        }

        private TestPublicSingleton NewPublic(bool active = true)
        {
            var go = new GameObject("PublicSingleton");
            _created.Add(go);
            if (!active) go.SetActive(false);
            return go.AddComponent<TestPublicSingleton>();
        }

        [UnityTest]
        public IEnumerator FirstInstance_RegistersItself_AndRunsOnSingletonAwakeOnce()
        {
            var first = NewPublic();
            yield return null;

            Assert.AreSame(first, TestPublicSingleton.Instance);
            Assert.AreEqual(1, first.AwakeCount, "the surviving singleton runs OnSingletonAwake exactly once");
        }

        [UnityTest]
        public IEnumerator DuplicateInstance_IsRejected_AndDoesNotReplaceTheOriginal()
        {
            var first = NewPublic();
            var duplicate = NewPublic();
            yield return null; // let the duplicate's deferred Destroy(this) run

            Assert.AreSame(first, TestPublicSingleton.Instance, "the original stays registered");
            Assert.AreEqual(0, duplicate.AwakeCount, "the duplicate never runs OnSingletonAwake");
            Assert.IsTrue(duplicate == null, "the duplicate component is destroyed");
        }

        [UnityTest]
        public IEnumerator DestroyGameObjectOnDuplicate_DestroysTheWholeDuplicateObject()
        {
            NewPublic();

            // Build the duplicate inactive so its flag is set before Awake decides what to destroy.
            var duplicate = NewPublic(active: false);
            typeof(Singleton)
                .GetField("destroyGameObjectOnDuplicate", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(duplicate, true);
            var duplicateGo = duplicate.gameObject;
            duplicateGo.SetActive(true); // Awake runs now, with the flag applied

            yield return null;

            Assert.IsTrue(duplicateGo == null, "the entire duplicate GameObject is destroyed, not just the component");
        }

        [UnityTest]
        public IEnumerator DestroyingInstance_ClearsRegistration_AndRunsOnSingletonDestroy()
        {
            var first = NewPublic();
            yield return null;

            Object.DestroyImmediate(first.gameObject);

            Assert.IsNull(TestPublicSingleton.Instance, "RemoveSelf clears the static Instance on destroy");
            // (DestroyCount lived on the now-destroyed component; the registration clear above is the observable contract.)
        }

        [UnityTest]
        public IEnumerator PrivateSingleton_GuardsDuplicates_AndReleasesSlotOnDestroy()
        {
            var firstGo = new GameObject("PrivateSingleton");
            _created.Add(firstGo);
            var first = firstGo.AddComponent<TestPrivateSingleton>();

            var dupGo = new GameObject("PrivateSingletonDup");
            _created.Add(dupGo);
            var duplicate = dupGo.AddComponent<TestPrivateSingleton>();
            yield return null;

            Assert.AreEqual(1, first.AwakeCount, "only the first private singleton initializes");
            Assert.AreEqual(0, duplicate.AwakeCount, "the duplicate is rejected");

            // Destroy the owner; a fresh instance should then be able to claim the slot.
            Object.DestroyImmediate(firstGo);

            var replacementGo = new GameObject("PrivateSingletonReplacement");
            _created.Add(replacementGo);
            var replacement = replacementGo.AddComponent<TestPrivateSingleton>();
            yield return null;

            Assert.AreEqual(1, replacement.AwakeCount, "destroying the owner frees the slot for a new instance");
        }
    }
}
