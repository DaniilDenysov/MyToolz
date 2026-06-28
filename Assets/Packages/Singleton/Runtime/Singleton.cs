using UnityEngine;

namespace MyToolz.DesignPatterns.Singleton
{
    public abstract class Singleton : MonoBehaviour
    {
        [Header("Singleton")]
        [SerializeField] protected bool dontDestroyOnLoad = false;
        [SerializeField] protected bool destroyGameObjectOnDuplicate;

        private void Awake()
        {
            if (IsValid())
            {
                SetSelf();
                if (dontDestroyOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
                OnSingletonAwake();
            }
            else
            {
                if (!destroyGameObjectOnDuplicate)
                {
                    Destroy(this);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        /// Validation for singleton initialization
        /// </summary>
        /// <returns>true if the instance is elegible for the singleton and false otherwise</returns>
        protected abstract bool IsValid();
        protected abstract void SetSelf();
        protected abstract void RemoveSelf();

        /// <summary>
        /// Runs once, only on the surviving singleton instance, right after it has
        /// registered itself (and optionally been marked <see cref="DontDestroyOnLoad"/>).
        /// Override this instead of declaring your own <c>Awake</c>: a subclass <c>Awake</c>
        /// shadows this base one (Unity only dispatches the most-derived magic method),
        /// which silently disables the whole singleton guard.
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        /// <summary>
        /// Runs when the component is destroyed, after <see cref="RemoveSelf"/>. Override
        /// this instead of declaring your own <c>OnDestroy</c> for the same shadowing reason.
        /// </summary>
        protected virtual void OnSingletonDestroy() { }

        private void OnDestroy()
        {
            RemoveSelf();
            OnSingletonDestroy();
        }
    }
}
