using System;

namespace MyToolz.DesignPatterns.MVP.View
{
    public interface IInteractableView<T> : IReadOnlyView<T>
    {
        event Action<T> OnUserInput;
        event Action OnSubmit;
        event Action OnCancel;
        void SetInteractable(bool interactable);
    }
}
