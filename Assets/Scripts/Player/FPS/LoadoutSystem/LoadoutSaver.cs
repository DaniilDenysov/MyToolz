using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MyToolz.Player.FPS.LoadoutSystem.View;
using MyToolz.IO;
using MyToolz.Player.FPS.LoadoutSystem.Model;
using MyToolz.DesignPatterns.Adapter;
using MyToolz.InventorySystem.Models;

namespace MyToolz.Player.FPS.LoadoutSystem.View
{
        [System.Serializable]
    public class SlotsAdapter : IAdapter<GameObject, ILoadoutSlot>
    {
        [SerializeField] protected GameObject slot;

        public ILoadoutSlot LoadoutSlot
        {
            get => Convert(slot);
        }

        public ILoadoutSlot Convert(GameObject reference)
        {
            if (reference.TryGetComponent(out ILoadoutSlot loadoutSlot))
            {
                return loadoutSlot;
            }
            return null;
        }
    }
}

namespace MyToolz.Player.FPS.LoadoutSystem.Model
{
    public class Loadout
    {
        public List<string> weapons;
        private ItemSO[] allWeapons;

        public Loadout()
        {
            allWeapons = Resources.LoadAll<ItemSO>("");
        }

        public Dictionary<LoadoutSlotCategory, T> ToWeaponSOs<T>() where T : ItemSO
        {
            Dictionary<LoadoutSlotCategory, T> convertedLoadout = new Dictionary<LoadoutSlotCategory, T>();
            if (weapons != null && weapons.Count > 0)
            {
                foreach (var weaponGuid in weapons)
                {
                    var weaponSO = allWeapons.FirstOrDefault((so) => so.ItemGuid.Equals(weaponGuid) && so is T);
                    //if (weaponSO) convertedLoadout.TryAdd(weaponSO.LoadoutCategory, weaponSO as T);
                }
            }
            return convertedLoadout;
        }

    }
}

namespace MyToolz.Player.FPS.LoadoutSystem.Presenter
{
    public class LoadoutSaver : SaveLoadBase<Loadout>
    {

    }
}