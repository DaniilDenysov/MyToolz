using MyToolz.DesignPatterns.MVP.View;
using MyToolz.EditorToolz;
using MyToolz.Player.FPS.CombatSystem.Model;
using MyToolz.Tweener.UI;
using MyToolz.Utilities.Debug;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Zenject;

namespace MyToolz.Player.FPS.CombatSystem.View
{
    public class WeaponView : MonoInstaller, IReadOnlyView<WeaponModel>
    {
        [SerializeField] private UITweener tweener;

        [Header("WeaponView")]
        [SerializeField, Required] private Transform root;
        [SerializeField, Required] private ItemDisplay<WeaponModel> weaponDisplayPrefab;
        [SerializeField, Required] private TMP_Text currentAmmo;
        [SerializeField, Required] private TMP_Text maxAmmo;

        private WeaponModel current;
        private Dictionary<WeaponModel, IReadOnlyView<WeaponModel>> assignedViews = new Dictionary<WeaponModel, IReadOnlyView<WeaponModel>>();

        public override void InstallBindings()
        {
            Container.Bind<IReadOnlyView<WeaponModel>>().FromInstance(this).AsSingle();
        }

        public void Initialize(WeaponModel model)
        {
            if (model == null) return;
            if (!assignedViews.TryGetValue(model, out IReadOnlyView<WeaponModel> view))
            {
                var instance = Instantiate(weaponDisplayPrefab, root);
                view = instance.GetComponent<IReadOnlyView<WeaponModel>>();
                view?.Initialize(model);
                instance.transform.SetSiblingIndex((int)model.GetItemSO().LoadoutCategory);
                assignedViews.Add(model, view);
                DebugUtility.Log(this, "Initialized view!");
            }
            else
            {
                view?.Initialize(model);
            }
        }

        public void Show()
        {
            if (current == null) return;
            if (assignedViews.TryGetValue(current, out IReadOnlyView<WeaponModel> view))
            {
                view?.Show();
            }
            tweener.OnPointerClick(null);
        }

        public void Hide()
        {
            if (current == null) return;
            if (assignedViews.TryGetValue(current, out IReadOnlyView<WeaponModel> view))
            {
                view?.Hide();
            }
        }

        public void UpdateView(WeaponModel model)
        {
            if (model == null) return;
            current = model;
            string currentAmmoValue = current.MaxBullets <= 0 ? "-" : $"{current.CurrentBullets}";
            currentAmmo.text = currentAmmoValue;
            string maxAmmoValue = current.MaxBullets <= 0 ? "" : $"{current.BulletsTotal}";
            maxAmmo.text = maxAmmoValue;
            if(assignedViews.TryGetValue(model, out IReadOnlyView<WeaponModel> view))
            {
                view?.UpdateView(model);
            }
            else
            {
                Initialize(model);
            }

        }

        public void Destroy(WeaponModel weaponModel)
        {
            if (assignedViews.TryGetValue(current, out IReadOnlyView<WeaponModel> view))
            {
                view?.Destroy(weaponModel);
            }
        }
    }
}
