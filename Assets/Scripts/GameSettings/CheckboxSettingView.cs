using UnityEngine;
using UnityEngine.UI;
using MyToolz.ScriptableObjects.GameSettings;
using MyToolz.EditorToolz;

namespace MyToolz.GameSettings
{
    [RequireComponent(typeof(Toggle))]
    public class CheckboxSettingView : SettingView<BoolSettingSO>
    {
        [SerializeField, Required] private Toggle toggle;

        private void Awake()
        {
            if (toggle != null) return;
            toggle = GetComponent<Toggle>();
        }

        protected override void Register()
        {
            base.Register();
            Awake();
            toggle.onValueChanged.AddListener(OnInternalValueUpdated);
        }

        protected override void Deregister()
        {
            base.Deregister();
            Awake();
            toggle.onValueChanged.RemoveListener(OnInternalValueUpdated);
        }

        private void OnInternalValueUpdated(bool arg0)
        {
            setting.SetCurrentValue(arg0);
        }

        protected override void OnExternalValueUpdated(BoolSettingSO setting)
        {
            if (toggle == null)
            {
                LogError($"Toggle is missing, please assign it!");
                return;
            }
            toggle.SetIsOnWithoutNotify(setting.CurrentValue);
        }
    }
}