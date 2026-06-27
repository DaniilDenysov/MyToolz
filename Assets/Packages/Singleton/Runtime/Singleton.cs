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

        private void OnDestroy()
        {
            RemoveSelf();
        }
    }
}
