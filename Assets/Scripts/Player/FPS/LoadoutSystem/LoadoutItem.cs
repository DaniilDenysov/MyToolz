using MyToolz.Player.FPS.Inventory;
using MyToolz.UI.Labels;
using Sirenix.OdinInspector;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace MyToolz.Player.FPS.LoadoutSystem.View
{
    [RequireComponent(typeof(Button))]
    public abstract class LoadoutItem<T> : Label, IPointerEnterHandler,IPointerExitHandler  where T : ItemSO
    {
        public static Action<LoadoutItem<T>> OnHoveredOver;
        public static Action<LoadoutItem<T>> OnItemSelected;
        [SerializeField] protected TMP_Text weaponName;
        [SerializeField] protected Image weaponIcon;
        [SerializeField] protected Toggle weaponStatus;
        [SerializeField] protected Color selectedColor, deselectedColor;
        [FoldoutGroup("LockScreen"), SerializeField, Required] private GameObject lockScreen;
        [FoldoutGroup("LockScreen"), SerializeField, Required] private TMP_Text unlockRequirement;
        protected Button button;
        protected T item;
        protected Vector2 originalSize;
        protected bool isSelected;

        public T Item
        {
            get => item;
        }

        private void OnEnable()
        {
            SetSeleced(isSelected);
        }

        private void SetButton()
        {
            if (TryGetComponent(out button))
            {
                button.onClick.AddListener(OnSelected);
            }
        }

        public void Construct(ItemSO itemSO, bool isSelected, UnityAction onSelected)
        {
            weaponName.text = itemSO.name;
            weaponIcon.sprite = itemSO.ItemIcon;
            item = (T)itemSO;
            SetButton();
            if (weaponStatus != null)
            {
                weaponStatus.isOn = isSelected;
            }
            this.isSelected = isSelected;
            button.onClick.AddListener(onSelected);
            //uint requiredLevel = model?.Value.Level ?? 0;
            //lockScreen.SetActive(itemSO.RequiredLevel < requiredLevel);
            //unlockRequirement.SetText($"Reach {requiredLevel} to unlock it");
        }

        private void OnSelected()
        {
            SetSeleced(true);
        }

        public void SetSeleced(bool state)
        {
            isSelected = state;
            if (weaponStatus != null)
            {
                weaponStatus.isOn = state;
            }
            if (isSelected) OnItemSelected?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnHoveredOver?.Invoke(this);
            Select();
        }

        private void Select()
        {
            weaponName.color = selectedColor;
            weaponIcon.color = selectedColor;
        }

        private void Deselect()
        {
            weaponName.color = deselectedColor;
            weaponIcon.color = deselectedColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Deselect();
        }
    }
}
