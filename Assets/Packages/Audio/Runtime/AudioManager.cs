using MyToolz.Audio.Events;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.Singleton;
using MyToolz.EditorToolz;
using MyToolz.Events;
using MyToolz.Utilities.Debug;
using System.Collections.Generic;
using UnityEngine;
#if MYTOOLZ_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

namespace MyToolz.Audio
{
    public class AudioManager : PrivateSingleton<AudioManager>, IEventListener
    {
        [SerializeField, Tooltip("Pooled prefab used for one-shot clips. Leave empty when using an Addressable reference.")]
        private AudioSourceWrapper defaultPrefab;
#if MYTOOLZ_ADDRESSABLES
        [SerializeField, Tooltip("Addressable one-shot prefab, served by an AddressableObjectPoolInstaller. Used when set.")]
        private AssetReferenceGameObject defaultReference;
#endif

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

            float minInterval = e.IntervalOverload > 0f ? e.IntervalOverload : audioClipSO.MinimalInterval;
            // Unscaled: sounds still play (and throttle) while the game is paused.
            float now = Time.unscaledTime;

            if (history.TryGetValue(audioClipSO, out float lastPlayed) && lastPlayed + minInterval > now)
            {
                return;
            }

            history[audioClipSO] = now;

            float pitch = e.Pitch;
            var request = new PoolRequest<AudioSourceWrapper>
            {
                Position = e.Position,
                Rotation = Quaternion.identity,
                Parent = null,
                Callback = wrapper => wrapper.Play(audioClipSO, pitch)
            };

#if MYTOOLZ_ADDRESSABLES
            if (defaultReference != null && defaultReference.RuntimeKeyIsValid())
            {
                request.Key = defaultReference.RuntimeKey;
            }
            else
#endif
            if (defaultPrefab != null)
            {
                request.Prefab = defaultPrefab;
            }
            else
            {
                DebugUtility.LogError(this, "No one-shot audio prefab (or Addressable reference) is assigned.");
                return;
            }

            EventBus<PoolRequest<AudioSourceWrapper>>.Raise(request);
        }

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
