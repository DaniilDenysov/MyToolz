using UnityEngine;
using TMPro;
using MyToolz.EditorToolz;
using MyToolz.ScriptableObjects.GameSettings;

namespace MyToolz.GameSettings
{

    [RequireComponent(typeof(TMP_InputField))]
    public class InputFieldSettingView : SettingView<StringSettingSO> 
    {
        [SerializeField, Required] private TMP_InputField inputField;

        private void Awake()
        {
            if (inputField != null) return;
            inputField = GetComponent<TMP_InputField>();
        }

        protected override void Register()
        {
            base.Register();
            Awake();
            inputField.onValueChanged.AddListener(OnInternalValueUpdated);
        }

        protected override void Deregister()
        {
            base.Deregister();
            Awake();
            inputField.onValueChanged.RemoveListener(OnInternalValueUpdated);
        }

        private void OnInternalValueUpdated(string arg0)
        {
            setting.SetCurrentValue(arg0);
        }

        protected override void OnExternalValueUpdated(StringSettingSO setting)
        {
            if (inputField == null)
            {
                LogError($"InputField is missing, please assign it!");
                return;
            }
            inputField.SetTextWithoutNotify(setting.CurrentValue);
        }
    }
}