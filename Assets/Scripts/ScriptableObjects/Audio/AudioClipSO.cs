using Sirenix.OdinInspector;
using UnityEngine;

namespace MyToolz.ScriptableObjects.Audio
{
    [System.Serializable]
    public class AudioItemWithConfig
    {
        [SerializeField] private AudioClip audioClip;
        [SerializeField] private AudioSourceConfigSO soundConfig;

        public AudioClip AudioClip => audioClip;
        public AudioSourceConfigSO SoundConfig => soundConfig;
    }

    [CreateAssetMenu(fileName = "AudioClipSO", menuName = "Data/AudioSystem/AudioClipSO")]
    public class AudioClipSO : ScriptableObject
    {
        [SerializeField] private bool randomize;
        [SerializeField] private bool useAudioConfigPerClip;
        [SerializeField, Range(0, 10f)] private float minimalInterval = 1f;
        public float MinimalInterval
        {
            get => minimalInterval;
        }

        [SerializeField, ShowIf("@!randomize && !useAudioConfigPerClip")]
        private AudioClip clip;

        [SerializeField, ShowIf("@randomize && !useAudioConfigPerClip")]
        private AudioClip[] audioClips;

        [SerializeField, ShowIf("@!useAudioConfigPerClip")]
        private AudioSourceConfigSO globalSoundConfig;

        [SerializeField, ShowIf("@useAudioConfigPerClip")]
        private AudioItemWithConfig[] audioItemWithConfigs;

        public (AudioClip clip, AudioSourceConfigSO config) GetClipAndConfig()
        {
            if (!useAudioConfigPerClip)
            {
                AudioClip selectedClip = randomize && audioClips != null && audioClips.Length > 0
                    ? audioClips[Random.Range(0, audioClips.Length)]
                    : clip;
                return (selectedClip, globalSoundConfig);
            }
            else
            {
                if (audioItemWithConfigs != null && audioItemWithConfigs.Length > 0)
                {
                    var selectedItem = audioItemWithConfigs[Random.Range(0, audioItemWithConfigs.Length)];
                    return (selectedItem.AudioClip, selectedItem.SoundConfig);
                }
                return (null, null);
            }
        }
    }
}
