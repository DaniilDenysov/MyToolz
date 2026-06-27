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
        private bool initialSet = true; //set value without notify doesn't update slider, so initial set is the way to bypass

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
            if (initialSet)
            {
                initialSet = false;
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
            slider.minValue = setting.MinValue.ToFloat();
            slider.maxValue = setting.MaxValue.ToFloat();
            slider.value = setting.CurrentValue.ToFloat();
            slider.wholeNumbers = false;
            UpdateText();
        }
    }
}
