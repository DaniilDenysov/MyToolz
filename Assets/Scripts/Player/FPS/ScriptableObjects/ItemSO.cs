using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using MyToolz.Player.FPS.LoadoutSystem.View;
using MyToolz.Player.FPS.CombatSystem.View;
using MyToolz.EditorToolz;

namespace MyToolz.Player.FPS.Inventory
{
    [CreateAssetMenu(fileName = "create new weapon", menuName = "NoSaints/CombatSystem/Weapon")]
    public class ItemSO : ScriptableObject
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

        [SerializeField, FoldoutGroup("Item")] private HudItem hudDisplayItem;
        public HudItem HudDisplayItem { get => hudDisplayItem; }

        [SerializeField, FoldoutGroup("Item")] private LoadoutSlotCategory loadoutSlotCategory;
        public LoadoutSlotCategory LoadoutCategory { get => loadoutSlotCategory; }

        [Button("Generate Guid")]
        private void GenerateWeaponGuid()
        {
            itemGuid = Guid.NewGuid().ToString();
        }

        [SerializeField, FoldoutGroup("Item")] private InputActionReference hotKey;
        public InputActionReference HotKey
        {
            get => hotKey;
        }

        public string HotKeyName
        {
            get
            {
                foreach (var binding in HotKey.action.bindings)
                {
                    if (binding.isPartOfComposite) continue;
                    string keyBindingPath = binding.effectivePath;
                    string keyName = keyBindingPath.Split('/').Last();
                    if (string.IsNullOrEmpty(keyName)) continue;
                    return keyName.ToUpper();
                }
                return "Null";
            }
        }


#if UNITY_EDITOR
        public void Awake()
        {
            if (!(string.IsNullOrEmpty(itemGuid) || string.IsNullOrWhiteSpace(itemGuid))) return;
            GenerateWeaponGuid();
        }
#endif
    }
}
