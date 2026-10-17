using System.Reflection;
using MyToolz.GameSettings;
using MyToolz.ScriptableObjects.GameSettings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.Tests.EditMode.UI
{
    public class SliderSettingViewTests
    {
        private GameObject go;
        private FloatSettingSO setting;
        private Slider slider;
        private SliderSettingView view;

        [SetUp]
        public void SetUp()
        {
            setting = ScriptableObject.CreateInstance<FloatSettingSO>();
            SetSettingField("minValue", 0d);
            SetSettingField("maxValue", 1d);

            go = new GameObject("Slider", typeof(RectTransform));
            slider = go.AddComponent<Slider>();
            view = go.AddComponent<SliderSettingView>();
            typeof(SliderSettingView).GetField("slider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, slider);
            typeof(SettingView<FloatSettingSO>).GetField("setting", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, setting);

            // Edit mode does not run OnEnable for components added from code.
            typeof(SettingView<FloatSettingSO>).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(setting);
        }

        [Test]
        public void FirstPlayerChange_IsKept_WhenTheLoadedValueMatchedTheSlider()
        {
            SetSettingField("defaultValue", 0d); // equals the slider's own value, so assigning it raises no callback
            ApplyExternalValue();

            slider.value = 0.6f; // the player's first drag

            Assert.That(setting.HasValue, Is.True, "the first change reaches the setting");
            Assert.That(setting.CurrentValue, Is.EqualTo(0.6d).Within(1e-4));
        }

        [Test]
        public void ApplyingAnExternalValue_DoesNotWriteItBack()
        {
            SetSettingField("defaultValue", 0.25d);
            ApplyExternalValue();

            Assert.That(slider.value, Is.EqualTo(0.25f).Within(1e-4));
            Assert.That(setting.HasValue, Is.False, "showing the stored value is not a change by the player");
        }

        private void ApplyExternalValue() =>
            typeof(SliderSettingView)
                .GetMethod("OnExternalValueUpdated", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(FloatSettingSO) }, null)
                .Invoke(view, new object[] { setting });

        private void SetSettingField(string name, object value)
        {
            System.Type type = typeof(FloatSettingSO);
            FieldInfo field = null;
            while (field == null && type != null)
            {
                field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                type = type.BaseType;
            }
            field.SetValue(setting, value);
        }
    }
}
