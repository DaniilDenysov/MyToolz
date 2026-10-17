using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyToolz.InventorySystem.Models;
using MyToolz.InventorySystem.Persistance;
using MyToolz.InventorySystem.Settings;
using MyToolz.InventorySystem.Views;
using MyToolz.IO;
using NUnit.Framework;
using UnityEngine;

namespace MyToolz.Tests.EditMode
{
    public class TestInventoryItem : ItemSO { }

    [Serializable]
    public class TestInventoryModel : InventoryModel<TestInventoryItem> { }

    public class TestInventorySettings : InventorySettingsSO<TestInventoryItem> { }

    public class TestInventorySaver : InventorySaver<TestInventoryItem>
    {
        protected override void Awake() { }
    }

    /// <summary>In-memory storage that counts writes, so tests can assert "no intermediate saves".</summary>
    [Serializable]
    public class CountingMemoryStorage : StorageStrategy
    {
        public readonly Dictionary<string, string> Slots = new Dictionary<string, string>();
        public int WriteCount;

        public override void Write(in StorageLocation location, string content)
        {
            WriteCount++;
            Slots[location.Key] = content;
        }

        public override bool TryReadPrimary(in StorageLocation location, out string content) =>
            Slots.TryGetValue(location.Key, out content);

        public override bool Exists(in StorageLocation location) => Slots.ContainsKey(location.Key);

        public override void Delete(in StorageLocation location) => Slots.Remove(location.Key);
    }

    public class InventoryModelTests
    {
        private TestInventoryItem sword;

        [SetUp] public void SetUp() => sword = ScriptableObject.CreateInstance<TestInventoryItem>();
        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(sword);

        [Test]
        public void Remove_MoreThanAvailable_ClampsToZeroAndRemovesStack()
        {
            var model = new TestInventoryModel();
            model.Add(sword, 3);
            uint reported = 99;
            model.OnItemUpdated += (_, amount) => reported = amount;

            model.Remove(sword, 10);

            Assert.That(reported, Is.Zero);
            Assert.That(model.InventoryItems.ContainsKey(sword), Is.False);
        }

        [Test]
        public void Add_SaturatesInsteadOfOverflowing()
        {
            var model = new TestInventoryModel();
            model.Add(sword, uint.MaxValue - 1);
            model.Add(sword, 5);

            Assert.That(model.InventoryItems[sword], Is.EqualTo(uint.MaxValue));
        }

        [Test]
        public void Add_NullItem_IsIgnored()
        {
            var model = new TestInventoryModel();
            model.Add(null, 1);
            Assert.That(model.InventoryItems, Is.Empty);
        }
    }

    public class InventorySaverTests : SilentLogTest
    {
        private GameObject owner;
        private TestInventorySaver saver;
        private TestInventoryModel model;
        private TestInventorySettings settings;
        private CountingMemoryStorage storage;
        private TestInventoryItem sword;
        private TestInventoryItem shield;
        private const string Key = "Saves/inventory.json";

        [SetUp]
        public void SetUp()
        {
            sword = CreateItem("Sword", "guid-sword");
            shield = CreateItem("Shield", "guid-shield");
            settings = ScriptableObject.CreateInstance<TestInventorySettings>();
            SetPrivate(typeof(InventorySettingsSO<TestInventoryItem>), settings, "itemCatalog", new[] { sword, shield });

            storage = new CountingMemoryStorage();
            model = new TestInventoryModel();
            owner = new GameObject("inventory saver");
            owner.SetActive(false);
            saver = owner.AddComponent<TestInventorySaver>();
            SetPrivate(typeof(SaveLoadBase<InventorySaveData>), saver, "storageStrategy", storage);
            SetPrivate(typeof(SaveLoadBase<InventorySaveData>), saver, "fileName", "inventory");
            Invoke("Construct", model, settings);
            Invoke("OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(owner);
            UnityEngine.Object.DestroyImmediate(settings);
            UnityEngine.Object.DestroyImmediate(sword);
            UnityEngine.Object.DestroyImmediate(shield);
        }

        [Test]
        public void Restore_AddsItems_WithoutIntermediateWrites()
        {
            Store(new InventorySaveData
            {
                items =
                {
                    new InventorySaveData.ItemEntry { itemId = "guid-sword", itemName = "Sword", amount = 2, gridIndex = 0 },
                    new InventorySaveData.ItemEntry { itemId = "guid-shield", itemName = "Shield", amount = 1, gridIndex = 3 },
                }
            });
            int writesBefore = storage.WriteCount;

            Assert.That(saver.TryRestoreModel(), Is.True);

            Assert.That(model.InventoryItems[sword], Is.EqualTo(2u));
            Assert.That(model.InventoryItems[shield], Is.EqualTo(1u));
            Assert.That(storage.WriteCount, Is.EqualTo(writesBefore), "restoring must not save partially rebuilt inventories");
            Assert.That(saver.GetCellPositions()[shield], Is.EqualTo(3));
        }

        [Test]
        public void EmptySave_CountsAsSaveData()
        {
            Store(new InventorySaveData());

            Assert.That(saver.HasSaveData(), Is.True, "an empty inventory is a valid save, not a missing one");
            Assert.That(saver.TryRestoreModel(), Is.True);
            Assert.That(model.InventoryItems, Is.Empty);
        }

        [Test]
        public void MissingSave_IsNotSaveData()
        {
            Assert.That(saver.HasSaveData(), Is.False);
            Assert.That(saver.LastLoadStatus, Is.EqualTo(LoadStatus.Missing));
        }

        [Test]
        public void Restore_ResolvesByStableId_WhenAssetWasRenamed()
        {
            Store(new InventorySaveData
            {
                items = { new InventorySaveData.ItemEntry { itemId = "guid-sword", itemName = "OldSwordName", amount = 4, gridIndex = 0 } }
            });

            saver.TryRestoreModel();

            Assert.That(model.InventoryItems[sword], Is.EqualTo(4u));
        }

        [Test]
        public void Restore_ResolvesLegacyNameOnlyEntries()
        {
            Store(new InventorySaveData
            {
                items = { new InventorySaveData.ItemEntry { itemName = "Shield", amount = 1, gridIndex = 0 } }
            });

            saver.TryRestoreModel();

            Assert.That(model.InventoryItems[shield], Is.EqualTo(1u));
        }

        [Test]
        public void Restore_DuplicateCellIndices_AreMadeUnique()
        {
            Store(new InventorySaveData
            {
                items =
                {
                    new InventorySaveData.ItemEntry { itemId = "guid-sword", amount = 1, gridIndex = 2 },
                    new InventorySaveData.ItemEntry { itemId = "guid-shield", amount = 1, gridIndex = 2 },
                }
            });

            saver.TryRestoreModel();
            var positions = saver.GetCellPositions();

            Assert.That(positions[sword], Is.Not.EqualTo(positions[shield]));
        }

        [Test]
        public void NewItem_TakesLowestFreeCell_NotTheCount()
        {
            Store(new InventorySaveData
            {
                items = { new InventorySaveData.ItemEntry { itemId = "guid-sword", amount = 1, gridIndex = 1 } }
            });
            saver.TryRestoreModel();
            RaiseInitialized();

            model.Add(shield, 1);

            Assert.That(saver.GetCellPositions()[shield], Is.EqualTo(0), "index 1 (== count) would collide with the sword");
        }

        [Test]
        public void ModelChanges_ArePersistedOnlyAfterInitialization()
        {
            model.Add(sword, 1);
            Assert.That(storage.WriteCount, Is.Zero, "starter items are not saved one by one");

            RaiseInitialized();
            Assert.That(storage.WriteCount, Is.EqualTo(1), "initialization writes the whole inventory once");

            model.Add(sword, 1);
            Assert.That(storage.WriteCount, Is.EqualTo(2));
            Assert.That(storage.Slots[Key], Does.Contain("guid-sword"));
        }

        [Test]
        public void DisableThenEnable_RestoresModelSubscription()
        {
            RaiseInitialized();
            Invoke("OnDisable");
            Invoke("OnEnable");
            int before = storage.WriteCount;

            model.Add(shield, 1);

            Assert.That(storage.WriteCount, Is.EqualTo(before + 1));
        }

        [Test]
        public void UnreadableSave_IsNotOverwrittenByModelChanges()
        {
            storage.Slots[Key] = "{ not json";

            Assert.That(saver.TryRestoreModel(), Is.False);
            Assert.That(saver.LastLoadStatus, Is.EqualTo(LoadStatus.Unreadable));
            RaiseInitialized();
            model.Add(sword, 1);

            Assert.That(storage.Slots[Key], Is.EqualTo("{ not json"));
        }

        [Test]
        public void UnreadableSave_IsReplaced_WhenPolicyAllowsIt()
        {
            storage.Slots[Key] = "{ not json";
            SetPrivate(typeof(InventorySaver<TestInventoryItem>), saver, "replaceUnreadableSave", true);

            saver.TryRestoreModel();
            RaiseInitialized();
            model.Add(sword, 1);

            Assert.That(storage.Slots[Key], Does.Contain("guid-sword"));
        }

        private void Store(InventorySaveData data)
        {
            storage.Slots[Key] = Newtonsoft.Json.JsonConvert.SerializeObject(data);
        }

        private void RaiseInitialized()
        {
            Invoke("OnInventoryInitialized", new InventoryInitializeEvent<TestInventoryItem>
            {
                Items = model.InventoryItems,
                CellPositions = saver.GetCellPositions()
            });
        }

        private static TestInventoryItem CreateItem(string name, string guid)
        {
            var item = ScriptableObject.CreateInstance<TestInventoryItem>();
            item.name = name;
            SetPrivate(typeof(ItemSO), item, "itemGuid", guid);
            return item;
        }

        private void Invoke(string method, params object[] args)
        {
            for (Type type = saver.GetType(); type != null; type = type.BaseType)
            {
                MethodInfo info = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .FirstOrDefault(m => m.Name == method && m.GetParameters().Length == args.Length);
                if (info == null) continue;
                info.Invoke(saver, args);
                return;
            }
            Assert.Fail($"Method {method} not found");
        }

        private static void SetPrivate(Type type, object target, string field, object value)
        {
            FieldInfo info = type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(info, $"{type.Name}.{field} not found");
            info.SetValue(target, value);
        }
    }
}
