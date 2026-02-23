using System;
using UnityEngine;

namespace MyToolz.DesignPatterns.MVP.View
{
    public abstract class InteractableViewBase<T> : MonoBehaviour, IInteractableView<T>
    {
        public event Action<T> OnUserInput;
        public event Action OnSubmit;
        public event Action OnCancel;

        public bool IsVisible => gameObject.activeSelf;

        public virtual void Initialize(T model)
        {
            BindUIEvents();
            UpdateView(model);
        }

        public abstract void UpdateView(T model);

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        public virtual void Destroy(T model)
        {
            UnbindUIEvents();
            Destroy(gameObject);
        }

        public abstract void SetInteractable(bool interactable);

        protected abstract void BindUIEvents();
        protected abstract void UnbindUIEvents();

        protected void RaiseUserInput(T data)
        {
            OnUserInput?.Invoke(data);
        }

        protected void RaiseSubmit()
        {
            OnSubmit?.Invoke();
        }

        protected void RaiseCancel()
        {
            OnCancel?.Invoke();
        }
    }
}
