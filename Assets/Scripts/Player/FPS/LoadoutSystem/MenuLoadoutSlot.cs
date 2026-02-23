using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.InventorySystem.Models;
using MyToolz.Player.FPS.LoadoutSystem.Events;
using MyToolz.UI.Labels;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.Player.FPS.LoadoutSystem.Events
{
    public struct LoadoutItemChosen : IEvent
    {
        public ItemSO itemSO;
    }
}

namespace MyToolz.Player.FPS.LoadoutSystem.View
{

    public enum LoadoutSlotCategory
    {
        None = -1,
        Primary = 0,
        Secondary = 1,
        Tertiary = 2,
        LethalEquipment = 3,
    }

    public interface ILoadoutSlot
    {
        public void LoadSlot(ItemSO[] allItems, ItemSO selectedItem);
        string GetWeaponGUID();
        LoadoutSlotCategory GetCategory();
    }
    public abstract class MenuLoadoutSlot<T> : MonoBehaviour, ILoadoutSlot where T : ItemSO
    {
        public LoadoutSlotCategory Category { get { return category; } private set { } }
        [SerializeField] protected LoadoutSlotCategory category;
        [SerializeField] protected Transform container;
        [SerializeField] protected LoadoutSelectedItem<T> selectedItemDisplay;
        [SerializeField] protected T currentlySelected;
        [SerializeField] protected LoadoutItem<T> prefab;
        protected Dictionary<T, LoadoutItem<T>> availableWeapons = new Dictionary<T, LoadoutItem<T>>();
        protected LoadoutItem<T> currentlySelectedItem;

        public void LoadSlot(ItemSO[] allItems, ItemSO selectedItem)
        {
            foreach (var weapon in allItems)
            {
                //if (weapon.LoadoutCategory == category)
                {
                    if (weapon is not T) continue;
                    AddItemSO((T)weapon, weapon == selectedItem);
                    if (selectedItem)
                    {
                        if (weapon == selectedItem) SelectItem((T)weapon);
                    }
                    else
                    {
                        if (!currentlySelected)
                        {
                            SelectItem((T)weapon);
                        }
                    }
                }
            }
        }

        public string GetWeaponGUID()
        {
            return currentlySelected?.ItemGuid;
        }

        public void AddItemSO(T itemSO, bool isSelected)
        {
            //if (itemSO.LoadoutCategory != category) return;
            if (!availableWeapons.ContainsKey(itemSO))
            {
                EventBus<PoolRequest<Label>>.Raise(new PoolRequest<Label>()
                {
                    Prefab = prefab,
                    Callback = (weaponLabel) =>
                    {
                        var weapon = (LoadoutItem<T>)weaponLabel;
                        weapon.Construct(itemSO, isSelected, () =>
                        {
                            if (currentlySelectedItem == weapon) return;
                            if (currentlySelectedItem != null)
                            {
                                currentlySelectedItem.SetSeleced(false);
                            }
                            SelectItem(itemSO);
                            currentlySelectedItem = weapon;
                        });
                        if (isSelected)
                        {
                            currentlySelectedItem = weapon;
                        }
                        availableWeapons.Add(itemSO, weapon);
                        weaponLabel.transform.localScale = Vector3.one;
                    },
                    Parent = container
                });
               // var weapon = Instantiate(prefab, container);
               
            }
        }

        public void SelectItem(T itemSO)
        {
            if (!availableWeapons.TryGetValue(itemSO, out LoadoutItem<T> button)) return;
            currentlySelected = itemSO;
            selectedItemDisplay.Construct(itemSO);
            EventBus<LoadoutItemChosen>.Raise(new LoadoutItemChosen()
            {
                itemSO = itemSO,
            });
        }

        public LoadoutSlotCategory GetCategory()
        {
            return Category;
        }
    }
}