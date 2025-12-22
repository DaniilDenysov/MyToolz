using MyToolz.Inventory.Models;
using MyToolz.Inventory.Views;
using UnityEngine;
using Zenject;

namespace MyToolz.Inventory.Presenters
{
    public interface IInventoryPresenter<T> where T : ScriptableObject
    {
        public void Add(T inventoryItemSO, uint amount = 1);
        public void Remove(T inventoryItemSO, uint amount = 1);
    }

    public abstract class InventoryPresenter<T> : MonoBehaviour, IInventoryPresenter<T> where T : ScriptableObject
    {
        protected IInventoryView<T> view;
        protected IInventoryModel<T> model;

        [Inject]
        private void Construct(IInventoryView<T> view, IInventoryModel<T> model)
        {
            this.view = view;
            this.model = model;
        }

        private void Start()
        {
            model.Initialize();
            view.Initialize(model.InventoryItems);
        }

        protected virtual void OnEnable()
        {
            model.OnItemUpdated += OnItemUpdated;
            view.OnItemAmountChanged += Remove;
        }

        protected virtual void OnDisable()
        {
            model.OnItemUpdated -= OnItemUpdated;
            view.OnItemAmountChanged -= Remove;
        }

        protected void OnItemUpdated(T itemSO, uint amount = 1)
        {
            view.UpdateItem(itemSO, amount);
        }

        public void Add(T inventoryItemSO, uint amount = 1)
        {
            model.Add(inventoryItemSO,amount);
        }

        public void Remove(T inventoryItemSO, uint amount = 1)
        {
            model.Remove(inventoryItemSO, amount);
        }
    }
}
