using MyToolz.EditorToolz;
using MyToolz.GameSettings;
using MyToolz.ScriptableObjects.GameSettings;
using MyToolz.UI.Layout;
using UnityEngine;

namespace MyToolz.UI.Kit
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MyToolz/UI Kit/Switch Button")]
    public class SwitchButton : UIStrongButton, ISettingView
    {
        [SerializeField] private IntSettingSO setting;

        [SerializeField] private UnityEngine.UI.Image icon;

        [SerializeReference, SubclassSelector]
        private SwitchButtonStrategy strategy;

        private bool applyingSetting;

        protected override void Start()
        {
            base.Start();
            ApplySetting();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Bind(SelectNext);

            if (setting != null)
            {
                setting.OnSettingUpdated += ApplySetting;
                setting.OnLoadCompleted += ApplySetting;
            }

            if (strategy != null)
            {
                strategy.Register(SynchronizeSettingFromStrategy);
            }

            ApplySetting();
        }

        protected override void OnDisable()
        {
            if (setting != null)
            {
                setting.OnSettingUpdated -= ApplySetting;
                setting.OnLoadCompleted -= ApplySetting;
            }

            if (strategy != null)
            {
                strategy.Deregister(SynchronizeSettingFromStrategy);
            }

            Unbind(SelectNext);

            base.OnDisable();
        }

        public void PreLoad() => ApplySetting();

        private void SelectNext()
        {
            if (strategy == null)
            {
                return;
            }

            Sprite[] icons = strategy.Icons;

            if (icons == null || icons.Length < 2)
            {
                return;
            }

            int current = Mathf.Clamp(setting != null ? setting.CurrentValue - setting.MinValue : strategy.CurrentIndex, 0, icons.Length - 1);
            int next = (current + 1) % icons.Length;
            if (setting != null)
                setting.SetCurrentValue(setting.MinValue + next);
            else
            {
                strategy.Select(next);
                RefreshIcon(icons, next);
            }
        }

        private void ApplySetting()
        {
            if (strategy == null || applyingSetting)
            {
                return;
            }

            Sprite[] icons = strategy.Icons;

            if (icons == null || icons.Length == 0)
            {
                return;
            }

            int index = Mathf.Clamp(setting != null ? setting.CurrentValue - setting.MinValue : strategy.CurrentIndex, 0, icons.Length - 1);
            applyingSetting = true;
            try
            {
                strategy.Select(index);
                RefreshIcon(icons, index);
            }
            finally
            {
                applyingSetting = false;
            }
        }

        private void SynchronizeSettingFromStrategy()
        {
            if (strategy == null || applyingSetting)
            {
                return;
            }

            int index = strategy.CurrentIndex;
            int value = setting != null ? setting.MinValue + index : index;

            if (setting != null && index >= 0 && value >= setting.MinValue && value <= setting.MaxValue && setting.CurrentValue != value)
            {
                setting.SetCurrentValue(value);
                return;
            }

            Sprite[] icons = strategy.Icons;

            if (icons != null && icons.Length > 0)
            {
                RefreshIcon(icons, Mathf.Clamp(index, 0, icons.Length - 1));
            }
        }

        private void RefreshIcon(Sprite[] icons, int index)
        {
            if (icon == null)
            {
                return;
            }

            Sprite next = icons[index];

            if (next != null && icon.sprite != next)
            {
                icon.sprite = next;
            }
        }
    }
}
