using MyToolz.Input;
using MyToolz.Tweener.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MyToolz.UI
{
   [System.Serializable]
    public abstract class InputMode : IPlayerInputState
    {
        public abstract void OnEnter();
        public abstract void OnExit();
    }

    public abstract class UIScreenBase : MonoBehaviour, IUIState
    {

        [Header("Base UI Config")]
        [SerializeField] protected UIScreen parent;
        [SerializeField] protected UITweener screenTweener;
        [SerializeField] protected GameObject firstSelected;
        [Header("Callbacks")]
        [SerializeField] private UnityEvent onEnter;
        [SerializeField] private UnityEvent onExit;
        protected bool isActive;
        public bool IsActive => isActive;

        public virtual void OnEnter()
        {
            isActive = true;
            if (screenTweener != null)
                screenTweener.SetActive(true);
            if (firstSelected != null)
                EventSystem.current.firstSelectedGameObject = firstSelected;
            onEnter?.Invoke();
        }

        public virtual void OnExit()
        {
            isActive = false;
            if (screenTweener != null)
                screenTweener.SetActive(false);
            if (firstSelected != null)
                EventSystem.current.firstSelectedGameObject = null;
            onExit?.Invoke();
        }

        public override string ToString()
        {
            return $"[UIScreen] {gameObject.name}";
        }

        public abstract void Open();

        public abstract void Close();
    }
}
