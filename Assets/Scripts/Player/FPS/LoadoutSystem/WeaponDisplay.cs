using MyToolz.Player.FPS.CombatSystem.Model;
using MyToolz.Player.FPS.CombatSystem.View;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.Player.FPS.CombatSystem
{
    public class WeaponDisplay : ItemDisplay<WeaponModel>
    {
        [SerializeField, Required] private TMP_Text currentAmmo;
        [SerializeField, Required] private TMP_Text binding;
        [SerializeField, Required] private Image icon;

        public override void Initialize(WeaponModel model)
        {
            base.Initialize(model);
            if (model == null) return;
            var itemSO = model.GetItemSO();
            if (currentAmmo != null)
            {
                string totalValue = model.MaxBullets <= 0 ? "" : $"{model.BulletsTotal}";
                currentAmmo.text = totalValue;
            }
            if (icon != null)
            {
                icon.sprite = itemSO.ItemIcon;
            }
            if (binding != null)
            {
                binding.text = itemSO.HotKeyName;
            }
        }

        public override void UpdateView(WeaponModel model)
        {
            if (model == null) return;
            if (currentAmmo == null) return;
            string totalValue = model.MaxBullets <= 0 ? "" : $"{model.BulletsTotal}";
            currentAmmo.text = totalValue;
        }
    }
}
