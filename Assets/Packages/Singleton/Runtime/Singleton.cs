using UnityEngine;

namespace MyToolz.DesignPatterns.Singleton
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        [SerializeField] protected bool dontDestroyOnLoad = false;
        [SerializeField] protected bool destroyGameObjectOnDuplicate;

        public static T Instance { get; private set; }

        public virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = this as T;
                if (dontDestroyOnLoad)
                    DontDestroyOnLoad(gameObject);
            }
            else
            {
                if (!destroyGameObjectOnDuplicate) Destroy(this);
                else Destroy(gameObject);
            }
        }

        public virtual void OnDestroy()
        {
            if (Instance == this as T)
                Instance = null;
        }
    }
}
