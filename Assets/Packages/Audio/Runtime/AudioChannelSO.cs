using System;
using UnityEngine;

namespace MyToolz.Audio
{
    /// <summary>
    /// A volume bus that needs no <see cref="UnityEngine.Audio.AudioMixer"/>. Assign it to <see cref="AudioSourceConfigSO"/>s
    /// and every source configured from them plays at <c>config volume × channel volume</c>; songs follow it live.
    /// Use it where mixer groups are unavailable (the Web platform ignores them) or for a simple music/SFX split.
    /// The volume is runtime state: it starts at <see cref="DefaultVolume"/> and is usually driven by a setting.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioChannelSO", menuName = "MyToolz/AudioSystem/AudioChannel")]
    public class AudioChannelSO : ScriptableObject
    {
        [SerializeField, Range(0f, 1f)] private float defaultVolume = 1f;

        [NonSerialized] private float volume;
        [NonSerialized] private bool hasVolume;

        public float DefaultVolume => defaultVolume;
        public float Volume => hasVolume ? volume : defaultVolume;

        public event Action<float> Changed;

        public void SetVolume(float value)
        {
            value = Mathf.Clamp01(value);
            if (hasVolume && Mathf.Approximately(volume, value))
            {
                return;
            }

            volume = value;
            hasVolume = true;
            Changed?.Invoke(value);
        }

        private void OnEnable()
        {
            hasVolume = false;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= ResetOnExit;
            UnityEditor.EditorApplication.playModeStateChanged += ResetOnExit;
#endif
        }

#if UNITY_EDITOR
        private void OnDisable()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= ResetOnExit;
        }

        private void ResetOnExit(UnityEditor.PlayModeStateChange change)
        {
            if (change == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                hasVolume = false;
            }
        }
#endif
    }
}
