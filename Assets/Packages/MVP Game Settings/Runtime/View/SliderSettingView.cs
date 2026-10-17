using UnityEngine;
using UnityEngine.UI;
using MyToolz.ScriptableObjects.GameSettings;
using MyToolz.EditorToolz;
using MyToolz.Extensions;
using MyToolz.Utilities.Debug;
using TMPro;

namespace MyToolz.GameSettings
{
    [RequireComponent(typeof(Slider))]
    public class SliderSettingView : SettingView<FloatSettingSO>
    {
        [SerializeField] private TMP_Text display;
        [SerializeField] private string prefix = "Placeholder:";
        [SerializeReference] private ValueDisplayFormat valueFormat = new PercentDisplayFormat();
        [SerializeField, Required] private Slider slider;
        private bool applyingExternalValue;

        private void Awake()
        {
            if (slider != null)
            {
                return;
            }

            slider = GetComponent<Slider>();
        }

        private void UpdateText()
        {
            if (display == null)
            {
                DebugUtility.Log(this, "Display is missing, might not be intentional!");
                return;
            }
            float valueCurr = slider.value - slider.minValue;
            float valueMax = slider.maxValue - slider.minValue;
            display.SetText($"{prefix}{valueFormat.Format((valueCurr / valueMax) * 100)}");
        }

        protected override void Register()
        {
            base.Register();
            Awake();
            slider.onValueChanged.AddListener(OnInternalValueUpdated);
        }

        protected override void Deregister()
        {
            base.Deregister();
            Awake();
            slider.onValueChanged.RemoveListener(OnInternalValueUpdated);
        }

        private void OnInternalValueUpdated(float arg0)
        {
            if (applyingExternalValue)
            {
                return;
            }
            setting.SetCurrentValue(arg0);
            UpdateText();
        }

        protected override void OnExternalValueUpdated(FloatSettingSO setting)
        {
            if (slider == null)
            {
                DebugUtility.LogError(this, $"Slider is missing, please assign it!");
                return;
            }
            // Assigning the value (rather than SetValueWithoutNotify) keeps the handle in sync; the flag stops the
            // resulting callback from writing the same value back to the setting.
            applyingExternalValue = true;
            slider.wholeNumbers = false;
            slider.minValue = setting.MinValue.ToFloat();
            slider.maxValue = setting.MaxValue.ToFloat();
            slider.value = setting.CurrentValue.ToFloat();
            applyingExternalValue = false;
            UpdateText();
        }
    }
}
