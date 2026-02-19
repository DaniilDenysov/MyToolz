using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;
using UnityEngine;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.UI.Management;

namespace MyToolz.InventorySystem.Views
{
    public interface IInventoryView<T> where T : ScriptableObject
    {
        public event Action<T, uint> OnItemAmountChanged;
        public void Initialize(IReadOnlyDictionary<T, uint> items);
        public void UpdateItem(T item, uint  newAmount);
        public void Open();
        public void Close();
    }

    public abstract class InventoryView<ItemSO> : UIScreen, IInventoryView<ItemSO> where ItemSO : ScriptableObject
    {
        [SerializeField] protected RectTransform container;
        [SerializeField] protected InventoryItemView<ItemSO> inventoryItemViewPrefab;
        public event Action<ItemSO, uint> OnItemAmountChanged;
        protected Dictionary<ItemSO, InventoryItemView<ItemSO>> itemViews = new Dictionary<ItemSO, InventoryItemView<ItemSO>>();


        public virtual void Initialize(IReadOnlyDictionary<ItemSO, uint> items)
        {
            foreach (var item in items)
            {
                UpdateItem(item.Key, item.Value);
                DebugUtility.Log(this, "Updated");
            }
        }

        public abstract void Enter();

        public abstract void Exit();

        public virtual void UpdateItem(ItemSO itemSO, uint amount)
        {
            if (amount > 0)
            {
                if (itemViews.TryGetValue(itemSO, out var itemView))
                {
                    itemView.Initialize(itemSO, amount);
                }
                else
                {
                    EventBus<PoolRequest<InventoryItemView<ItemSO>>>.Raise(new PoolRequest<InventoryItemView<ItemSO>>()
                    {
                        Prefab = inventoryItemViewPrefab,
                        Parent = container,
                        Callback = (itm) =>
                        {
                            if (itm.TryGetComponent(out IInventoryItemView<ItemSO> item))
                            {
                                if (itm.TryGetComponent(out RectTransform rectTransform))
                                {
                                    rectTransform.localScale = Vector3.one;
                                }
                                item.Initialize(itemSO, amount);
                                item.OnValueChanged += PurchaseItem;
                                itemViews.Add(itemSO, itm);
                            }
                        }
                    });
                }
            }
            else
            {
                if (itemViews.TryGetValue(itemSO, out var itemView))
                {
                    EventBus<ReleaseRequest<InventoryItemView<ItemSO>>>.Raise(new ReleaseRequest<InventoryItemView<ItemSO>>()
                    {
                        PoolObject = itemView,
                        Callback = (itm) =>
                        {
                            if (itm.TryGetComponent(out IInventoryItemView<ItemSO> item))
                            {

                                item.OnValueChanged -= PurchaseItem;
                                itemViews.Remove(itemSO);
                            }
                        }
                    });
                }
            }

        }

        private void PurchaseItem(ItemSO itm, uint amount)
        {
            DebugUtility.Log(this, "Changed amount!");
            OnItemAmountChanged?.Invoke(itm, amount);
        }
    }
}
