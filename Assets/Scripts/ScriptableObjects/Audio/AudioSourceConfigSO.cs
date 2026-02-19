using MyToolz.ScriptableObjects.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace MyToolz.ScriptableObjects.Audio
{
    [CreateAssetMenu(fileName = "AudioSourceConfigSO", menuName = "Data/AudioSystem/AudioSourceConfig")]
    public class AudioSourceConfigSO : ScriptableObject
    {
        [SerializeField] private AudioMixerGroup audioMixer;
        public AudioMixerGroup AudioMixer
        {
            get => audioMixer;
        }

        [SerializeField] private bool isGlobal = true;
        public bool IsGlobal { get => isGlobal; }

        [Header("Core Audio")]
        [SerializeField, Range(0, 1)] private float volume;
        public float Volume { get => volume; }

        [SerializeField, Range(0f, 1f)] private float spatialBlend = 0f;
        public float SpatialBlend { get => spatialBlend; }

        [Header("Playback Settings")]
        [SerializeField] private bool playOnAwake;
        public bool PlayOnAwake { get => playOnAwake; }

        [SerializeField] private bool loop = false;
        public bool Loop { get => loop; }


        [Header("Pitch & Variation")]
        [SerializeField, Range(-3f, 3f)] private float pitch = 1f;
        public float Pitch { get => pitch; }

        [SerializeField] private bool randomizePitch = false;
        public bool RandomizePitch { get => randomizePitch; }

        [SerializeField] private Vector2 pitchRange;
        public Vector2 PitchRange { get => pitchRange; }

        public float MaxPitch
        {
            get => pitchRange.y;
        }

        public float MinPitch
        {
            get => pitchRange.x;
        }

        public float GetRandomPitch() => Random.Range(pitchRange.x, pitchRange.y);

        [Header("Advanced")]
        [SerializeField] private bool bypassEffects = false;
        public bool BypassEffects { get => bypassEffects; }
    }
}

namespace MyToolz.Extensions
{
    public static class AudioSourceExtensions
    {
        public static void Configure(this AudioSource audioSource, AudioSourceConfigSO configSO)
        {
            if (audioSource == null || configSO == null) return;
            audioSource.outputAudioMixerGroup = configSO.AudioMixer;
            audioSource.volume = configSO.Volume;
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
                audioSource.PlayOneShot(audio.clip);
            }
            else
            {
                audioSource.clip = audio.clip;
                audioSource.PlayDelayed(delay);
            }
        }

        public static float Play(this AudioSource audioSource, AudioClipSO audioClip, float lastPlayed, float delay = 0f)
        {
            if (audioClip == null) return lastPlayed;
            var audio = audioClip.GetClipAndConfig();
            if (audio.clip == null) return lastPlayed;
            if (lastPlayed + audioClip.MinimalInterval > Time.time) return lastPlayed;

            if (audio.config != null)
            {
                audioSource.Configure(audio.config);
            }

            if (delay > 0)
            {
                double currentDSPTime = AudioSettings.dspTime;
                double scheduledTime = currentDSPTime + delay;

                audioSource.clip = audio.clip;
                audioSource.PlayScheduled(scheduledTime);
            }
            else
            {
                audioSource.PlayOneShot(audio.clip);
            }

            return Time.time + delay;
        }

        public static void PlayLoop(this AudioSource audioSource, AudioClipSO audioClipSO)
        {
            if (audioClipSO == null) return;
            var audio = audioClipSO.GetClipAndConfig();
            if (audio.clip == null) return;
            if (audio.config != null) audioSource.Configure(audio.config);
            audioSource.loop = true;
            audioSource.clip = audio.clip;
            audioSource.Play();
        }
    }
}
