using MyToolz.Audio;
using UnityEngine;
using DG.Tweening;

namespace MyToolz.Extensions
{
    public static class AudioSourceExtensions
    {
        public static void Configure(this AudioSource audioSource, AudioSourceConfigSO configSO)
        {
            if (audioSource == null || configSO == null) return;

            audioSource.outputAudioMixerGroup = configSO.AudioMixer;
            audioSource.volume = configSO.RandomizeVolume ? configSO.GetRandomVolume() : configSO.Volume;
            audioSource.pitch = configSO.RandomizePitch ? configSO.GetRandomPitch() : configSO.Pitch;
            audioSource.bypassEffects = configSO.BypassEffects;
            audioSource.bypassReverbZones = configSO.BypassEffects;
            audioSource.spatialBlend = configSO.SpatialBlend;
            audioSource.loop = configSO.Loop;
            audioSource.playOnAwake = configSO.PlayOnAwake;
        }

        public static void Play(this AudioSource audioSource, AudioClipSO audioClipSO, float delay = 0f)
        {
            if (audioClipSO == null) return;

            var audio = audioClipSO.GetClipAndConfig();
            if (audio.clip == null) return;

            audioSource.Configure(audio.config);

            if (delay <= 0f)
            {
                audioSource.clip = audio.clip;
                audioSource.Play();
            }
            else
            {
                audioSource.clip = audio.clip;
                audioSource.PlayDelayed(delay);
            }
        }

        public static bool PlayWithCooldown(this AudioSource audioSource, AudioClipSO audioClip, float delay = 0f)
        {
            if (audioClip == null) return false;
            if (audioClip.IsOnCooldown) return false;

            var audio = audioClip.GetClipAndConfig();
            if (audio.clip == null) return false;

            if (audio.config != null)
                audioSource.Configure(audio.config);

            if (delay > 0f)
            {
                double scheduledTime = AudioSettings.dspTime + delay;
                audioSource.clip = audio.clip;
                audioSource.PlayScheduled(scheduledTime);
            }
            else
            {
                audioSource.clip = audio.clip;
                audioSource.Play();
            }

            audioClip.MarkPlayed(delay);
            return true;
        }

        public static void PlayLoop(this AudioSource audioSource, AudioClipSO audioClipSO)
        {
            if (audioClipSO == null) return;

            var audio = audioClipSO.GetClipAndConfig();
            if (audio.clip == null) return;

            if (audio.config != null)
                audioSource.Configure(audio.config);

            audioSource.loop = true;
            audioSource.clip = audio.clip;
            audioSource.Play();
        }

        public static void StopLoop(this AudioSource audioSource)
        {
            audioSource.loop = false;
            audioSource.Stop();
        }

        public static Tween FadeOut(this AudioSource audioSource, float duration, Ease ease = Ease.InQuad)
        {
            float startVolume = audioSource.volume;

            return audioSource
                .DOFade(0f, duration)
                .SetEase(ease)
                .OnComplete(() =>
                {
                    audioSource.Stop();
                    audioSource.volume = startVolume;
                });
        }

        public static Tween FadeIn(this AudioSource audioSource, AudioClipSO audioClipSO, float duration, Ease ease = Ease.OutQuad)
        {
            if (audioClipSO == null) return null;

            var audio = audioClipSO.GetClipAndConfig();
            if (audio.clip == null) return null;

            if (audio.config != null)
                audioSource.Configure(audio.config);

            float targetVolume = audioSource.volume;
            audioSource.volume = 0f;
            audioSource.clip = audio.clip;
            audioSource.Play();

            return audioSource
                .DOFade(targetVolume, duration)
                .SetEase(ease);
        }

        public static Sequence CrossFade(this AudioSource audioSource, AudioSource targetSource, AudioClipSO audioClipSO, float duration, Ease ease = Ease.InOutQuad)
        {
            if (audioClipSO == null) return null;

            var audio = audioClipSO.GetClipAndConfig();
            if (audio.clip == null) return null;

            if (audio.config != null)
                targetSource.Configure(audio.config);

            float sourceStartVolume = audioSource.volume;
            float targetVolume = targetSource.volume;

            targetSource.volume = 0f;
            targetSource.clip = audio.clip;
            targetSource.Play();

            return DOTween.Sequence()
                .Join(audioSource.DOFade(0f, duration).SetEase(ease))
                .Join(targetSource.DOFade(targetVolume, duration).SetEase(ease))
                .OnComplete(() =>
                {
                    audioSource.Stop();
                    audioSource.volume = sourceStartVolume;
                });
        }
    }
}