using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using MyToolz.IO;
using NUnit.Framework;
using UnityEngine;

namespace MyToolz.Tests.EditMode
{
    [Serializable]
    public class IOPersistenceData
    {
        public int Value;
    }

    /// <summary>
    /// Concrete saver with Unity lifecycle hooks disabled so tests drive every save explicitly.
    /// </summary>
    public class IOPersistenceTestSaver : SaveLoadBase<IOPersistenceData>
    {
        protected override void Awake() { }
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        protected override void OnDestroy() { }
    }

    public class IOPersistenceTests : SilentLogTest
    {
        private GameObject owner;
        private IOPersistenceTestSaver saver;
        private string folder;
        private StorageLocation location;

        [SetUp]
        public void SetUp()
        {
            string relativeFolder = "MyToolzIOTests/" + Guid.NewGuid().ToString("N");
            folder = Path.Combine(Application.persistentDataPath, relativeFolder);
            string path = Path.Combine(folder, "save.json");
            location = new StorageLocation(path, path + ".tmp", path + ".bak", folder, relativeFolder + "/save.json");

            owner = new GameObject("IO test saver");
            owner.SetActive(false); // Awake is not required: save/load initialize lazily.
            saver = owner.AddComponent<IOPersistenceTestSaver>();
            SetField("filePath", relativeFolder);
            SetField("useCache", true);
            SetField("storageStrategy", new FileStorageStrategy());
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(owner);
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        [Test]
        public void SaveBeforeAwake_InitializesPathsAndRoundTrips()
        {
            saver.Save(new IOPersistenceData { Value = 42 });

            Assert.That(saver.LastSaveTask.Result, Is.True);
            Assert.That(File.Exists(location.FullPath), Is.True);
            Assert.That(saver.TryLoad(out var data, out var status), Is.True);
            Assert.That(data.Value, Is.EqualTo(42));
            Assert.That(status, Is.EqualTo(LoadStatus.LoadedPrimary));
        }

        [Test]
        public void LoadBeforeAwake_ReadsExistingLegacySave()
        {
            new FileStorageStrategy().Write(location, "{\"Value\":17}");

            Assert.That(saver.Load().Value, Is.EqualTo(17));
            Assert.That(saver.LastLoadStatus, Is.EqualTo(LoadStatus.LoadedPrimary));
        }

        [Test]
        public async Task AsyncSave_RoundTripsAndKeepsBackup()
        {
            Assert.That(await saver.SaveAsync(new IOPersistenceData { Value = 7 }), Is.True);
            Assert.That(await saver.SaveAsync(new IOPersistenceData { Value = 8 }), Is.True);

            Assert.That(saver.TryLoad(out var data, out _), Is.True);
            Assert.That(data.Value, Is.EqualTo(8));
            Assert.That(File.ReadAllText(location.BackupPath), Does.Contain("7"));
        }

        [Test]
        public void ExplicitSave_ReplacesCache_SoLaterAutomaticSaveKeepsNewestState()
        {
            // Regression: Load(A) -> Save(B) -> Save() used to write the stale A over B.
            new FileStorageStrategy().Write(location, "{\"Value\":1}");
            Assert.That(saver.Load().Value, Is.EqualTo(1));

            saver.Save(new IOPersistenceData { Value = 2 });
            saver.Save();

            Assert.That(saver.Load().Value, Is.EqualTo(2), "cache follows the explicitly saved object");
            Assert.That(ReadStoredValue(), Is.EqualTo(2), "the automatic save did not write stale data");
        }

        [Test]
        public void AutomaticSave_PersistsMutationsOfTheCachedObject()
        {
            IOPersistenceData data = saver.Load();
            data.Value = 5;

            saver.Save();

            Assert.That(ReadStoredValue(), Is.EqualTo(5));
        }

        [Test]
        public void AutomaticSave_SkipsWriteWhenCachedStateIsUnchanged()
        {
            saver.Save(new IOPersistenceData { Value = 3 });
            File.WriteAllText(location.FullPath, "{\"Value\":99}"); // marker written behind the saver's back

            saver.Save();
            Assert.That(ReadRawPrimary(), Does.Contain("99"), "unchanged cache does not rewrite storage");

            saver.Load().Value = 4;
            saver.Save();
            Assert.That(ReadStoredValue(), Is.EqualTo(4), "a changed cache is written");
        }

        [Test]
        public void MissingSave_ReportsMissing()
        {
            Assert.That(saver.TryLoad(out var data, out var status), Is.False);
            Assert.That(data, Is.Null);
            Assert.That(status, Is.EqualTo(LoadStatus.Missing));
            Assert.That(saver.Load().Value, Is.Zero, "legacy Load still returns a default object");
        }

        [Test]
        public void UnreadableSave_ReportsUnreadable_PreservesCorruptCopy_AndBlocksAutomaticSave()
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(location.FullPath, "{ not json");

            IOPersistenceData data = saver.Load();

            Assert.That(saver.LastLoadStatus, Is.EqualTo(LoadStatus.Unreadable));
            Assert.That(data.Value, Is.Zero);
            Assert.That(File.Exists(location.FullPath + ".corrupt"), Is.True, "unreadable payload is preserved");

            saver.Save();
            Assert.That(ReadRawPrimary(), Is.EqualTo("{ not json"), "defaults never overwrite unreadable data automatically");

            saver.Save(new IOPersistenceData { Value = 6 });
            Assert.That(ReadStoredValue(), Is.EqualTo(6), "an explicit save is an intentional reset");
        }

        [Test]
        public void TamperedEnvelope_IsRejected_AndBackupIsRecovered()
        {
            saver.Save(new IOPersistenceData { Value = 10 });
            saver.Save(new IOPersistenceData { Value = 11 });
            string tampered = ReadRawPrimary().Replace("11", "12");
            File.WriteAllText(location.FullPath, tampered);

            Assert.That(saver.TryLoad(out var data, out var status), Is.True);
            Assert.That(status, Is.EqualTo(LoadStatus.RecoveredBackup));
            Assert.That(data.Value, Is.EqualTo(10));
        }

        [Test]
        public void MissingPrimary_RecoversBackupWithoutRotatingIt()
        {
            var storage = new FileStorageStrategy();
            storage.Write(location, "{\"Value\":11}");
            storage.Write(location, "{\"Value\":12}");
            File.Delete(location.FullPath);

            Assert.That(saver.TryLoad(out var data, out var status), Is.True);
            Assert.That(data.Value, Is.EqualTo(11));
            Assert.That(status, Is.EqualTo(LoadStatus.RecoveredBackup));
            Assert.That(File.ReadAllText(location.FullPath), Is.EqualTo(File.ReadAllText(location.BackupPath)));
        }

        [Test]
        public void CompletePendingTemp_IsPromoted()
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(location.TempPath, "{\"Value\":21}");

            Assert.That(saver.TryLoad(out var data, out var status), Is.True);
            Assert.That(status, Is.EqualTo(LoadStatus.RecoveredPending));
            Assert.That(data.Value, Is.EqualTo(21));
            Assert.That(File.Exists(location.FullPath), Is.True);
        }

        [Test]
        public void IncompletePendingTemp_WithNoOtherSave_IsTreatedAsMissing()
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(location.TempPath, "{\"Val"); // interrupted first-ever write

            Assert.That(saver.TryLoad(out _, out var status), Is.False);
            Assert.That(status, Is.EqualTo(LoadStatus.Missing));
        }

        [Test]
        public void MissingReload_ClearsPreviouslyCachedState()
        {
            saver.Save(new IOPersistenceData { Value = 99 });
            new FileStorageStrategy().Delete(location);

            Assert.That(saver.TryLoad(out _, out _), Is.False);
            Assert.That(saver.Load().Value, Is.Zero);
        }

        [Test]
        public void NullSave_ReportsFailure()
        {
            saver.Save(null);
            Assert.That(saver.LastSaveTask.Result, Is.False);
        }

        [Test]
        public void UnsafeRelativePath_FallsBackToRoot()
        {
            string uniqueName = "iotest_" + Guid.NewGuid().ToString("N");
            SetField("filePath", "../../outside");
            SetField("fileName", uniqueName);
            saver.Save(new IOPersistenceData { Value = 1 });

            string expected = Path.Combine(Path.GetFullPath(Application.persistentDataPath), uniqueName + ".json");
            try
            {
                Assert.That(File.Exists(expected), Is.True);
            }
            finally
            {
                if (File.Exists(expected)) File.Delete(expected);
            }
        }

        [TestCase(RuntimePlatform.Android, SaveLoadBase<IOPersistenceData>.SaveRoot.DataPath)]
        [TestCase(RuntimePlatform.Android, SaveLoadBase<IOPersistenceData>.SaveRoot.StreamingAssetsPath)]
        [TestCase(RuntimePlatform.IPhonePlayer, SaveLoadBase<IOPersistenceData>.SaveRoot.DataPath)]
        [TestCase(RuntimePlatform.WebGLPlayer, SaveLoadBase<IOPersistenceData>.SaveRoot.DataPath)]
        [TestCase(RuntimePlatform.WebGLPlayer, SaveLoadBase<IOPersistenceData>.SaveRoot.StreamingAssetsPath)]
        [TestCase(RuntimePlatform.WebGLPlayer, SaveLoadBase<IOPersistenceData>.SaveRoot.TemporaryCachePath)]
        public void PackagedOrVolatilePlayerRoot_UsesPersistentDataPath(RuntimePlatform platform, SaveLoadBase<IOPersistenceData>.SaveRoot root)
        {
            Assert.That(ResolveRoot(root, platform, false), Is.EqualTo(SaveLoadBase<IOPersistenceData>.SaveRoot.PersistentDataPath));
            Assert.That(ResolveRoot(root, platform, true), Is.EqualTo(root));
        }

        [Test]
        public void AndroidTemporaryCache_RemainsAnExplicitDisposableChoice()
        {
            var root = SaveLoadBase<IOPersistenceData>.SaveRoot.TemporaryCachePath;
            Assert.That(ResolveRoot(root, RuntimePlatform.Android, false), Is.EqualTo(root));
        }

        [Test]
        public void WebGlStorageRoot_UsesStableProductDirectory()
        {
            MethodInfo method = typeof(SaveLoadBase<IOPersistenceData>).GetMethod(
                "ResolveRootPath",
                BindingFlags.Static | BindingFlags.NonPublic);

            string path = (string)method.Invoke(null, new object[]
            {
                SaveLoadBase<IOPersistenceData>.SaveRoot.PersistentDataPath,
                RuntimePlatform.WebGLPlayer,
                false,
                "MyGame"
            });

            Assert.That(path, Is.EqualTo("/idbfs/MyGame"));
        }

        [Test]
        public void SerializationStrategyField_IsAManagedReference()
        {
            FieldInfo field = typeof(SaveLoadBase<IOPersistenceData>).GetField("serializationStrategy", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field.GetCustomAttribute<SerializeReference>(), Is.Not.Null,
                "an abstract strategy field only persists the inspector selection as [SerializeReference]");
        }

        private int ReadStoredValue()
        {
            Assert.That(new FileStorageStrategy().TryReadPrimary(location, out _), Is.True);
            var probe = new GameObject("IO probe");
            probe.SetActive(false);
            try
            {
                var reader = probe.AddComponent<IOPersistenceTestSaver>();
                typeof(SaveLoadBase<IOPersistenceData>).GetField("filePath", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(reader, GetField("filePath"));
                typeof(SaveLoadBase<IOPersistenceData>).GetField("storageStrategy", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(reader, new FileStorageStrategy());
                Assert.That(reader.TryLoad(out var data, out _), Is.True);
                return data.Value;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
            }
        }

        private string ReadRawPrimary() => File.ReadAllText(location.FullPath);

        private static object ResolveRoot(SaveLoadBase<IOPersistenceData>.SaveRoot root, RuntimePlatform platform, bool editor) =>
            typeof(SaveLoadBase<IOPersistenceData>).GetMethod("ResolveWritableRoot", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { root, platform, editor });

        private object GetField(string name) =>
            typeof(SaveLoadBase<IOPersistenceData>).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(saver);

        private void SetField(string name, object value) =>
            typeof(SaveLoadBase<IOPersistenceData>).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(saver, value);
    }
}
