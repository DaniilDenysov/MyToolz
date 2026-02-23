using UnityEngine;
using UnityEngine.UI;
using MyToolz.ScriptableObjects.GameSettings;
using MyToolz.EditorToolz;
using MyToolz.Extensions;
using MyToolz.Utilities.Debug;

namespace MyToolz.GameSettings
{
    [RequireComponent(typeof(Slider))]
    public class SliderSettingView : SettingView<FloatSettingSO>
    {
        [SerializeField, Required] private Slider slider;

        private void Awake()
        {
            if (slider != null) return;
            slider = GetComponent<Slider>();
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
            setting.SetCurrentValue(arg0);
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
            slider.SetValueWithoutNotify(setting.CurrentValue.ToFloat());
        }
    }
}