using MyToolz.EditorToolz;
using MyToolz.ScriptableObjects.GameSettings;
using MyToolz.Utilities.Debug;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.GameSettings
{
    public class ToggleSettingView : SettingView<BoolSettingSO>
    {
        [SerializeField, Required] protected Toggle toggle;

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
                DebugUtility.LogError(this, $"Toggle is missing, please assign it!");
                return;
            }
            toggle.SetIsOnWithoutNotify(setting.CurrentValue);
        }
    }
}
