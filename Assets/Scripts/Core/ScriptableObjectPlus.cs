using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.Core
{
    public abstract class ScriptableObjectPlus : ScriptableObject
    {
        [SerializeField] private bool enableLogging = true;

        protected void Log(string message)
        {
            if (!enableLogging) return;
            DebugUtility.Log(this, message);
        }

        protected void LogError(string message)
        {
            if (!enableLogging) return;
            DebugUtility.LogError(this, message);
        }

        protected void LogWarning(string message)
        {
            if (!enableLogging) return;
            DebugUtility.LogWarning(this, message);
        }
    }
}
