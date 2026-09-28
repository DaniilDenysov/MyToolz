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
        // Loaded setting instances grouped by ID. The same asset can be loaded more than once (for
        // example once directly and once through an Addressables bundle); instances with the same ID
        // and type are "twins" and are kept in sync so every copy reports the same value.
        private static readonly Dictionary<string, List<SettingSOAbstract>> liveById = new();

        public event Action OnSettingUpdated;

        /// <summary>
        /// Raised once the SettingsPresenter has applied saved values (or confirmed there are none).
        /// Systems that read a setting at startup (e.g. localization) should wait for this.
        /// </summary>
        public event Action OnLoadCompleted;

        [SerializeField] protected string settingName;
        [SerializeField] protected string settingDescription;
        [SerializeField, ReadOnly] protected string id;

        private bool loadFinished;
        private string registeredId;

        public string SettingName => settingName;
        public string SettingDescription => settingDescription;
        public string ID => id;
        public bool IsLoadFinished => loadFinished;

        /// <summary>True when a value was set or loaded (as opposed to reporting the default).</summary>
        public abstract bool HasValue { get; }

        protected virtual void OnEnable()
        {
            CheckID();
            Register();
        }

        protected virtual void OnDisable()
        {
            Unregister();
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
            Unregister();
            id = Guid.NewGuid().ToString();
            Register();

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        private void Register()
        {
            if (registeredId != null || string.IsNullOrEmpty(id))
            {
                return;
            }

            if (!liveById.TryGetValue(id, out List<SettingSOAbstract> group))
            {
                group = new List<SettingSOAbstract>(1);
                liveById[id] = group;
            }

            SettingSOAbstract source = null;
            foreach (SettingSOAbstract other in group)
            {
                if (other == null)
                {
                    continue;
                }

                if (other.GetType() != GetType())
                {
                    DebugUtility.LogError(this,
                        $"Setting ID '{id}' is used by '{name}' ({GetType().Name}) and '{other.name}' ({other.GetType().Name}). " +
                        "IDs must be unique per setting — use 'Generate ID' on one of them.");
                    continue;
                }

                if (other.name != name)
                {
                    DebugUtility.LogWarning(this,
                        $"Setting assets '{name}' and '{other.name}' share ID '{id}' and are treated as the same setting. " +
                        "If one was duplicated from the other, use 'Generate ID' on the copy.");
                }

                if (source == null && (other.HasValue || other.loadFinished))
                {
                    source = other;
                }
            }

            group.Add(this);
            registeredId = id;

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

        private void Unregister()
        {
            if (registeredId == null)
            {
                return;
            }

            if (liveById.TryGetValue(registeredId, out List<SettingSOAbstract> group))
            {
                group.Remove(this);
                if (group.Count == 0)
                {
                    liveById.Remove(registeredId);
                }
            }

            registeredId = null;
        }

        /// <summary>
        /// Live instances with this setting's ID and type, excluding this one. Iterate it with
        /// <c>foreach</c> and skip entries for which <see cref="IsTwin"/> is false; the list is shared
        /// and must not be modified.
        /// </summary>
        protected IReadOnlyList<SettingSOAbstract> TwinCandidates =>
            registeredId != null && liveById.TryGetValue(registeredId, out List<SettingSOAbstract> group)
                ? group
                : (IReadOnlyList<SettingSOAbstract>)Array.Empty<SettingSOAbstract>();

        protected bool IsTwin(SettingSOAbstract other) =>
            other != null && other != this && other.GetType() == GetType();

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

        /// <summary>Marks this setting (and its twins) as loaded and raises <see cref="OnLoadCompleted"/>.</summary>
        public void CompleteLoad()
        {
            FinishLoad();

            IReadOnlyList<SettingSOAbstract> candidates = TwinCandidates;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (IsTwin(candidates[i]))
                {
                    candidates[i].FinishLoad();
                }
            }
        }

        private void FinishLoad()
        {
            loadFinished = true;
            OnLoadFinished();
            OnLoadCompleted?.Invoke();
        }

        // A twin that appears after loading completed adopts the value immediately but reports
        // completion on the next frame, so listeners subscribing in the same frame still hear it.
        private async void FinishLoadNextFrame()
        {
            await Awaitable.NextFrameAsync();

            if (this != null && !loadFinished && registeredId != null)
            {
                FinishLoad();
            }
        }

        protected virtual void OnLoadFinished()
        {
        }

        protected abstract bool IsCurrentValueValid();

        public abstract void Load(SettingEntry entry);
        public abstract SettingEntry Save();
    }
}
