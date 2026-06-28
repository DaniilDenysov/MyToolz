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

namespace MyToolz.Audio
{
    public class MXManager : PrivateSingleton<MXManager>, IEventListener
    {
        [FoldoutGroup("Pool"), SerializeField, Required] private AudioSourceWrapper audioSourcePrefab;

        [FoldoutGroup("Playback"), SerializeField] private bool loopCurrentSong = true;
        [FoldoutGroup("Playback"), SerializeField] private bool playOnAwake = true;
        [FoldoutGroup("Playback"), SerializeField, Range(0f, 1f)] private float maxVolume = 1f;

        [FoldoutGroup("Blending"), SerializeField] private float defaultSongBlendDuration = 1f;
        [FoldoutGroup("Blending"), SerializeField] private float defaultIntensityBlendDuration = 1f;
        [FoldoutGroup("Blending"), SerializeField, Range(0f, 1f)] private float intensity;

        [FoldoutGroup("Songs"), SerializeField] private List<SongSO> songs = new();

        private EventBinding<PlaySong> playSongBinding;
        private EventBinding<StopSong> stopSongBinding;
        private EventBinding<SetIntensity> setIntensityBinding;
        private readonly List<LoopInstance> activeLoopInstances = new();
        private CancellationTokenSource lifetimeCts;
        private CancellationTokenSource intensityFadeCts;

        public float Intensity => intensity;

        public float ClipTimeRemaining
        {
            get
            {
                if (activeLoopInstances.Count == 0)
                {
                    return 0f;
                }

                LoopInstance last = activeLoopInstances[activeLoopInstances.Count - 1];
                return (loopCurrentSong ? last.End : last.Tail) - Time.time;
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
            if (evt.SongIndex < 0 || evt.SongIndex >= songs.Count)
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

            LoopInstance looper = new LoopInstance(this, songs[evt.SongIndex], evt.StartTime);
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

                elapsed += Time.deltaTime;
                intensity = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            intensity = to;
        }

        private async UniTaskVoid PlayOnAwakeAsync(CancellationToken token)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.25), cancellationToken: token);
            EventBus<PlaySong>.Raise(PlaySong.Default(0));
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
                    LoopInstance fresh = new LoopInstance(this, pendingLoops[i], 0f);
                    fresh.SetFadeIn(0f);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        private AudioSourceWrapper AcquireFromPool(Transform parent)
        {
            AudioSourceWrapper result = null;

            EventBus<PoolRequest<AudioSourceWrapper>>.Raise(new PoolRequest<AudioSourceWrapper>
            {
                Prefab = audioSourcePrefab,
                Callback = (wrapper) =>
                {
                    wrapper.transform.SetParent(parent);
                    result = wrapper;
                }
            });

            return result;
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
            private float fadeInStart;
            private float fadeInEnd;
            private float fadeOutStart;
            private float fadeOutEnd;

            public float Tail { get; private set; }
            public float End { get; private set; }

            public LoopInstance(MXManager manager, SongSO song, float startTime)
            {
                this.manager = manager;
                this.song = song;

                if (song.IntensityClips.Count == 0)
                {
                    DebugUtility.LogError(this,$"Attempted to play a song with zero clips: {song.name}");
                    return;
                }

                int clipCount = song.IntensityClips.Count;
                wrappers = new AudioSourceWrapper[clipCount];
                sources = new AudioSource[clipCount];

                for (int i = 0; i < clipCount; i++)
                {
                    AudioSourceWrapper wrapper = manager.AcquireFromPool(manager.transform);
                    AudioSource source = wrapper.GetComponent<AudioSource>();
                    source.Configure(song.AudioSourceConfigSO);
                    source.clip = song.IntensityClips[i];
                    source.volume = 0f;
                    source.Play();
                    source.time = startTime;

                    wrappers[i] = wrapper;
                    sources[i] = source;
                }

                fadeInStart = -1f;
                fadeInEnd = -1f;
                fadeOutStart = -1f;
                fadeOutEnd = -1f;

                float clipLength = song.IntensityClips[0].length;
                End = Time.time + (clipLength - startTime);

                float tailOffset = song.ReverbTail <= 0f ? 0.25f : song.ReverbTail;
                Tail = End - tailOffset;

                manager.activeLoopInstances.Add(this);
            }

            public void SetFadeIn(float duration)
            {
                if (duration <= 0f)
                {
                    return;
                }

                fadeInStart = Time.time;
                fadeInEnd = Time.time + duration;
            }

            public void SetFadeOut(float duration)
            {
                if (duration <= 0f)
                {
                    End = Time.time;
                    return;
                }

                fadeOutStart = Time.time;
                fadeOutEnd = Time.time + duration;
                End = Time.time + duration;
                Tail = -1f;
            }

            public bool Tick(List<SongSO> pendingLoops)
            {
                if (Time.time > End)
                {
                    return true;
                }

                float primaryVolume = manager.maxVolume;

                if (fadeInStart >= 0f && fadeInEnd >= 0f)
                {
                    if (Time.time > fadeInEnd)
                    {
                        fadeInStart = -1f;
                        fadeInEnd = -1f;
                    }
                    else
                    {
                        float t = (Time.time - fadeInStart) / (fadeInEnd - fadeInStart);
                        primaryVolume = Mathf.Lerp(0f, primaryVolume, t);
                    }
                }

                if (fadeOutStart >= 0f && fadeOutEnd >= 0f)
                {
                    if (Time.time > fadeOutEnd)
                    {
                        fadeOutStart = -1f;
                        fadeOutEnd = -1f;
                    }
                    else
                    {
                        float t = (Time.time - fadeOutStart) / (fadeOutEnd - fadeOutStart);
                        primaryVolume = Mathf.Lerp(primaryVolume, 0f, t);
                    }
                }

                if (Tail > 0f && Time.time > Tail)
                {
                    Tail = -1f;
                    if (manager.loopCurrentSong)
                    {
                        pendingLoops.Add(song);
                    }
                }

                ApplyIntensityVolumes(primaryVolume);
                return false;
            }

            public void Dispose()
            {
                for (int i = 0; i < wrappers.Length; i++)
                {
                    manager.ReleaseToPool(wrappers[i]);
                }
            }

            private void ApplyIntensityVolumes(float primaryVolume)
            {
                for (int i = 0; i < sources.Length; i++)
                {
                    sources[i].volume = 0f;
                }

                float currentIntensity = manager.intensity;

                if (currentIntensity <= 0f)
                {
                    sources[0].volume = primaryVolume;
                    return;
                }

                if (currentIntensity >= 1f)
                {
                    sources[sources.Length - 1].volume = primaryVolume;
                    return;
                }

                float scaled = currentIntensity * (sources.Length - 1);
                int lower = Mathf.FloorToInt(scaled);
                int upper = lower + 1;
                float blend = scaled - lower;

                sources[lower].volume = (1f - blend) * primaryVolume;
                sources[upper].volume = blend * primaryVolume;
            }
        }
    }
}
