using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.InventorySystem.Models
{
    public interface IInventoryModel<T> where T : ScriptableObject
    {
        public event Action<T, uint> OnItemUpdated;
        public IReadOnlyDictionary<T, uint> InventoryItems {  get; }
        public void Initialize();
        public void Add(T item, uint amount = 1);
        public void Remove(T item, uint amount = 1);
    }

    [System.Serializable]
    public abstract class InventoryModel<T> : IInventoryModel<T> where T : ScriptableObject
    {
        public event Action<T, uint> OnItemUpdated;
        protected Dictionary<T, uint> inventoryItems = new Dictionary<T, uint>();

        public IReadOnlyDictionary<T, uint> InventoryItems
        {
            get => inventoryItems;
        }

        public abstract void Initialize();

        public virtual void Add(T inventoryItemSO, uint amount = 1)
        {
            if (amount == 0) return; 
            if (!inventoryItems.TryAdd(inventoryItemSO,amount))
            {
                inventoryItems[inventoryItemSO] += amount;
            }
            OnItemUpdated?.Invoke(inventoryItemSO, inventoryItems[inventoryItemSO]);
        }

        public virtual void Remove(T inventoryItemSO, uint amount = 1)
        {
            if (amount == 0 || !inventoryItems.TryGetValue(inventoryItemSO, out uint currentAmount))
                return;

            uint newAmount = (uint)Mathf.Max(0, currentAmount - amount);
            DebugUtility.Log(this, $"{newAmount}");
            if (newAmount > 0)
            {
                inventoryItems[inventoryItemSO] = newAmount;
                OnItemUpdated?.Invoke(inventoryItemSO, newAmount);
            }
            OnItemUpdated?.Invoke(inventoryItemSO, newAmount);
        }
    }
}
