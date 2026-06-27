using MyToolz.EditorToolz;
using System;
using UnityEngine;

namespace MyToolz.InventorySystem.Models
{
    public abstract class ItemSO : ScriptableObject
    {
        [SerializeField, FoldoutGroup("Item"), Required] private string itemName;
        public string ItemName { get => itemName; }

        [SerializeField, FoldoutGroup("Item"), Required] private Sprite itemIcon;
        public Sprite ItemIcon { get => itemIcon; }

        [SerializeField, TextArea, FoldoutGroup("Item")] private string itemDescription;
        public string ItemDescription { get => itemDescription; }

        [SerializeField, FoldoutGroup("Item"), ReadOnly] private string itemGuid;
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
