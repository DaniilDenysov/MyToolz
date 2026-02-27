using MyToolz.EditorToolz;
using UnityEngine;
using UnityEngine.Audio;

namespace MyToolz.Audio
{
    [CreateAssetMenu(fileName = "AudioSourceConfigSO", menuName = "MyToolz/AudioSystem/AudioSourceConfig")]
    public class AudioSourceConfigSO : ScriptableObject
    {
        [SerializeField] private AudioMixerGroup audioMixer;
        public AudioMixerGroup AudioMixer => audioMixer;

        [SerializeField] private bool isGlobal = true;
        public bool IsGlobal => isGlobal;

        [Header("Core Audio")]
        [SerializeField, Range(0, 1)] private float volume;
        public float Volume => volume;

        [SerializeField] private bool randomizeVolume;
        public bool RandomizeVolume => randomizeVolume;

        [SerializeField, ShowIf("@randomizeVolume")]
        private Vector2 volumeRange = new Vector2(0.8f, 1f);
        public Vector2 VolumeRange => volumeRange;

        public float GetRandomVolume() => Random.Range(volumeRange.x, volumeRange.y);

        [SerializeField, Range(0f, 1f)] private float spatialBlend = 0f;
        public float SpatialBlend => spatialBlend;

        [Header("Playback Settings")]
        [SerializeField] private bool playOnAwake;
        public bool PlayOnAwake => playOnAwake;

        [SerializeField] private bool loop;
        public bool Loop => loop;

        [Header("Pitch & Variation")]
        [SerializeField, Range(-3f, 3f)] private float pitch = 1f;
        public float Pitch => pitch;

        [SerializeField] private bool randomizePitch;
        public bool RandomizePitch => randomizePitch;

        [SerializeField, ShowIf("@randomizePitch")]
        private Vector2 pitchRange = new Vector2(0.9f, 1.1f);
        public Vector2 PitchRange => pitchRange;

        public float MaxPitch => pitchRange.y;
        public float MinPitch => pitchRange.x;

        public float GetRandomPitch() => Random.Range(pitchRange.x, pitchRange.y);

        [Header("Advanced")]
        [SerializeField] private bool bypassEffects;
        public bool BypassEffects => bypassEffects;
    }
}
