using MyToolz.Audio.Events;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.Singleton;
using MyToolz.EditorToolz;
using MyToolz.Events;
using MyToolz.Utilities.Debug;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MyToolz.Audio
{
    public class AudioManager : PrivateSingleton<AudioManager>, IEventListener
    {
        [Tooltip("Addressable AudioSourceWrapper prefab pooled by an AddressableObjectPoolInstaller (e.g. AudioSourceObjectPool).")]
        [SerializeField] private AssetReferenceGameObject defaultReference;

        [Tooltip("Directly referenced prefab, used when no Addressable reference is set.")]
        [SerializeField] private AudioSourceWrapper defaultPrefab;

        private readonly Dictionary<AudioClipSO, float> history = new();
        private EventBinding<PlayAudioClipSO> playBinding;

        public void RegisterEvents()
        {
            playBinding = new EventBinding<PlayAudioClipSO>(OnPlayAudioClipSO);
            EventBus<PlayAudioClipSO>.Register(playBinding);
        }

        public void UnregisterEvents()
        {
            EventBus<PlayAudioClipSO>.Deregister(playBinding);
        }

        private void OnPlayAudioClipSO(PlayAudioClipSO e)
        {
            AudioClipSO audioClipSO = e.AudioClipSO;

            if (audioClipSO == null)
            {
                DebugUtility.LogError(this, "Audio Clip SO is null!");
                return;
            }

            float minInterval = e.IntervalOverload < 0 ? audioClipSO.MinimalInterval : e.IntervalOverload;
            float lastPlayed = float.MinValue;

            if (history.TryGetValue(audioClipSO, out lastPlayed))
            {
                if (lastPlayed + minInterval > Time.time)
                {
                    return;
                }
            }

            lastPlayed = Time.time;
            history[audioClipSO] = lastPlayed;

            if (!HasSource())
            {
                DebugUtility.LogError(this, "Neither an Addressable reference nor a prefab is assigned for audio sources.");
                return;
            }

            EventBus<PoolRequest<AudioSourceWrapper>>.Raise(new PoolRequest<AudioSourceWrapper>
            {
                Key = UsesAddressable() ? defaultReference.RuntimeKey : null,
                Prefab = defaultPrefab,
                Position = e.Position,
                Rotation = Quaternion.identity,
                Parent = null,
                Callback = wrapper => wrapper.Play(audioClipSO, e.Pitch)
            });
        }

        private bool UsesAddressable() => defaultReference != null && defaultReference.RuntimeKeyIsValid();

        private bool HasSource() => UsesAddressable() || defaultPrefab != null;

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }
    }
}
