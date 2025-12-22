using MyToolz.Input;
using UnityEngine;
using Zenject;

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
        [SerializeField] protected GameObject screen;

        protected bool isActive;
        public bool IsActive => isActive;

        public virtual void OnEnter()
        {
            isActive = true;
            if (screen != null)
                screen.SetActive(true);  
        }

        public virtual void OnExit()
        {
            isActive = false;
            if (screen != null)
                screen.SetActive(false);
        }

        public override string ToString()
        {
            return $"[UIScreen] {gameObject.name}";
        }

        public abstract void Open();

        public abstract void Close();
    }
}
