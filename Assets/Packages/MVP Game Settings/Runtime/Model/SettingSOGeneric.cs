using MyToolz.GameSettings.Data;
using MyToolz.Utilities.Debug;
using System;
using UnityEngine;

namespace MyToolz.ScriptableObjects.GameSettings
{
    public abstract class SettingSOGeneric<T> : SettingSOAbstract
    {
        [SerializeField] protected T defaultValue = default;
        protected T currentValue;
        private bool hasBeenSet;

        public T DefaultValue => defaultValue;
        public T CurrentValue => hasBeenSet ? currentValue : defaultValue;
        public override bool HasValue => hasBeenSet;

#if UNITY_EDITOR
        protected override void OnEnable()
        {
            base.OnEnable();
            UnityEditor.EditorApplication.playModeStateChanged -= ResetRuntimeStateOnExit;
            UnityEditor.EditorApplication.playModeStateChanged += ResetRuntimeStateOnExit;
        }

        private void ResetRuntimeStateOnExit(UnityEditor.PlayModeStateChange change)
        {
            if (change != UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            currentValue = defaultValue;
            hasBeenSet = false;
            ForgetLoad();
        }
#endif

        public virtual void SetCurrentValue(T newValue)
        {
            if (!IsValueValid(newValue))
            {
                DebugUtility.LogError(this, $"Invalid value on {settingName}, it will not be accepted!");
                return;
            }
            Accept(newValue);
            OnSetted();

            foreach (SettingSOAbstract twin in Twins())
            {
                SettingSOGeneric<T> mirror = (SettingSOGeneric<T>)twin;
                mirror.Accept(newValue);
                mirror.OnSetted();
            }
        }

        private void Accept(T value)
        {
            currentValue = value;
            hasBeenSet = true;
            NotifyValueUpdated();
        }

        protected override void AdoptValueFrom(SettingSOAbstract source)
        {
            SettingSOGeneric<T> typed = (SettingSOGeneric<T>)source;
            if (!typed.hasBeenSet)
            {
                return;
            }

            currentValue = typed.currentValue;
            hasBeenSet = true;
        }

        protected virtual bool IsValueValid(T value)
        {
            return value != null;
        }

        protected override bool IsCurrentValueValid()
        {
            return IsValueValid(currentValue);
        }

        protected virtual void OnSetted()
        {

        }

        protected virtual void OnLoaded()
        {

        }

        public override void Load(SettingEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
            {
                DebugUtility.LogError(this, $"Loaded entry on {settingName} is null or has empty ID!");
                return;
            }
            if (!string.Equals(id, entry.Id))
            {
                DebugUtility.LogError(this, $"ID mismatch on {settingName}, loaded {entry.Id} doesn't match {id}");
                return;
            }
            bool accepted = false;
            try
            {
                T loadedValue = entry.GetValue<T>();
                if (IsValueValid(loadedValue))
                {
                    currentValue = loadedValue;
                    hasBeenSet = true;
                    accepted = true;
                }
                else
                {
                    DebugUtility.LogWarning(this, $"Loaded value on {settingName} is invalid, keeping default.");
                }
            }
            catch (Exception e)
            {
                DebugUtility.LogError(this, $"Deserialization error for {settingName} with ID {entry.Id}: {e}");
            }
            NotifyValueUpdated();
            OnLoaded();

            if (!accepted)
            {
                return;
            }

            foreach (SettingSOAbstract twin in Twins())
            {
                SettingSOGeneric<T> mirror = (SettingSOGeneric<T>)twin;
                mirror.currentValue = currentValue;
                mirror.hasBeenSet = true;
                mirror.NotifyValueUpdated();
                mirror.OnLoaded();
            }
        }

        public override SettingEntry Save()
        {
            if (!hasBeenSet)
            {
                return null;
            }
            if (!IsCurrentValueValid())
            {
                DebugUtility.LogWarning(this, $"Invalid value on {settingName}, it will not be saved");
                return null;
            }
            return new SettingEntry(id, currentValue);
        }
    }
}
