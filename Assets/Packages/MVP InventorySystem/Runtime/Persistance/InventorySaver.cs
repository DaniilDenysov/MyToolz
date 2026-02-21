using MyToolz.DesignPatterns.EventBus;
using MyToolz.InventorySystem.Models;
using MyToolz.InventorySystem.Settings;
using MyToolz.InventorySystem.Views;
using MyToolz.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace MyToolz.InventorySystem.Persistance
{
    public interface IInventorySaver<T> where T : ScriptableObject
    {
        bool HasSaveData();
        void LoadIntoModel();
        IReadOnlyDictionary<T, int> GetCellPositions();
    }

    public abstract class InventorySaver<T> : SaveLoadBase<InventorySaveData>, IInventorySaver<T>
        where T : ScriptableObject
    {
        private IInventoryModel<T> model;
        private InventorySettingsSO<T> settings;

        private readonly Dictionary<T, int> cellIndexMap = new Dictionary<T, int>();

        private EventBinding<InventoryInitializeEvent<T>> initBinding;
        private EventBinding<InventoryCellDropEvent<T>> cellDropBinding;

        public override void InstallBindings()
        {
            base.InstallBindings();
            Container.Bind<IInventorySaver<T>>().FromInstance(this).AsSingle();
        }

        [Inject]
        private void Construct(IInventoryModel<T> model,
                                InventorySettingsSO<T> settings)
        {
            this.model    = model;
            this.settings = settings;
            model.OnItemUpdated += OnModelUpdated;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            initBinding     = new EventBinding<InventoryInitializeEvent<T>>(OnInventoryInitialized);
            cellDropBinding = new EventBinding<InventoryCellDropEvent<T>>(OnCellDrop);
            EventBus<InventoryInitializeEvent<T>>.Register(initBinding);
            EventBus<InventoryCellDropEvent<T>>.Register(cellDropBinding);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventBus<InventoryInitializeEvent<T>>.Deregister(initBinding);
            EventBus<InventoryCellDropEvent<T>>.Deregister(cellDropBinding);
            if (model != null) model.OnItemUpdated -= OnModelUpdated;
        }

        protected virtual void OnDestroy()
        {
            EventBus<InventoryInitializeEvent<T>>.Deregister(initBinding);
            EventBus<InventoryCellDropEvent<T>>.Deregister(cellDropBinding);
            if (model != null) model.OnItemUpdated -= OnModelUpdated;
        }

        public override void Save()
        {
            if (model == null) return;
            cache = BuildSaveData();
            Save(cache);
        }

        public bool HasSaveData()
        {
            var data = Load();
            return data != null && data.items != null && data.items.Count > 0;
        }

        public void LoadIntoModel()
        {
            var data = Load();
            if (data == null || data.items == null || data.items.Count == 0) return;

            cellIndexMap.Clear();
            foreach (var entry in data.items.OrderBy(e => e.gridIndex))
            {
                var item = settings?.ItemCatalog?.FirstOrDefault(i => i.name == entry.itemName);
                if (item != null)
                {
                    cellIndexMap[item] = entry.gridIndex;
                    model.Add(item, entry.amount);
                }
            }
        }

        public IReadOnlyDictionary<T, int> GetCellPositions()
        {
            return cellIndexMap;
        }

        private void OnInventoryInitialized(InventoryInitializeEvent<T> e)
        {
            if (cellIndexMap.Count > 0) return;
            int index = 0;
            foreach (var kvp in e.Items)
                cellIndexMap[kvp.Key] = index++;
        }

        private void OnCellDrop(InventoryCellDropEvent<T> e)
        {
            if (e.DroppedItem == null) return;

            int sourceIdx = cellIndexMap.TryGetValue(e.DroppedItem, out int si) ? si : -1;

            if (e.TargetItem != null)
            {
                int targetIdx = cellIndexMap.TryGetValue(e.TargetItem, out int ti) ? ti : -1;
                if (sourceIdx >= 0) cellIndexMap[e.DroppedItem] = targetIdx;
                if (targetIdx >= 0) cellIndexMap[e.TargetItem] = sourceIdx;
            }
            else
            {
                if (e.TargetCell != null)
                {
                    int targetIdx = e.TargetCell.transform.GetSiblingIndex();
                    cellIndexMap[e.DroppedItem] = targetIdx;
                }
            }

            cache = BuildSaveData();
        }

        private void OnModelUpdated(T item, uint amount)
        {
            if (amount > 0 && !cellIndexMap.ContainsKey(item))
                cellIndexMap[item] = cellIndexMap.Count;
            else if (amount == 0)
                cellIndexMap.Remove(item);

            cache = BuildSaveData();
        }

        private InventorySaveData BuildSaveData()
        {
            var data = new InventorySaveData();
            foreach (var kvp in model.InventoryItems)
            {
                data.items.Add(new InventorySaveData.ItemEntry
                {
                    itemName  = kvp.Key.name,
                    amount    = kvp.Value,
                    gridIndex = cellIndexMap.TryGetValue(kvp.Key, out int idx) ? idx : -1
                });
            }
            data.items.Sort((a, b) => a.gridIndex.CompareTo(b.gridIndex));
            return data;
        }
    }
}


