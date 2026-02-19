using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.InventorySystem.Views
{
    public interface IInventoryItemView<ItemSO> where ItemSO : ScriptableObject
    {
        public event Action<ItemSO, uint> OnValueChanged;
        public void Initialize(ItemSO inventoryItemSO, uint amount = 1);
    }

    public abstract class InventoryItemView<ItemSO> : MonoBehaviour, IInventoryItemView<ItemSO> where ItemSO : ScriptableObject
    {
        public event Action<ItemSO, uint> OnValueChanged;

        [SerializeField] protected TMP_Text quantityDisplay;
        [SerializeField] protected TMP_Text nameDisplay;
        [SerializeField] protected Image icon;
        protected ItemSO inventoryItemSO;

        public virtual void Initialize(ItemSO inventoryItemSO, uint amount = 1)
        {
            this.inventoryItemSO = inventoryItemSO;
            nameDisplay.text = $"{inventoryItemSO.name}";
            quantityDisplay.text = $"{amount}";
        }

        public virtual void Purchase(int amount)
        {
            if (inventoryItemSO == null) return;
            OnValueChanged?.Invoke(inventoryItemSO, (uint)Mathf.Max(0, amount));
        }
    }
}
