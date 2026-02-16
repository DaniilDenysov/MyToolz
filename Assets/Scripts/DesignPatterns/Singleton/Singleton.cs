using MyToolz.Core;
using UnityEngine;

namespace MyToolz.DesignPatterns.Singleton
{
    public abstract class Singleton<T> : MonoBehaviourPlus
    {
        [SerializeField] protected bool dontDestroyOnLoad = false;
        [SerializeField] protected bool destroyWholeIfDuplicate;
        public static T Instance
        {
            get;
            private set;
        }

        public virtual void Awake ()
        {
            if (Instance == null)
            {
                Instance = GetInstance();
                if (dontDestroyOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else
            {
                if (!destroyWholeIfDuplicate) Destroy(this);
                else Destroy(gameObject);
            }
        }

        public virtual void OnDestroy()
        {
            if (Instance as Singleton<T> == this)
            {
                Instance = default;
            }
        }

        public abstract T GetInstance();
    }
}
