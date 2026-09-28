using System.Threading;
using Cysharp.Threading.Tasks;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.ObjectPool;
using MyToolz.Events;
using MyToolz.Extensions;
using UnityEngine;

namespace MyToolz.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioSourceWrapper : MonoBehaviour, IPoolable
    {
        private AudioSource audioSource;
        private CancellationTokenSource cts;
        private SourceSettings authored;

        // Settings authored on the prefab's AudioSource. A pooled source is reused for many clips; it
        // is reset to these on despawn so a clip without a config never inherits the previous clip's.
        private struct SourceSettings
        {
            public UnityEngine.Audio.AudioMixerGroup Mixer;
            public float Volume;
            public float Pitch;
            public float SpatialBlend;
            public bool Loop;
            public bool BypassEffects;
            public bool BypassReverbZones;
            public bool PlayOnAwake;

            public static SourceSettings Capture(AudioSource source) => new SourceSettings
            {
                Mixer = source.outputAudioMixerGroup,
                Volume = source.volume,
                Pitch = source.pitch,
                SpatialBlend = source.spatialBlend,
                Loop = source.loop,
                BypassEffects = source.bypassEffects,
                BypassReverbZones = source.bypassReverbZones,
                PlayOnAwake = source.playOnAwake
            };

            public void Apply(AudioSource source)
            {
                source.outputAudioMixerGroup = Mixer;
                source.volume = Volume;
                source.pitch = Pitch;
                source.spatialBlend = SpatialBlend;
                source.loop = Loop;
                source.bypassEffects = BypassEffects;
                source.bypassReverbZones = BypassReverbZones;
                source.playOnAwake = PlayOnAwake;
            }
        }

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            authored = SourceSettings.Capture(audioSource);
        }

        public void Play(AudioClipSO audioClipSO) => Play(audioClipSO, 0f);

        /// <param name="pitch">Pitch override; zero or less keeps the clip config's pitch.</param>
        public void Play(AudioClipSO audioClipSO, float pitch)
        {
            if (audioClipSO == null) return;

            var audio = audioClipSO.GetClipAndConfig();
            if (audio.clip == null) return;

            if (audio.config != null)
                audioSource.Configure(audio.config);

            if (pitch > 0f)
                audioSource.pitch = pitch;

            audioSource.clip = audio.clip;
            audioSource.Play();

            if (!audioSource.loop)
                WaitAndRelease().Forget();
        }

        private async UniTaskVoid WaitAndRelease()
        {
            CancelPending();
            cts = new CancellationTokenSource();

            var cancelled = await UniTask.WaitWhile(
                () => audioSource.isPlaying,
                PlayerLoopTiming.Update,
                cts.Token
            ).SuppressCancellationThrow();

            if (cancelled) return;

            EventBus<ReleaseRequest<AudioSourceWrapper>>.Raise(new ReleaseRequest<AudioSourceWrapper>
            {
                PoolObject = this
            });
        }

        private void CancelPending()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        public void OnSpawned() { }

        public void OnDespawned()
        {
            CancelPending();
            audioSource.Stop();
            audioSource.clip = null;
            audioSource.time = 0f;
            authored.Apply(audioSource);
        }

        private void OnDestroy()
        {
            CancelPending();
        }
    }
}