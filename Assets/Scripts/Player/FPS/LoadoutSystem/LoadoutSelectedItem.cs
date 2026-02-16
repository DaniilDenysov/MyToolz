using MyToolz.Player.FPS.Inventory;
using MyToolz.UI.Labels;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyToolz.Player.FPS.LoadoutSystem.View
{
    public abstract class LoadoutSelectedItem<T> : Label, IPointerEnterHandler where T : ItemSO
    {
        public static Action<T> OnPointerEnter;
        [SerializeField] protected Image iconDisplay;
        [SerializeField] protected TMP_Text nameDisplay;
        [SerializeField] protected TMP_Text categoryDisplay;
        [SerializeField] protected T itemSO;

        public T SelectedItem
        {
            get => itemSO;
            set
            {
                if (value == null) return;
                itemSO = value;
                Construct(value);
            }
        }

        public virtual void Construct(T itemSO)
        {
            if (itemSO == null) return;
            this.itemSO = itemSO;
            iconDisplay.sprite = itemSO.ItemIcon;
            nameDisplay.text = itemSO.ItemName;
            categoryDisplay.text = itemSO.LoadoutCategory.ToString().ToUpper();
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            OnPointerEnter?.Invoke(itemSO);
        }
    }
}
