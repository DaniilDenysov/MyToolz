using MyToolz.EditorToolz;
using MyToolz.ScriptableObjects.GameSettings;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.GameSettings
{
    public abstract class SettingView<T> : MonoBehaviour where T : SettingSOAbstract
    {
        [SerializeField, Required] protected T setting;

        private void OnEnable()
        {
            Register();   
        }

        protected virtual void Start()
        {
            OnExternalValueUpdated();
        }

        private void OnDisable()
        {
            Deregister();
        }

        private void OnDestroy()
        {
            Deregister();
        }

        protected virtual void Register()
        {
            if (setting == null)
            {
                DebugUtility.LogError(this, $"SettingSO is missing, please reassign it!");
                return;
            }
            setting.OnSettingUpdated += OnExternalValueUpdated;
        }

        protected virtual void Deregister()
        {
            if (setting == null)
            {
                DebugUtility.LogError(this, $"SettingSO is missing, please reassign it!");
                return;
            }
            setting.OnSettingUpdated -= OnExternalValueUpdated;
        }

        private void OnExternalValueUpdated()
        {
            if (setting == null)
            {
                DebugUtility.LogError(this, $"SettingSO is missing, please reassign it!");
                return;
            }
            OnExternalValueUpdated(setting);
        }
        protected abstract void OnExternalValueUpdated(T setting);
    }
}