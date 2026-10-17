using Cysharp.Threading.Tasks;
using MyToolz.Audio.Events;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.Singleton;
using MyToolz.EditorToolz;
using MyToolz.Events;
using MyToolz.Extensions;
using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
#if MYTOOLZ_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

namespace MyToolz.Audio
{
    public class MXManager : PrivateSingleton<MXManager>, IEventListener
    {
        [FoldoutGroup("Pool"), SerializeField, Tooltip("Pooled prefab used for music layers. Leave empty when using an Addressable reference.")]
        private AudioSourceWrapper audioSourcePrefab;
#if MYTOOLZ_ADDRESSABLES
        [FoldoutGroup("Pool"), SerializeField, Tooltip("Addressable music-layer prefab, served by an AddressableObjectPoolInstaller. Used when set.")]
        private AssetReferenceGameObject audioSourceReference;
#endif

        [FoldoutGroup("Playback"), SerializeField] private bool loopCurrentSong = true;
        [FoldoutGroup("Playback"), SerializeField] private bool playOnAwake = true;
        [FoldoutGroup("Playback"), SerializeField, Range(0f, 1f)] private float maxVolume = 1f;

        [FoldoutGroup("Blending"), SerializeField] private float defaultSongBlendDuration = 1f;
        [FoldoutGroup("Blending"), SerializeField] private float defaultIntensityBlendDuration = 1f;
        [FoldoutGroup("Blending"), SerializeField, Range(0f, 1f)] private float intensity;

        [FoldoutGroup("Songs"), SerializeField] private SongSO defaultSong;
        private SongSO currentSong;

        private EventBinding<PlaySong> playSongBinding;
        private EventBinding<StopSong> stopSongBinding;
        private EventBinding<SetIntensity> setIntensityBinding;
        private readonly List<LoopInstance> activeLoopInstances = new();
        private CancellationTokenSource lifetimeCts;
        private CancellationTokenSource intensityFadeCts;

        public float Intensity => intensity;

        // Music keeps playing when Time.timeScale is 0, so loop scheduling follows the audio clock
        // rather than scaled game time.
        private static double Now => AudioSettings.dspTime;

        public float ClipTimeRemaining
        {
            get
            {
                if (activeLoopInstances.Count == 0)
                {
                    return 0f;
                }

                LoopInstance last = activeLoopInstances[activeLoopInstances.Count - 1];
                return (float)((loopCurrentSong ? last.End : last.Tail) - Now);
            }
        }

        private void OnEnable()
        {
            lifetimeCts = new CancellationTokenSource();
            RegisterEvents();

            if (playOnAwake)
            {
                PlayOnAwakeAsync(lifetimeCts.Token).Forget();
            }

            RunUpdateLoop(lifetimeCts.Token).Forget();
        }

        private void OnDisable()
        {
            UnregisterEvents();
            CancelTokenSource(ref lifetimeCts);
            CancelTokenSource(ref intensityFadeCts);

            // The update loop that would retire these is cancelled: stop and return every layer now
            // so pooled sources do not keep playing unowned.
            for (int i = 0; i < activeLoopInstances.Count; i++)
            {
                activeLoopInstances[i].Dispose();
            }
            activeLoopInstances.Clear();
            currentSong = null;
        }

        public void RegisterEvents()
        {
            playSongBinding = new EventBinding<PlaySong>(OnPlaySong);
            EventBus<PlaySong>.Register(playSongBinding);

            stopSongBinding = new EventBinding<StopSong>(OnStopSong);
            EventBus<StopSong>.Register(stopSongBinding);

            setIntensityBinding = new EventBinding<SetIntensity>(OnSetIntensity);
            EventBus<SetIntensity>.Register(setIntensityBinding);
        }

        public void UnregisterEvents()
        {
            EventBus<PlaySong>.Deregister(playSongBinding);
            EventBus<StopSong>.Deregister(stopSongBinding);
            EventBus<SetIntensity>.Deregister(setIntensityBinding);
        }

        private void OnPlaySong(PlaySong evt)
        {
            if (evt.Song == null)
            {
                return;
            }

            if (evt.Song == currentSong && activeLoopInstances.Count > 0)
            {
                return;
            }

            float blendIn = evt.BlendInDuration >= 0f ? evt.BlendInDuration : defaultSongBlendDuration;
            float blendOut = evt.BlendOutDuration >= 0f ? evt.BlendOutDuration : defaultSongBlendDuration;

            if (activeLoopInstances.Count > 0)
            {
                LoopInstance mostRecent = activeLoopInstances[activeLoopInstances.Count - 1];
                mostRecent.SetFadeOut(blendOut);
            }

            if (evt.Intensity >= 0f)
            {
                intensity = Mathf.Clamp01(evt.Intensity);
            }

            currentSong = evt.Song;
            LoopInstance looper = new LoopInstance(this, evt.Song, evt.StartTime);
            looper.SetFadeIn(blendIn);
        }

        private void OnStopSong(StopSong evt)
        {
            if (activeLoopInstances.Count > 0)
            {
                LoopInstance mostRecent = activeLoopInstances[activeLoopInstances.Count - 1];
                mostRecent.SetFadeOut(evt.FadeOutDuration);
            }
        }

        private void OnSetIntensity(SetIntensity evt)
        {
            float target = Mathf.Clamp01(evt.Intensity);
            float duration = evt.BlendDuration >= 0f ? evt.BlendDuration : defaultIntensityBlendDuration;

            CancelTokenSource(ref intensityFadeCts);

            if (duration <= 0f)
            {
                intensity = target;
                return;
            }

            intensityFadeCts = new CancellationTokenSource();
            FadeIntensityAsync(intensity, target, duration, intensityFadeCts.Token).Forget();
        }

        private async UniTaskVoid FadeIntensityAsync(float from, float to, float duration, CancellationToken token)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                elapsed += Time.unscaledDeltaTime;
                intensity = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            intensity = to;
        }

        private async UniTaskVoid PlayOnAwakeAsync(CancellationToken token)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.25), DelayType.Realtime, cancellationToken: token);

            if (defaultSong != null)
            {
                EventBus<PlaySong>.Raise(PlaySong.Default(defaultSong));
            }
        }

        private async UniTaskVoid RunUpdateLoop(CancellationToken token)
        {
            List<LoopInstance> deadLoops = new();
            List<SongSO> pendingLoops = new();

            while (!token.IsCancellationRequested)
            {
                deadLoops.Clear();
                pendingLoops.Clear();

                for (int i = 0; i < activeLoopInstances.Count; i++)
                {
                    LoopInstance looper = activeLoopInstances[i];
                    if (looper.Tick(pendingLoops))
                    {
                        deadLoops.Add(looper);
                    }
                }

                for (int i = 0; i < deadLoops.Count; i++)
                {
                    deadLoops[i].Dispose();
                    activeLoopInstances.Remove(deadLoops[i]);
                }

                for (int i = 0; i < pendingLoops.Count; i++)
                {
                    new LoopInstance(this, pendingLoops[i], 0f);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        private sealed class Acquisition
        {
            public AudioSourceWrapper Result;
            public bool Abandoned;
        }

        private AudioSourceWrapper AcquireFromPool(Transform parent)
        {
            var acquisition = new Acquisition();
            var request = new PoolRequest<AudioSourceWrapper>
            {
                Parent = parent,
                Position = parent != null ? parent.position : Vector3.zero,
                Rotation = Quaternion.identity,
                Callback = (wrapper) =>
                {
                    if (acquisition.Abandoned)
                    {
                        // The pool answered after we gave up (it was still initializing): hand the
                        // source straight back instead of leaking it.
                        ReleaseToPool(wrapper);
                        return;
                    }

                    acquisition.Result = wrapper;
                }
            };

#if MYTOOLZ_ADDRESSABLES
            if (audioSourceReference != null && audioSourceReference.RuntimeKeyIsValid())
            {
                request.Key = audioSourceReference.RuntimeKey;
            }
            else
#endif
            {
                request.Prefab = audioSourcePrefab;
            }

            EventBus<PoolRequest<AudioSourceWrapper>>.Raise(request);

            acquisition.Abandoned = acquisition.Result == null;
            return acquisition.Result;
        }

        private void ReleaseToPool(AudioSourceWrapper wrapper)
        {
            EventBus<ReleaseRequest<AudioSourceWrapper>>.Raise(new ReleaseRequest<AudioSourceWrapper>
            {
                PoolObject = wrapper
            });
        }

        private static void CancelTokenSource(ref CancellationTokenSource cts)
        {
            if (cts == null)
            {
                return;
            }

            cts.Cancel();
            cts.Dispose();
            cts = null;
        }

        private class LoopInstance
        {
            private readonly MXManager manager;
            private readonly SongSO song;
            private readonly AudioSourceWrapper[] wrappers;
            private readonly AudioSource[] sources;
            private readonly float configuredVolume;
            private double fadeInStart;
            private double fadeInEnd;
            private double fadeOutStart;
            private double fadeOutEnd;

            public double Tail { get; private set; }
            public double End { get; private set; }
            public bool IsValid => sources != null;

            public LoopInstance(MXManager manager, SongSO song, float startTime)
            {
                this.manager = manager;
                this.song = song;
                configuredVolume = song.AudioSourceConfigSO == null
                    ? 1f
                    : song.AudioSourceConfigSO.RandomizeVolume
                        ? song.AudioSourceConfigSO.GetRandomVolume()
                        : song.AudioSourceConfigSO.Volume;

                if (song.IntensityClips.Count == 0 || song.IntensityClips[0] == null)
                {
                    DebugUtility.LogError(manager, $"Attempted to play a song with zero clips: {song.name}");
                    return;
                }

                int clipCount = song.IntensityClips.Count;
                var acquiredWrappers = new AudioSourceWrapper[clipCount];
                var acquiredSources = new AudioSource[clipCount];

                for (int i = 0; i < clipCount; i++)
                {
                    AudioSourceWrapper wrapper = manager.AcquireFromPool(manager.transform);
                    if (wrapper == null)
                    {
                        DebugUtility.LogWarning(manager, $"Audio pool not ready; aborting loop for song: {song.name}");
                        for (int j = 0; j < i; j++)
                        {
                            manager.ReleaseToPool(acquiredWrappers[j]);
                        }
                        return;
                    }

                    AudioSource source = wrapper.GetComponent<AudioSource>();
                    source.Configure(song.AudioSourceConfigSO);
                    // The manager owns looping (loops are overlapped by the reverb tail), not the source.
                    source.loop = false;
                    source.clip = song.IntensityClips[i];
                    source.volume = 0f;
                    source.Play();
                    source.time = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, source.clip != null ? source.clip.length - 0.01f : 0f));

                    acquiredWrappers[i] = wrapper;
                    acquiredSources[i] = source;
                }

                wrappers = acquiredWrappers;
                sources = acquiredSources;

                fadeInStart = -1d;
                fadeInEnd = -1d;
                fadeOutStart = -1d;
                fadeOutEnd = -1d;

                float pitch = Mathf.Abs(sources[0].pitch) > 0.01f ? Mathf.Abs(sources[0].pitch) : 1f;
                double clipLength = song.IntensityClips[0].length;
                End = Now + (clipLength - startTime) / pitch;

                double tailOffset = song.ReverbTail <= 0f ? 0.25d : song.ReverbTail;
                Tail = End - tailOffset;

                manager.activeLoopInstances.Add(this);
            }

            public void SetFadeIn(float duration)
            {
                if (duration <= 0f)
                {
                    return;
                }

                fadeInStart = Now;
                fadeInEnd = Now + duration;
            }

            public void SetFadeOut(float duration)
            {
                if (duration <= 0f)
                {
                    End = Now;
                    return;
                }

                fadeOutStart = Now;
                fadeOutEnd = Now + duration;
                End = Now + duration;
                Tail = -1d;
            }

            public bool Tick(List<SongSO> pendingLoops)
            {
                double now = Now;
                if (!IsValid || now > End)
                {
                    return true;
                }

                float primaryVolume = manager.maxVolume * configuredVolume;

                if (fadeInStart >= 0d && fadeInEnd >= 0d)
                {
                    if (now > fadeInEnd)
                    {
                        fadeInStart = -1d;
                        fadeInEnd = -1d;
                    }
                    else
                    {
                        float t = (float)((now - fadeInStart) / (fadeInEnd - fadeInStart));
                        primaryVolume = Mathf.Lerp(0f, primaryVolume, t);
                    }
                }

                if (fadeOutStart >= 0d && fadeOutEnd >= 0d)
                {
                    if (now > fadeOutEnd)
                    {
                        fadeOutStart = -1d;
                        fadeOutEnd = -1d;
                    }
                    else
                    {
                        float t = (float)((now - fadeOutStart) / (fadeOutEnd - fadeOutStart));
                        primaryVolume = Mathf.Lerp(primaryVolume, 0f, t);
                    }
                }

                if (Tail > 0d && now > Tail)
                {
                    Tail = -1d;
                    if (manager.loopCurrentSong && manager.currentSong == song)
                    {
                        pendingLoops.Add(song);
                    }
                }

                ApplyIntensityVolumes(primaryVolume);
                return false;
            }

            public void Dispose()
            {
                if (wrappers == null)
                {
                    return;
                }

                for (int i = 0; i < wrappers.Length; i++)
                {
                    if (wrappers[i] != null)
                    {
                        manager.ReleaseToPool(wrappers[i]);
                    }
                }
            }

            private void ApplyIntensityVolumes(float primaryVolume)
            {
                for (int i = 0; i < sources.Length; i++)
                {
                    if (sources[i] != null) sources[i].volume = 0f;
                }

                SetLayerVolumes(sources, manager.intensity, primaryVolume);
            }
        }

        /// <summary>
        /// Distributes <paramref name="volume"/> across intensity layers: intensity 0 plays the first
        /// layer, 1 the last, values in between cross-fade the two neighbouring layers. A single
        /// layer always plays at full volume.
        /// </summary>
        internal static void SetLayerVolumes(AudioSource[] layers, float intensity, float volume)
        {
            float[] weights = GetLayerWeights(layers.Length, intensity);
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] != null) layers[i].volume = weights[i] * volume;
            }
        }

        public static float[] GetLayerWeights(int layerCount, float intensity)
        {
            var weights = new float[Mathf.Max(0, layerCount)];
            if (layerCount <= 0)
            {
                return weights;
            }

            if (layerCount == 1 || intensity <= 0f)
            {
                weights[0] = 1f;
                return weights;
            }

            if (intensity >= 1f)
            {
                weights[layerCount - 1] = 1f;
                return weights;
            }

            float scaled = intensity * (layerCount - 1);
            int lower = Mathf.Min(Mathf.FloorToInt(scaled), layerCount - 2);
            float blend = scaled - lower;
            weights[lower] = 1f - blend;
            weights[lower + 1] = blend;
            return weights;
        }

    }
}
