using MyToolz.InventorySystem.Models;
using MyToolz.InventorySystem.Views;
using UnityEngine;

namespace MyToolz.InventorySystem.Settings
{
    public abstract class InventorySettingsSO<T> : ScriptableObject where T : ScriptableObject
    {
        [SerializeField] private InventoryItemView<T> itemViewPrefab;
        [SerializeField] private InventoryCell<T> cellPrefab;
        [SerializeField] private int inventorySize = 20;
        [SerializeField] private InitialItem<T>[] initialItems;
        [SerializeField] private T[] itemCatalog;

        public InventoryItemView<T> ItemViewPrefab => itemViewPrefab;
        public InventoryCell<T> CellPrefab => cellPrefab;
        public int InventorySize => inventorySize;
        public InitialItem<T>[] InitialItems => initialItems;
        public T[] ItemCatalog => itemCatalog;
    }
}
