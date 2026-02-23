using UnityEngine;

namespace MyToolz.DesignPatterns.MVP.View
{
    public abstract class ViewBase<T> : MonoBehaviour, IReadOnlyView<T>
    {
        public bool IsVisible => gameObject.activeSelf;

        public virtual void Initialize(T model)
        {
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
            Destroy(gameObject);
        }
    }
}
