using MyToolz.DesignPatterns.EventBus;
using MyToolz.InventorySystem.Models;
using MyToolz.InventorySystem.Settings;
using MyToolz.InventorySystem.Views;
using MyToolz.IO;
using MyToolz.Utilities.Debug;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace MyToolz.InventorySystem.Persistance
{
    public interface IInventorySaver<T> where T : ScriptableObject
    {
        /// <summary>Outcome of the last load. <see cref="LoadStatus.Unreadable"/> means a save exists but could not be decoded.</summary>
        LoadStatus LastLoadStatus { get; }

        /// <summary>True when a save was decoded — including a valid save of an empty inventory.</summary>
        bool HasSaveData();

        /// <summary>
        /// Loads the save and restores it into the model as one transaction (no intermediate writes).
        /// Returns true when a save was restored, even if it holds no items.
        /// </summary>
        bool TryRestoreModel();

        /// <summary>Kept for 1.x callers; equivalent to <see cref="TryRestoreModel"/>.</summary>
        void LoadIntoModel();

        IReadOnlyDictionary<T, int> GetCellPositions();
    }

    public abstract class InventorySaver<T> : SaveLoadBase<InventorySaveData>, IInventorySaver<T>
        where T : ScriptableObject
    {
        [SerializeField, Tooltip("When the stored inventory exists but cannot be decoded, overwrite it with the current inventory. " +
            "Off by default so an unreadable save (also kept as a .corrupt copy) is never replaced without the game deciding to.")]
        private bool replaceUnreadableSave;

        private IInventoryModel<T> model;
        private InventorySettingsSO<T> settings;

        private readonly Dictionary<T, int> cellIndexMap = new Dictionary<T, int>();

        private EventBinding<InventoryInitializeEvent<T>> initBinding;
        private EventBinding<InventoryCellDropEvent<T>> cellDropBinding;

        private bool modelSubscribed;
        private bool eventsRegistered;
        // Model changes are not persisted until the presenter reports the inventory as initialized.
        // Restoring or seeding starter items therefore produces one write, never partial states.
        private bool inventoryInitialized;
        private bool restoring;
        private bool loadAttempted;
        private bool reportedBlockedSave;
        private InventorySaveData loadedData;

        public override void InstallBindings()
        {
            base.InstallBindings();
            Container.Bind<IInventorySaver<T>>().FromInstance(this).AsSingle();
        }

        [Inject]
        private void Construct(IInventoryModel<T> model,
                                InventorySettingsSO<T> settings)
        {
            UnsubscribeModel();
            this.model    = model;
            this.settings = settings;
            if (isActiveAndEnabled) SubscribeModel();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RegisterEvents();
            SubscribeModel();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnregisterEvents();
            UnsubscribeModel();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnregisterEvents();
            UnsubscribeModel();
        }

        public override void Save()
        {
            PersistModel();
        }

        public bool HasSaveData()
        {
            EnsureLoaded();
            return LastLoadStatus.IsLoaded();
        }

        public bool TryRestoreModel()
        {
            EnsureLoaded();
            if (!LastLoadStatus.IsLoaded() || model == null) return false;

            restoring = true;
            try
            {
                cellIndexMap.Clear();
                var catalog = BuildCatalogLookup();
                var usedIndices = new HashSet<int>();

                foreach (var entry in loadedData.items.OrderBy(e => e.gridIndex))
                {
                    if (entry == null || entry.amount == 0) continue;
                    if (!TryResolveItem(catalog, entry, out T item))
                    {
                        DebugUtility.LogWarning(this, $"Saved item '{entry.itemId ?? entry.itemName}' is not in the item catalog and was skipped.");
                        continue;
                    }

                    int index = entry.gridIndex;
                    if (index < 0 || !usedIndices.Add(index))
                    {
                        index = NextFreeIndex(usedIndices);
                        usedIndices.Add(index);
                    }

                    cellIndexMap[item] = index;
                    model.Add(item, entry.amount);
                }
            }
            finally
            {
                restoring = false;
            }

            return true;
        }

        public void LoadIntoModel() => TryRestoreModel();

        public IReadOnlyDictionary<T, int> GetCellPositions()
        {
            return cellIndexMap;
        }

        private void EnsureLoaded()
        {
            if (loadAttempted) return;
            loadAttempted = true;
            TryLoad(out loadedData, out _);
            if (loadedData != null && loadedData.items == null)
                loadedData.items = new List<InventorySaveData.ItemEntry>();
        }

        private void RegisterEvents()
        {
            if (eventsRegistered) return;
            initBinding     = new EventBinding<InventoryInitializeEvent<T>>(OnInventoryInitialized);
            cellDropBinding = new EventBinding<InventoryCellDropEvent<T>>(OnCellDrop);
            EventBus<InventoryInitializeEvent<T>>.Register(initBinding);
            EventBus<InventoryCellDropEvent<T>>.Register(cellDropBinding);
            eventsRegistered = true;
        }

        private void UnregisterEvents()
        {
            if (!eventsRegistered) return;
            EventBus<InventoryInitializeEvent<T>>.Deregister(initBinding);
            EventBus<InventoryCellDropEvent<T>>.Deregister(cellDropBinding);
            eventsRegistered = false;
        }

        private void SubscribeModel()
        {
            if (modelSubscribed || model == null) return;
            model.OnItemUpdated += OnModelUpdated;
            modelSubscribed = true;
        }

        private void UnsubscribeModel()
        {
            if (!modelSubscribed) return;
            if (model != null) model.OnItemUpdated -= OnModelUpdated;
            modelSubscribed = false;
        }

        private void OnInventoryInitialized(InventoryInitializeEvent<T> e)
        {
            var usedIndices = new HashSet<int>(cellIndexMap.Values);
            if (e.Items != null)
            {
                foreach (var kvp in e.Items)
                {
                    if (kvp.Key == null || cellIndexMap.ContainsKey(kvp.Key)) continue;
                    int index = NextFreeIndex(usedIndices);
                    usedIndices.Add(index);
                    cellIndexMap[kvp.Key] = index;
                }
            }

            inventoryInitialized = true;
            PersistModel();
        }

        private void OnCellDrop(InventoryCellDropEvent<T> e)
        {
            if (e.SourceCell == e.TargetCell) return;
            if (e.DroppedItem == null) return;

            int sourceIdx = cellIndexMap.TryGetValue(e.DroppedItem, out int si) ? si : -1;

            if (e.TargetItem != null)
            {
                int targetIdx = cellIndexMap.TryGetValue(e.TargetItem, out int ti) ? ti : -1;
                if (targetIdx >= 0) cellIndexMap[e.DroppedItem] = targetIdx;
                if (sourceIdx >= 0) cellIndexMap[e.TargetItem] = sourceIdx;
            }
            else if (e.TargetCell != null)
            {
                cellIndexMap[e.DroppedItem] = e.TargetCell.transform.GetSiblingIndex();
            }

            PersistModel();
        }

        private void OnModelUpdated(T item, uint amount)
        {
            if (item == null) return;

            if (amount > 0 && !cellIndexMap.ContainsKey(item))
                cellIndexMap[item] = NextFreeIndex(new HashSet<int>(cellIndexMap.Values));
            else if (amount == 0)
                cellIndexMap.Remove(item);

            PersistModel();
        }

        private void PersistModel()
        {
            if (model == null || restoring || !inventoryInitialized) return;

            if (IsAutomaticSaveBlocked)
            {
                if (replaceUnreadableSave)
                {
                    DebugUtility.LogWarning(this, "Replacing an unreadable inventory save with the current inventory (Replace Unreadable Save is enabled).");
                    Save(BuildSaveData());
                    return;
                }

                if (!reportedBlockedSave)
                {
                    reportedBlockedSave = true;
                    DebugUtility.LogError(this, "The inventory save exists but could not be read, so inventory changes are not being saved. " +
                        "Call Save(data) to accept the current inventory, or enable Replace Unreadable Save.");
                }
                return;
            }

            SaveAutomatic(BuildSaveData());
        }

        private InventorySaveData BuildSaveData()
        {
            var data = new InventorySaveData();
            foreach (var kvp in model.InventoryItems)
            {
                if (kvp.Key == null) continue;
                data.items.Add(new InventorySaveData.ItemEntry
                {
                    itemId    = GetItemId(kvp.Key),
                    itemName  = kvp.Key.name,
                    amount    = kvp.Value,
                    gridIndex = cellIndexMap.TryGetValue(kvp.Key, out int idx) ? idx : -1
                });
            }
            data.items.Sort((a, b) => a.gridIndex.CompareTo(b.gridIndex));
            return data;
        }

        /// <summary>
        /// Stable identifier written to the save. <see cref="ItemSO"/> items use their GUID so renaming
        /// an asset does not orphan saved stacks; other item types fall back to the asset name.
        /// </summary>
        protected virtual string GetItemId(T item)
        {
            if (item is ItemSO itemSO && !string.IsNullOrWhiteSpace(itemSO.ItemGuid))
                return itemSO.ItemGuid;
            return item.name;
        }

        private Dictionary<string, T> BuildCatalogLookup()
        {
            var lookup = new Dictionary<string, T>();
            var catalog = settings?.ItemCatalog;
            if (catalog == null) return lookup;

            foreach (var item in catalog)
            {
                if (item == null) continue;
                string id = GetItemId(item);
                if (lookup.ContainsKey(id))
                {
                    DebugUtility.LogError(this, $"Item catalog contains the id '{id}' more than once ('{lookup[id].name}' and '{item.name}'). Saved stacks for it resolve to the first entry.");
                    continue;
                }
                lookup[id] = item;
            }
            return lookup;
        }

        private bool TryResolveItem(Dictionary<string, T> byId, InventorySaveData.ItemEntry entry, out T item)
        {
            if (!string.IsNullOrEmpty(entry.itemId) && byId.TryGetValue(entry.itemId, out item))
                return true;

            // 1.x saves only stored the asset name.
            item = settings?.ItemCatalog?.FirstOrDefault(i => i != null && i.name == entry.itemName);
            return item != null;
        }

        private static int NextFreeIndex(HashSet<int> used)
        {
            int index = 0;
            while (used.Contains(index)) index++;
            return index;
        }
    }
}
