using System;
using System.Reflection;
using UnityEngine;

namespace MyToolz.DesignPatterns.Singleton
{
    public abstract class Singleton : MonoBehaviour
    {
        [Header("Singleton")]
        [SerializeField] protected bool dontDestroyOnLoad = false;
        [SerializeField] protected bool destroyGameObjectOnDuplicate;

        // Protected (not private) so a subclass that declares its own Awake/OnDestroy gets a
        // "hides inherited member" compiler warning; see ValidateSubclasses for the editor check.
        protected void Awake()
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

        protected void OnDestroy()
        {
            RemoveSelf();
            OnSingletonDestroy();
        }

#if UNITY_EDITOR
        // Unity only dispatches the most-derived Awake/OnDestroy. A subclass declaring either one
        // silently skips the guard above, so report it as soon as scripts compile.
        [UnityEditor.InitializeOnLoadMethod]
        private static void ValidateSubclasses()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            foreach (Type type in UnityEditor.TypeCache.GetTypesDerivedFrom<Singleton>())
            {
                foreach (string message in new[] { "Awake", "OnDestroy" })
                {
                    if (type.GetMethod(message, flags, null, Type.EmptyTypes, null) != null)
                    {
                        string replacement = message == "Awake" ? nameof(OnSingletonAwake) : nameof(OnSingletonDestroy);
                        Debug.LogError($"{type.FullName} declares {message}(), which replaces Singleton.{message}() and breaks the singleton guard. Override {replacement}() instead.");
                    }
                }
            }
        }
#endif
    }
}
