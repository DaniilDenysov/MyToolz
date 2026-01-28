using MyToolz.EditorToolz;
using MyToolz.ScriptableObjects.GameSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace MyToolz.GameSettings
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class DropdownSettingViewAbstract<T> : SettingView<MultipleOptionSettingSO<T, List<T>>>
    {
        [SerializeField, Required] protected TMP_Dropdown dropdown;

        private void Awake()
        {
            if (dropdown != null) return;
            dropdown = GetComponent<TMP_Dropdown>();
        }

        protected override void Register()
        {
            base.Register();
            Awake();
            dropdown.onValueChanged.AddListener(OnInternalValueUpdated);
        }

        protected override void Deregister()
        {
            base.Deregister();
            Awake();
            dropdown.onValueChanged.RemoveListener(OnInternalValueUpdated);
        }

        private void OnInternalValueUpdated(int arg0)
        {
            var opts = setting.AvailableOptions;
            if (arg0 < 0 || arg0 >= opts.Count) return;
            setting.SetCurrentValue(opts[arg0]);
        }

        protected override void OnExternalValueUpdated(MultipleOptionSettingSO<T, List<T>> setting)
        {
            if (dropdown == null)
            {
                LogError($"Dropdown is missing, please assign it!");
                return;
            }
            dropdown.options = setting.AvailableOptions.Select(o => new TMP_Dropdown.OptionData(setting.ValueToString(o))).ToList();
            var idx = setting.CurrentValueIndex;
            if (idx < 0) idx = 0;
            if (idx >= dropdown.options.Count) idx = dropdown.options.Count - 1;

            dropdown.SetValueWithoutNotify(idx);

        }
    }
}
