#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using MyToolz.EditorToolz;
using MyToolz.GameSettings.Data;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.ScriptableObjects.GameSettings
{
    public abstract class SettingSOAbstract : ScriptableObject
    {
        private static readonly List<SettingSOAbstract> live = new();

        public event Action OnSettingUpdated;
        public event Action OnLoadCompleted;

        [SerializeField] protected string settingName;
        [SerializeField] protected string settingDescription;
        [SerializeField, ReadOnly] protected string id;

        private bool loadFinished;

        public string SettingName => settingName;
        public string SettingDescription => settingDescription;
        public string ID => id;
        public bool IsLoadFinished => loadFinished;

        protected virtual void OnEnable()
        {
            CheckID();
            Register();
        }

        protected virtual void OnDisable()
        {
            live.Remove(this);
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
            {
                return;
            }

            id = Guid.NewGuid().ToString();
            DebugUtility.Log(this, $"Auto-generated GUID: {id}");

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

        private void Register()
        {
            if (live.Contains(this))
            {
                return;
            }

            SettingSOAbstract source = null;
            foreach (SettingSOAbstract twin in Twins())
            {
                if (twin.HasValue || twin.loadFinished)
                {
                    source = twin;
                    break;
                }
            }

            live.Add(this);

            if (source == null)
            {
                return;
            }

            AdoptValueFrom(source);

            if (source.loadFinished)
            {
                FinishLoadNextFrame();
            }
        }

        protected List<SettingSOAbstract> Twins()
        {
            List<SettingSOAbstract> twins = new();
            if (string.IsNullOrEmpty(id))
            {
                return twins;
            }

            foreach (SettingSOAbstract other in live)
            {
                if (other != null && other != this && other.GetType() == GetType() && other.id == id)
                {
                    twins.Add(other);
                }
            }

            return twins;
        }

        protected virtual void AdoptValueFrom(SettingSOAbstract source)
        {
        }

        protected void NotifyValueUpdated()
        {
            OnSettingUpdated?.Invoke();
        }

        protected void ForgetLoad()
        {
            loadFinished = false;
        }

        public void CompleteLoad()
        {
            FinishLoad();

            foreach (SettingSOAbstract twin in Twins())
            {
                twin.FinishLoad();
            }
        }

        private void FinishLoad()
        {
            loadFinished = true;
            OnLoadFinished();
            OnLoadCompleted?.Invoke();
        }

        private void FinishLoadNextFrame() => _ = FinishLoadNextFrameAsync();

        // Not async void: a throwing listener of OnLoadCompleted is logged here instead of being
        // rethrown onto the synchronization context, where nothing observes it.
        private async Awaitable FinishLoadNextFrameAsync()
        {
            try
            {
                await Awaitable.NextFrameAsync();

                if (this != null && !loadFinished && live.Contains(this))
                {
                    FinishLoad();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                DebugUtility.LogError(this, $"Finishing the load of '{settingName}' failed: {e}");
            }
        }

        protected virtual void OnLoadFinished()
        {
        }

        public abstract bool HasValue { get; }

        protected abstract bool IsCurrentValueValid();

        public abstract void Load(SettingEntry entry);
        public abstract SettingEntry Save();
    }
}
