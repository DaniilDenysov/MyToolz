using MyToolz.ScriptableObjects.GameSettings;
using MyToolz.Utilities.Debug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.GameSettings
{
    public abstract class AbstractButtonCarousel : Button, ISettingView
    {
        [Header("Setting")]
        [Tooltip("Int setting storing the selected state. Its [MinValue, MaxValue] range defines the cycle.")]
        [SerializeField] protected IntSettingSO setting;

        [Header("Output")]
        [Tooltip("Image showing the current state's sprite. Optional.")]
        [SerializeField] protected Image display;

        [Tooltip("Text showing the current state's label. Optional.")]
        [SerializeField] protected TMP_Text label;

        [Tooltip("How far each click moves through the states. Use -1 to cycle backwards.")]
        [SerializeField] protected int step = 1;

        public IntSettingSO Setting => setting;

        protected abstract int Count { get; }

        protected abstract Sprite GetSprite(int index);

        protected abstract string GetLabel(int index);

        protected override void OnEnable()
        {
            base.OnEnable();
            Register();
        }

        protected override void Start()
        {
            base.Start();
            Refresh();
            ValidateStates();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Deregister();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Deregister();
        }

        public void PreLoad()
        {
            Refresh();
        }

        public void Next()
        {
            if (setting == null)
            {
                DebugUtility.LogError(this, "SettingSO is missing, please reassign it!");
                return;
            }

            setting.SetCurrentValue(Wrap(setting.CurrentValue + step));
        }

        protected void Refresh()
        {
            if (setting == null || Count == 0)
            {
                return;
            }

            int index = Mathf.Clamp(setting.CurrentValue - setting.MinValue, 0, Count - 1);

            Sprite sprite = GetSprite(index);
            if (display != null && sprite != null)
            {
                display.sprite = sprite;
            }

            string text = GetLabel(index);
            if (label != null && text != null)
            {
                label.SetText(text);
            }
        }

        private void Register()
        {
            onClick.AddListener(Next);

            if (setting == null)
            {
                DebugUtility.LogError(this, "SettingSO is missing, please reassign it!");
                return;
            }

            setting.OnSettingUpdated += Refresh;
        }

        private void Deregister()
        {
            onClick.RemoveListener(Next);

            if (setting == null)
            {
                return;
            }

            setting.OnSettingUpdated -= Refresh;
        }

        private int Wrap(int value)
        {
            int count = setting.MaxValue - setting.MinValue + 1;
            if (count <= 0)
            {
                return setting.MinValue;
            }

            int offset = (((value - setting.MinValue) % count) + count) % count;
            return setting.MinValue + offset;
        }

        private void ValidateStates()
        {
            if (setting == null)
            {
                return;
            }

            int range = setting.MaxValue - setting.MinValue + 1;
            if (Count != range)
            {
                DebugUtility.LogWarning(this,
                    $"Carousel has {Count} state(s) but '{setting.name}' allows {range} value(s) " +
                    $"({setting.MinValue}..{setting.MaxValue}); they should match.");
            }
        }
    }
}
