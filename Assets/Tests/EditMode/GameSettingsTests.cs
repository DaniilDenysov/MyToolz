using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyToolz.GameSettings;
using MyToolz.GameSettings.Data;
using MyToolz.IO;
using MyToolz.ScriptableObjects.GameSettings;
using NUnit.Framework;
using UnityEngine;

namespace MyToolz.Tests.EditMode
{
    /// <summary>Recoverable saver fake that records every explicit save.</summary>
    internal sealed class FakeSettingsSaver : IRecoverableSaver<SavableData>
    {
        public SavableData Stored;
        public LoadStatus StatusToReport = LoadStatus.Missing;
        public int SaveCount;

        public LoadStatus LastLoadStatus { get; private set; } = LoadStatus.NotAttempted;

        public void Save() { }

        public void Save(SavableData obj)
        {
            SaveCount++;
            Stored = obj;
        }

        public SavableData Load() => Stored ?? new SavableData();

        public bool TryLoad(out SavableData data, out LoadStatus status)
        {
            status = LastLoadStatus = StatusToReport;
            data = StatusToReport.IsLoaded() ? Stored : null;
            return data != null;
        }
    }

    public class SettingTwinTests : SilentLogTest
    {
        private readonly List<ScriptableObject> created = new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject so in created) Object.DestroyImmediate(so);
            created.Clear();
        }

        [Test]
        public void SetCurrentValue_IsMirroredToTwinsWithTheSameId()
        {
            BoolSettingSO original = Create<BoolSettingSO>("Vsync");
            BoolSettingSO twin = CreateTwinOf(original);

            original.SetCurrentValue(true);

            Assert.That(twin.CurrentValue, Is.True);
            Assert.That(twin.HasValue, Is.True);
        }

        [Test]
        public void TwinCreatedLater_AdoptsTheCurrentValue()
        {
            BoolSettingSO original = Create<BoolSettingSO>("Vsync");
            original.SetCurrentValue(true);

            BoolSettingSO twin = CreateTwinOf(original);

            Assert.That(twin.CurrentValue, Is.True);
        }

        [Test]
        public void CompleteLoad_RaisesLoadCompletedOnTwins()
        {
            BoolSettingSO original = Create<BoolSettingSO>("Vsync");
            BoolSettingSO twin = CreateTwinOf(original);
            bool twinCompleted = false;
            twin.OnLoadCompleted += () => twinCompleted = true;

            original.CompleteLoad();

            Assert.That(twinCompleted, Is.True);
            Assert.That(twin.IsLoadFinished, Is.True);
        }

        [Test]
        public void DifferentTypeWithSameId_IsNotATwin()
        {
            BoolSettingSO flag = Create<BoolSettingSO>("Flag");
            StringSettingSO text = Create<StringSettingSO>("Text");
            Rekey(text, flag.ID);

            flag.SetCurrentValue(true);

            Assert.That(text.HasValue, Is.False);
        }

        private T Create<T>(string assetName) where T : SettingSOAbstract
        {
            T so = ScriptableObject.CreateInstance<T>();
            so.name = assetName;
            created.Add(so);
            return so;
        }

        private BoolSettingSO CreateTwinOf(BoolSettingSO original)
        {
            BoolSettingSO twin = Create<BoolSettingSO>(original.name);
            Rekey(twin, original.ID);
            return twin;
        }

        // Simulates a second loaded copy of the same asset: re-registers the instance under another ID.
        private static void Rekey(SettingSOAbstract so, string id)
        {
            MethodInfo onDisable = typeof(SettingSOAbstract).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo onEnable = so.GetType().GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
            onDisable.Invoke(so, null);
            typeof(SettingSOAbstract).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(so, id);
            onEnable.Invoke(so, null);
        }
    }

    public class SettingsPresenterTests : SilentLogTest
    {
        private GameObject owner;
        private SettingsPresenter presenter;
        private FakeSettingsSaver saver;
        private BoolSettingSO setting;

        [SetUp]
        public void SetUp()
        {
            setting = ScriptableObject.CreateInstance<BoolSettingSO>();
            saver = new FakeSettingsSaver();
            owner = new GameObject("settings presenter");
            owner.SetActive(false);
            presenter = owner.AddComponent<SettingsPresenter>();
            Set("settings", new SettingSOAbstract[] { setting });
            Set("saveDelay", 0f);
            Call("Construct", saver);
            Call("Awake");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(setting);
        }

        [Test]
        public void Start_AppliesSavedValues_AndCompletesLoad()
        {
            saver.Stored = new SavableData { Data = { new SettingEntry(setting.ID, true) } };
            saver.StatusToReport = LoadStatus.LoadedPrimary;

            Call("Start");

            Assert.That(setting.CurrentValue, Is.True);
            Assert.That(setting.IsLoadFinished, Is.True);
            Assert.That(presenter.IsLoaded, Is.True);
        }

        [Test]
        public void RapidChanges_AreCoalescedIntoOneSave()
        {
            Call("Start");

            setting.SetCurrentValue(true);
            setting.SetCurrentValue(false);
            setting.SetCurrentValue(true);
            Assert.That(saver.SaveCount, Is.Zero, "changes are debounced, not written one by one");

            Call("Update");

            Assert.That(saver.SaveCount, Is.EqualTo(1));
            Assert.That(saver.Stored.Data.Single().GetValue<bool>(), Is.True);
        }

        [Test]
        public void Flush_WithoutChanges_DoesNotWrite()
        {
            Call("Start");

            presenter.Flush();

            Assert.That(saver.SaveCount, Is.Zero);
        }

        [Test]
        public void UnreadableSave_IsNotOverwritten_UntilASettingChanges()
        {
            saver.StatusToReport = LoadStatus.Unreadable;
            Call("Start");

            presenter.Flush();
            Assert.That(saver.SaveCount, Is.Zero, "defaults never replace an unreadable save on their own");
            Assert.That(presenter.LoadStatus, Is.EqualTo(LoadStatus.Unreadable));

            setting.SetCurrentValue(true);
            presenter.Flush();
            Assert.That(saver.SaveCount, Is.EqualTo(1), "a player change is an explicit decision to write new settings");
        }

        private void Call(string method, params object[] args) =>
            typeof(SettingsPresenter).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Invoke(presenter, args);

        private void Set(string field, object value) =>
            typeof(SettingsPresenter).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(presenter, value);
    }
}
