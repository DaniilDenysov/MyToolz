using MyToolz.DesignPatterns.MVP.View;
using MyToolz.Tweener.UI;
using UnityEngine;


namespace MyToolz.Player.FPS.CombatSystem.View
{
    public abstract class ItemDisplay<T> : MonoBehaviour, IReadOnlyView<T>
    {
        [SerializeField] protected UITweener tweener;
        protected T model;

        public virtual void Initialize(T model)
        {
            if (model == null) return;
            gameObject.SetActive(true);
            this.model = model;
        }

        public virtual void Show()
        {
            tweener.OnPointerEnter(null);
        }

        public abstract void UpdateView(T model);

        public virtual void Hide()
        {
            tweener.OnPointerExit(null);
        }

        public void Destroy(T model)
        {
            gameObject.SetActive(false);
        }
    }
}
