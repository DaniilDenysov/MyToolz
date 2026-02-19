using UnityEngine;
using MyToolz.InventorySystem.Views;
using MyToolz.EditorToolz;

namespace MyToolz.ScriptableObjects.Inventory
{
    public abstract class ItemSO : ScriptableObject
    {
        [FoldoutGroup("Item"), Required]
        [SerializeField] protected Sprite icon;

        [FoldoutGroup("Item")]
        [SerializeField] protected string itemName;
        [FoldoutGroup("Item"), SerializeField] protected InventoryItemView<ItemSO> inventoryItemViewPrefab;
        [FoldoutGroup("Item"), TextArea(3, 6)]
        [SerializeField] private string itemDescription;
        [FoldoutGroup("Item"), SerializeField, Range(0f, 360f)] protected float usageCoolDown;
        public float UsageCoolDown => usageCoolDown;
        public string ItemName => itemName;
        public Sprite Icon => icon;
        public string Description => itemDescription;
        public InventoryItemView<ItemSO> InventoryItemViewPrefab => inventoryItemViewPrefab;
    }
}
