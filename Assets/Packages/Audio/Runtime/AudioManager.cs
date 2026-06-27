using MyToolz.Audio.Events;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.Singleton;
using MyToolz.EditorToolz;
using MyToolz.Events;
using MyToolz.Utilities.Debug;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.Audio
{
    public class AudioManager : PrivateSingleton<AudioManager>, IEventListener
    {
        [SerializeField, Required] private AudioSourceWrapper defaultPrefab;

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

            EventBus<PoolRequest<AudioSourceWrapper>>.Raise(new PoolRequest<AudioSourceWrapper>
            {
                Prefab = defaultPrefab,
                Position = e.Position,
                Rotation = Quaternion.identity,
                Parent = null,
                Callback = wrapper => wrapper.Play(audioClipSO)
            });
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
