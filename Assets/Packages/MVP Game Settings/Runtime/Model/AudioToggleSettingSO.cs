using UnityEngine;
using UnityEngine.Audio;
using MyToolz.EditorToolz;
using MyToolz.Utilities.Debug;

namespace MyToolz.ScriptableObjects.GameSettings
{
    [CreateAssetMenu(fileName = "AudioToggleSettingSO", menuName = "MyToolz/GameSettings/AudioToggleSettingSO")]
    public class AudioToggleSettingSO : BoolSettingSO
    {
        [Header("Audio")]
        [SerializeField, Required] private AudioMixer audioMixer;
        [SerializeField, Required] private string exposedParameter = "Music";
        [SerializeField, Tooltip("Level applied while the toggle is on.")] private float onDecibels = 0f;
        [SerializeField, Tooltip("Level applied while the toggle is off. -80 is silence.")] private float offDecibels = -80f;

        protected override void OnSetted()
        {
            ApplyCurrent();
        }

        protected override void OnLoaded()
        {
            ApplyCurrent();
        }

        public void ApplyCurrent()
        {
            if (audioMixer == null || string.IsNullOrEmpty(exposedParameter))
            {
                DebugUtility.LogError(this, "AudioMixer or exposed parameter is not set.");
                return;
            }

            audioMixer.SetFloat(exposedParameter, CurrentValue ? onDecibels : offDecibels);
        }
    }
}
