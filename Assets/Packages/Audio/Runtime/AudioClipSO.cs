using MyToolz.EditorToolz;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.Audio
{
    [System.Serializable]
    public class AudioItemWithConfig
    {
        [SerializeField] private AudioClip audioClip;
        [SerializeField] private AudioSourceConfigSO soundConfig;

        public AudioClip AudioClip => audioClip;
        public AudioSourceConfigSO SoundConfig => soundConfig;
    }

    [CreateAssetMenu(fileName = "AudioClipSO", menuName = "MyToolz/AudioSystem/AudioClipSO")]
    public class AudioClipSO : ScriptableObject
    {
        [SerializeField] private bool randomize;
        [SerializeField] private bool useAudioConfigPerClip;
        [SerializeField, Range(0, 10f)] private float minimalInterval = 1f;

        public float MinimalInterval => minimalInterval;

        [SerializeField, ShowIf("@!randomize && !useAudioConfigPerClip")]
        private AudioClip clip;

        [SerializeField, ShowIf("@randomize && !useAudioConfigPerClip")]
        private AudioClip[] audioClips;

        [SerializeField, ShowIf("@!useAudioConfigPerClip")]
        private AudioSourceConfigSO globalSoundConfig;

        [SerializeField, ShowIf("@useAudioConfigPerClip")]
        private AudioItemWithConfig[] audioItemWithConfigs;

        private float lastPlayedTime = float.NegativeInfinity;

        public bool IsOnCooldown => lastPlayedTime + minimalInterval > Time.time;

        public void ResetCooldown()
        {
            lastPlayedTime = float.NegativeInfinity;
        }

        public void MarkPlayed(float delay = 0f)
        {
            lastPlayedTime = Time.time + delay;
        }

        public (AudioClip clip, AudioSourceConfigSO config) GetClipAndConfig()
        {
            if (!useAudioConfigPerClip)
            {
                AudioClip selectedClip = randomize && audioClips != null && audioClips.Length > 0
                    ? audioClips[Random.Range(0, audioClips.Length)]
                    : clip;

                if (selectedClip == null)
                    DebugUtility.LogWarning(this, $"'{name}' returned a null AudioClip.");

                return (selectedClip, globalSoundConfig);
            }

            if (audioItemWithConfigs != null && audioItemWithConfigs.Length > 0)
            {
                var selectedItem = audioItemWithConfigs[Random.Range(0, audioItemWithConfigs.Length)];
                return (selectedItem.AudioClip, selectedItem.SoundConfig);
            }

            DebugUtility.LogWarning(this, $"'{name}' has no audio items configured.");
            return (null, null);
        }
    }
}
