using MyToolz.DesignPatterns.EventBus;
using MyToolz.IO;
using MyToolz.Player.FPS.Inventory;
using MyToolz.Player.FPS.LoadoutSystem.Events;
using MyToolz.Player.FPS.LoadoutSystem.Model;
using MyToolz.Player.FPS.LoadoutSystem.View;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace MyToolz.Player.FPS.LoadoutSystem.Presenter
{
    public class MenuLoadoutController : MonoBehaviour
    {
        [SerializeField] private SlotsAdapter[] adapters;
        private HashSet<ILoadoutSlot> loadoutSlots = new HashSet<ILoadoutSlot>();
        private Loadout cachedData;

        [Inject]
        private void Construct(ISaver<Loadout> saver)
        {
            cachedData = saver.Load();
        }

        private EventBinding<LoadoutItemChosen> loadoutItemChosenEventBinding;

        private void OnEnable()
        {
            loadoutItemChosenEventBinding = new EventBinding<LoadoutItemChosen>(RefreshCache);
            EventBus<LoadoutItemChosen>.Register(loadoutItemChosenEventBinding);
        }

        private void OnDisable()
        {
            EventBus<LoadoutItemChosen>.Deregister(loadoutItemChosenEventBinding);
        }

        private void Start()
        {
            var loadedWeapons = Resources.LoadAll<ItemSO>("");
            FindAndRegisterSavableComponents();
            var weaponsSaved = cachedData?.ToWeaponSOs<ItemSO>();
            foreach (var slot in loadoutSlots)
            {
                ItemSO selectedWeapon = null;
                weaponsSaved?.TryGetValue(slot.GetCategory(), out selectedWeapon);
                slot.LoadSlot(loadedWeapons, selectedWeapon);
            }

            if (weaponsSaved == null || weaponsSaved.Count == 0) cachedData.weapons = loadoutSlots.Select(obj=>obj.GetWeaponGUID()).ToList();
        }

        private void RefreshCache()
        {
            cachedData.weapons = adapters.Select(a => a.LoadoutSlot.GetWeaponGUID()).ToList();
            WeaponLoadoutModel.OnLocalWeaponLoadoutUpdated?.Invoke();

        }

        private void FindAndRegisterSavableComponents()
        {
            foreach (var adapter in adapters)
            {
                ILoadoutSlot slot = adapter.LoadoutSlot;
                if (slot == null) continue;
                loadoutSlots.Add(slot);
            }
        }
    }
}
