using MyToolz.Utilities.Debug;
using System;
using UnityEngine;

namespace MyToolz.ScriptableObjects.GameSettings
{
    public abstract class SettingSOGeneric<T> : SettingSOAbstract
    {
        [SerializeField] protected T defaultValue = default;
        protected T currentValue;
        public T DefaultValue => defaultValue;
        public T CurrentValue => currentValue ?? defaultValue;

        public virtual void SetCurrentValue(T newValue)
        {
            if (!IsValueValid(newValue))
            {
                DebugUtility.LogError(this, $"Invalid value on {settingName}, it will not be accepted!");
                return;
            }
            currentValue = newValue;
            NotifyValueUpdated();
        }

        protected virtual bool IsValueValid(T value)
        {
            return value != null;
        }

        protected override bool IsCurrentValueValid()
        {
            return IsValueValid(currentValue);
        }

        public override void Load((string id, object value) data)
        {
            if (string.IsNullOrEmpty(data.id) || string.IsNullOrWhiteSpace(data.id))
            {
                DebugUtility.LogError(this, $"Loaded ID on {settingName} is null or empty!");
                return;
            }
            if (!string.Equals(id,data.id))
            {
                DebugUtility.LogError(this,     $"ID missmatch on {settingName}, loaded {data.id} doesn't match {id}");
                return;
            }
            try
            {
                currentValue = (T)data.value;
            }
            catch (Exception e)
            {
                DebugUtility.LogError(this, $"Unexpected cast error for {settingName} with ID {data.id} with error: {e}");
            }
            NotifyValueUpdated();
        }
        public override (string id, object value) Save()
        {
            if (!IsCurrentValueValid())
            {
                DebugUtility.LogError(this, $"Invalid value on {settingName}, it will fallback to default value");
            }
            return new (id, currentValue);
        }
    }
}
