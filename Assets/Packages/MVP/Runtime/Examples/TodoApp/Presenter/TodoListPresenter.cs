using System.Collections.Generic;
using MyToolz.DesignPatterns.MVP.Presenter;

namespace MyToolz.DesignPatterns.MVP.Examples.TodoApp
{
    public class TodoListPresenter : PresenterBase<List<TodoItemModel>, ITodoListView>
    {
        public TodoListPresenter(List<TodoItemModel> model, ITodoListView view)
            : base(model, view) { }

        protected override void OnInitialize()
        {
            View.Initialize(Model);
            View.PopulateList(Model);
        }

        protected override void SubscribeEvents()
        {
            View.OnAddRequested += HandleAddRequested;
            View.OnItemRemoved += HandleItemRemoved;
            View.OnToggleCompleted += HandleToggleCompleted;
        }

        protected override void UnsubscribeEvents()
        {
            View.OnAddRequested -= HandleAddRequested;
            View.OnItemRemoved -= HandleItemRemoved;
            View.OnToggleCompleted -= HandleToggleCompleted;
        }

        public void AddItem(TodoItemModel item)
        {
            if (!item.IsValid())
            {
                View.ShowError(string.Join("\n", item.GetValidationErrors()));
                return;
            }

            View.ClearError();
            Model.Add(item);
            View.AddItem(item);
            item.OnChanged += HandleItemChanged;
        }

        private void HandleAddRequested()
        {
            var newItem = new TodoItemModel();
            AddItem(newItem);
        }

        private void HandleItemRemoved(int index)
        {
            if (index < 0 || index >= Model.Count)
                return;

            Model[index].OnChanged -= HandleItemChanged;
            Model.RemoveAt(index);
            View.RemoveItemAt(index);
        }

        private void HandleToggleCompleted(int index)
        {
            if (index < 0 || index >= Model.Count)
                return;

            var item = Model[index];
            item.SetCompleted(!item.IsCompleted);
        }

        private void HandleItemChanged(TodoItemModel item)
        {
            View.UpdateView(Model);
        }

        protected override void OnDispose()
        {
            foreach (var item in Model)
                item.OnChanged -= HandleItemChanged;
        }
    }
}
