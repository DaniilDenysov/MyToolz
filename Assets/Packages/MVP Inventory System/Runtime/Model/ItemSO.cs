using System;
using UnityEngine;
using MyToolz.EditorToolz;

namespace MyToolz.InventorySystem.Models
{
    public abstract class ItemSO : ScriptableObject
    {
        [SerializeField, FoldoutGroup("Item")] private string itemName;
        public string ItemName { get => itemName; }

        [SerializeField, FoldoutGroup("Item")] private Sprite itemIcon;
        public Sprite ItemIcon { get => itemIcon; }

        [SerializeField, TextArea, FoldoutGroup("Item")] private string itemDescription;
        public string ItemDescription { get => itemDescription; }

        [SerializeField, FoldoutGroup("Item")] private uint requiredLevel;
        public uint RequiredLevel => requiredLevel;
        [SerializeField, FoldoutGroup("Item")] private string itemGuid;
        public string ItemGuid { get => itemGuid; }

        [Button("Generate Guid")]
        private void GenerateGuid()
        {
            itemGuid = Guid.NewGuid().ToString();
        }

#if UNITY_EDITOR
        public void Awake()
        {
            if (!(string.IsNullOrEmpty(itemGuid) || string.IsNullOrWhiteSpace(itemGuid))) return;
            GenerateGuid();
        }
#endif
    }
}
