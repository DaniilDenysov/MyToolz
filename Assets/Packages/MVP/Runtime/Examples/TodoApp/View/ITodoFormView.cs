using MyToolz.DesignPatterns.MVP.View;

namespace MyToolz.DesignPatterns.MVP.Examples.TodoApp
{
    public interface ITodoFormView : IInteractableView<TodoItemModel>
    {
        void ShowValidationErrors(System.Collections.Generic.IReadOnlyList<string> errors);
        void ClearValidationErrors();
    }
}
