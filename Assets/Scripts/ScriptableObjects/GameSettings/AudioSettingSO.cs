using UnityEngine;
using UnityEngine.Audio;
using MyToolz.EditorToolz;
using MyToolz.Extensions;

namespace MyToolz.ScriptableObjects.GameSettings
{
    [CreateAssetMenu(fileName = "AudioSettingSO", menuName = "MyToolz/GameSettings/AudioSettingSO")]
    public class AudioSettingSO : FloatSettingSO
    {
        [Header("Audio")]
        [SerializeField, Required] private AudioMixer audioMixer;
        [SerializeField, Required] private string exposedParameter = "Music";
        [SerializeField] private float minDecibels = -80f;


        public override void SetCurrentValue(double newValue)
        {
            newValue = Mathf.Clamp(newValue.ToFloat(), minValue.ToFloat(), maxValue.ToFloat());
            base.SetCurrentValue(newValue);
            ApplyCurrent();
        }

        public void ApplyCurrent()
        {
            if (audioMixer == null || string.IsNullOrEmpty(exposedParameter))
            {
                LogError("AudioMixer or exposed parameter is not set.");
                return;
            }

            float linear = Mathf.Max(currentValue.ToFloat(), 0.0001f);

            float db = Mathf.Log10(linear) * 20f;
            db = Mathf.Max(db, minDecibels);

            audioMixer.SetFloat(exposedParameter, db);
        }
    }
}
