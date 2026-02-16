using UnityEngine;
using Sirenix.OdinInspector;
using MyToolz.InventorySystem.Views;

namespace MyToolz.ScriptableObjects.Inventory
{
    public abstract class ItemSO : ScriptableObject
    {
        [FoldoutGroup("Item"), HorizontalGroup("Item/Header", 64)]
        [VerticalGroup("Item/Header/Left", 64), PreviewField(64, ObjectFieldAlignment.Left), HideLabel, Required]
        [SerializeField] protected Sprite icon;

        [FoldoutGroup("Item"), VerticalGroup("Item/Header/Right"), LabelWidth(100), Required, DelayedProperty]
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
