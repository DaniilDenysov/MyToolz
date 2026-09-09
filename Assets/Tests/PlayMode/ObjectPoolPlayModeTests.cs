using System.Collections;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.ObjectPool;
using MyToolz.Events;
using MyToolz.Utilities.Debug;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;

namespace MyToolz.Tests.PlayMode
{
    /// <summary>Pooled test object that records its poolable lifecycle callbacks.</summary>
    public class PoolablePeg : MonoBehaviour, IPoolable
    {
        public int SpawnCount { get; private set; }
        public int DespawnCount { get; private set; }

        public void OnSpawned() => SpawnCount++;
        public void OnDespawned() => DespawnCount++;
    }

    /// <summary>
    /// Concrete, non-generic pool installer so Unity can attach it as a component, plus test-only
    /// hooks to configure the pooled prefab and inspect the live spawn count. The protected members
    /// it touches are part of the base class's own extension surface.
    /// </summary>
    public class TestPegPool : DefaultObjectPoolInstaller<PoolablePeg>
    {
        public void ConfigureForTest(PoolablePeg prefab, int defaultCapacity, int maxCapacity, PoolCapacityMode mode)
        {
            poolObjects = new[]
            {
                new PoolObject
                {
                    Prefab = prefab,
                    DefaultCapacity = defaultCapacity,
                    MaxCapacity = maxCapacity,
                    CapacityMode = mode
                }
            };
        }

        public int SpawnedCount => spawned.Count;
    }

    public class ObjectPoolPlayModeTests
    {
        private GameObject _prefabGO;
        private PoolablePeg _prefab;
        private GameObject _installerGO;
        private TestPegPool _installer;
        private DiContainer _container;

        private LogGateSettingsSO _previousGate;
        private LogGateSettingsSO _silentGate;

        [SetUp]
        public void SetUp()
        {
            // Silence the installer's diagnostic logging (capacity refusals etc.) so it neither
            // spams the runner nor trips the "unexpected log" failure.
            _previousGate = LogGate.Settings;
            _silentGate = ScriptableObject.CreateInstance<LogGateSettingsSO>();
            _silentGate.SetDefaultEnabled(false);
            LogGate.Settings = _silentGate;
        }

        [TearDown]
        public void TearDown()
        {
            if (_installerGO != null) Object.DestroyImmediate(_installerGO);
            if (_prefabGO != null) Object.DestroyImmediate(_prefabGO);

            // Destroy every pooled instance (and its pool transform group) left in the scene.
            foreach (var peg in Resources.FindObjectsOfTypeAll<PoolablePeg>())
            {
                if (peg != null && peg.transform != null)
                {
                    Object.DestroyImmediate(peg.transform.root.gameObject);
                }
            }

            EventBusUtil.ClearAllBuses();

            LogGate.Settings = _previousGate;
            if (_silentGate != null)
            {
                Object.DestroyImmediate(_silentGate);
                _silentGate = null;
            }
        }

        private IEnumerator SetupPool(int defaultCapacity, int maxCapacity, PoolCapacityMode mode)
        {
            _prefabGO = new GameObject("PegPrefab");
            _prefab = _prefabGO.AddComponent<PoolablePeg>();
            _prefabGO.SetActive(false);

            _container = new DiContainer();

            _installerGO = new GameObject("PegPool");
            _installer = _installerGO.AddComponent<TestPegPool>();
            yield return null; // let Singleton.Awake run so the installer is ready to initialize

            _installer.ConfigureForTest(_prefab, defaultCapacity, maxCapacity, mode);
            _container.Inject(_installer); // runs [Inject] Construct -> pool init + event registration
            yield return null;
        }

        [UnityTest]
        public IEnumerator Get_ReturnsActiveInstance_AndFiresPoolableSpawn()
        {
            yield return SetupPool(1, 10, PoolCapacityMode.SoftLock);

            PoolablePeg obj = _installer.Get(_prefab);

            Assert.IsNotNull(obj, "a configured pool should hand out an instance");
            Assert.IsTrue(obj.gameObject.activeSelf, "spawned objects are activated");
            Assert.AreEqual(1, obj.SpawnCount, "IPoolable.OnSpawned should fire once");
            Assert.AreEqual(1, _installer.SpawnedCount);
        }

        [UnityTest]
        public IEnumerator Release_ReturnsInstanceToPool_AndFiresPoolableDespawn()
        {
            yield return SetupPool(1, 10, PoolCapacityMode.SoftLock);

            PoolablePeg obj = _installer.Get(_prefab);
            _installer.Release(obj);

            Assert.IsFalse(obj.gameObject.activeSelf, "released objects are deactivated");
            Assert.AreEqual(1, obj.DespawnCount, "IPoolable.OnDespawned should fire once");
            Assert.AreEqual(0, _installer.SpawnedCount);
        }

        [UnityTest]
        public IEnumerator SoftLock_GrowsBeyondMaxCapacity()
        {
            yield return SetupPool(0, 1, PoolCapacityMode.SoftLock);

            PoolablePeg first = _installer.Get(_prefab);
            PoolablePeg second = _installer.Get(_prefab);

            Assert.IsNotNull(first);
            Assert.IsNotNull(second, "SoftLock ignores the max and expands on demand");
            Assert.AreNotSame(first, second);
            Assert.AreEqual(2, _installer.SpawnedCount);
        }

        [UnityTest]
        public IEnumerator HardLock_RefusesBeyondMaxCapacity()
        {
            yield return SetupPool(1, 1, PoolCapacityMode.HardLock);

            PoolablePeg first = _installer.Get(_prefab);
            PoolablePeg second = _installer.Get(_prefab);

            Assert.IsNotNull(first);
            Assert.IsNull(second, "HardLock refuses requests once the max is reached");
            Assert.AreEqual(1, _installer.SpawnedCount);
        }

        [UnityTest]
        public IEnumerator QueueLock_RecyclesOldestWhenFull()
        {
            yield return SetupPool(1, 1, PoolCapacityMode.QueueLock);

            PoolablePeg first = _installer.Get(_prefab);
            PoolablePeg second = _installer.Get(_prefab);

            Assert.IsNotNull(second, "QueueLock recycles the oldest instead of refusing");
            Assert.AreEqual(1, _installer.SpawnedCount, "recycling keeps the live count at the cap");
            Assert.GreaterOrEqual(first.DespawnCount, 1, "the oldest instance was recycled");
        }

        [UnityTest]
        public IEnumerator ReleaseAll_ReturnsEveryActiveInstance()
        {
            yield return SetupPool(0, 100, PoolCapacityMode.SoftLock);

            var a = _installer.Get(_prefab);
            var b = _installer.Get(_prefab);
            var c = _installer.Get(_prefab);
            Assert.AreEqual(3, _installer.SpawnedCount);

            _installer.ReleaseAll();

            Assert.AreEqual(0, _installer.SpawnedCount);
            Assert.IsFalse(a.gameObject.activeSelf);
            Assert.IsFalse(b.gameObject.activeSelf);
            Assert.IsFalse(c.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator PoolRequestEvent_SpawnsObject_AtRequestedPosition()
        {
            yield return SetupPool(1, 10, PoolCapacityMode.SoftLock);

            PoolablePeg spawned = null;
            var position = new Vector3(1f, 2f, 3f);

            EventBus<PoolRequest<PoolablePeg>>.Raise(new PoolRequest<PoolablePeg>
            {
                Prefab = _prefab,
                Position = position,
                Rotation = Quaternion.identity,
                Callback = p => spawned = p
            });

            Assert.IsNotNull(spawned, "raising a PoolRequest should spawn via the registered pool");
            Assert.IsTrue(spawned.gameObject.activeSelf);
            Assert.AreEqual(1, spawned.SpawnCount);
            Assert.Less(Vector3.Distance(spawned.transform.position, position), 0.001f);
        }

        [UnityTest]
        public IEnumerator ReleaseRequestEvent_ReturnsObjectToPool()
        {
            yield return SetupPool(1, 10, PoolCapacityMode.SoftLock);

            PoolablePeg obj = _installer.Get(_prefab);
            Assert.AreEqual(1, _installer.SpawnedCount);

            EventBus<ReleaseRequest<PoolablePeg>>.Raise(new ReleaseRequest<PoolablePeg>
            {
                PoolObject = obj
            });

            Assert.AreEqual(0, _installer.SpawnedCount, "raising a ReleaseRequest should return the object");
            Assert.IsFalse(obj.gameObject.activeSelf);
            Assert.AreEqual(1, obj.DespawnCount);
        }
    }
}
