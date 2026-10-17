using UnityEngine;
using UnityEngine.Audio;
using MyToolz.EditorToolz;
using MyToolz.Extensions;
using MyToolz.Utilities.Debug;
using MyToolz.GameSettings.Data;

namespace MyToolz.ScriptableObjects.GameSettings
{
    [CreateAssetMenu(fileName = "AudioSettingSO", menuName = "MyToolz/GameSettings/AudioSettingSO")]
    public class AudioSettingSO : FloatSettingSO
    {
        [Header("Audio")]
        [SerializeField, Required] private AudioMixer audioMixer;
        [SerializeField, Required] private string exposedParameter = "Music";
        [SerializeField] private float minDecibels = -80f;

        protected override void OnLoaded()
        {
            ApplyCurrent();
        }

        // Runs for this asset and every twin, so all loaded copies drive the mixer.
        protected override void OnSetted()
        {
            ApplyCurrent();
        }

        // With no saved value, push the default so the mixer matches what the UI shows.
        protected override void OnLoadFinished()
        {
            ApplyCurrent();
        }

        public override void SetCurrentValue(double newValue)
        {
            newValue = Mathf.Clamp(newValue.ToFloat(), minValue.ToFloat(), maxValue.ToFloat());
            base.SetCurrentValue(newValue);
        }

        public void ApplyCurrent()
        {
            if (audioMixer == null || string.IsNullOrEmpty(exposedParameter))
            {
                DebugUtility.LogError(this, "AudioMixer or exposed parameter is not set.");
                return;
            }

            if (maxValue == 0)
            {
                DebugUtility.LogError(this, "Max value cannot be 0");
                return;
            }

            // CurrentValue (not the raw field) so an unset setting applies its default instead of 0.
            float linear = Mathf.Max(CurrentValue.ToFloat() / maxValue.ToFloat(), 0.0001f);

            float db = Mathf.Log10(linear) * 20f;
            db = Mathf.Max(db, minDecibels);

            audioMixer.SetFloat(exposedParameter, db);
        }
    }
}
