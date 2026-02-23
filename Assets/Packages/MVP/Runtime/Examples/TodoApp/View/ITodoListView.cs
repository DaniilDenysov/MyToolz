using System;
using System.Collections.Generic;
using MyToolz.DesignPatterns.MVP.View;

namespace MyToolz.DesignPatterns.MVP.Examples.TodoApp
{
    public interface ITodoListView : IReadOnlyView<IReadOnlyList<TodoItemModel>>, ICollectionView<TodoItemModel>
    {
        event Action OnAddRequested;
        event Action<int> OnToggleCompleted;
        void ShowError(string message);
        void ClearError();
    }
}
