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
        public void Initialize(InitialItem<T>[] items = null);
        public void Add(T item, uint amount = 1);
        public void Remove(T item, uint amount = 1);
    }

    [System.Serializable]
    public struct InitialItem<T>
    {
        public T Item;
        public uint Amount;
    }

    [System.Serializable]
    public abstract class InventoryModel<T> : IInventoryModel<T> where T : ScriptableObject
    {
        protected InitialItem<T> [] initialPool; 
        public event Action<T, uint> OnItemUpdated;
        protected Dictionary<T, uint> inventoryItems = new Dictionary<T, uint>();

        public IReadOnlyDictionary<T, uint> InventoryItems
        {
            get => inventoryItems;
        }

        public virtual void Initialize(InitialItem<T>[] items = null)
        {
            if (items != null && items.Length > 0)
            {
                foreach (var entry in items)
                {
                    if (entry.Item != null)
                        Add(entry.Item, entry.Amount);
                }
            }
        }

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
            if (newAmount == 0)
            {
                inventoryItems.Remove(inventoryItemSO);
            }
            else
            {
                inventoryItems[inventoryItemSO] = newAmount;
            }
            OnItemUpdated?.Invoke(inventoryItemSO, newAmount);
        }
    }
}
