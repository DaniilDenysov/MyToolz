#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using UnityEngine;
using MyToolz.Core;
using MyToolz.EditorToolz;

namespace MyToolz.ScriptableObjects.GameSettings
{
    public abstract class SettingSOAbstract : ScriptableObjectPlus
    {
        public event Action OnSettingUpdated;

        [SerializeField] protected string settingName;
        [SerializeField] protected string settingDescription;
        [SerializeField, ReadOnly] protected string id;

        public string SettingName => settingName;
        public string SettingDescription => settingDescription;
        public string ID => id;

        protected virtual void OnEnable()
        {
            CheckID();
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            CheckID();
        }
#endif

        private void CheckID()
        {
            if (!string.IsNullOrEmpty(id))
                return;

            id = Guid.NewGuid().ToString();
            Log($"Auto-generated GUID: {id}");

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        [Button("Generate ID")]
        public void GenerateNewID()
        {
            id = Guid.NewGuid().ToString();

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        protected void NotifyValueUpdated()
        {
            OnSettingUpdated?.Invoke();
        }

        protected abstract bool IsCurrentValueValid();

        public abstract void Load((string id, object value) data);
        public abstract (string id, object value) Save();
    }
}
