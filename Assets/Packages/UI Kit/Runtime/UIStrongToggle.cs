using MyToolz.EditorToolz;
using MyToolz.Audio;
using MyToolz.Audio.Events;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.GameSettings;
using MyToolz.ScriptableObjects.GameSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using MyToolz.Tweener.UI;

namespace MyToolz.UI.Kit
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MyToolz/UI Kit/Strong Toggle")]
    public class UIStrongToggle : UnityEngine.UI.Toggle, ISettingView, IUITweenClickOwner
    {
        [SerializeField] private BoolSettingSO setting;

        [Header("Icon Skin")]
        [SerializeField, Required] private UnityEngine.UI.Image icon;

        [SerializeField] private Sprite onSprite;

        [SerializeField] private Sprite offSprite;

        [Header("Audio (optional - leave empty for no sound)")]
        [SerializeField] private AudioClipSO clickClip;

        [SerializeField] private AudioClipSO pointerEnterClip;

        [SerializeField] private AudioClipSO pointerDownClip;

        [SerializeField] private AudioClipSO disableClip;

        private bool registered;

        protected override void OnEnable()
        {
            base.OnEnable();
            Register();
            RefreshFromSetting();
        }

        protected override void OnDisable()
        {
            Deregister();
            base.OnDisable();

            if (Application.isPlaying && gameObject.scene.isLoaded)
            {
                PlayClip(disableClip);
            }
        }

        protected override void OnDestroy()
        {
            Deregister();
            base.OnDestroy();
        }

        public void PreLoad() => RefreshFromSetting();

        public void Refresh() => RefreshFromSetting();

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && IsActive() && IsInteractable())
            {
                base.OnPointerClick(eventData);
                PlayClip(clickClip);
                GetComponent<UITweener>()?.CreateSequence(ActivationTrigger.OnClick);
            }
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            if (IsActive() && IsInteractable())
            {
                base.OnSubmit(eventData);
                PlayClip(clickClip);
                GetComponent<UITweener>()?.CreateSequence(ActivationTrigger.OnClick);
            }
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);

            if (IsInteractable())
            {
                PlayClip(pointerEnterClip);
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            if (eventData.button == PointerEventData.InputButton.Left && IsInteractable())
            {
                PlayClip(pointerDownClip);
            }
        }

        private void Register()
        {
            if (registered)
            {
                return;
            }

            if (setting != null)
            {
                setting.OnSettingUpdated += RefreshFromSetting;
                setting.OnLoadCompleted += RefreshFromSetting;
            }
            onValueChanged.AddListener(OnToggleValueChanged);
            registered = true;
        }

        private void Deregister()
        {
            if (!registered)
            {
                return;
            }

            if (setting != null)
            {
                setting.OnSettingUpdated -= RefreshFromSetting;
                setting.OnLoadCompleted -= RefreshFromSetting;
            }
            onValueChanged.RemoveListener(OnToggleValueChanged);
            registered = false;
        }

        private void OnToggleValueChanged(bool value)
        {
            if (setting != null)
                setting.SetCurrentValue(value);
            ApplyIcon(value);
        }

        private void RefreshFromSetting()
        {
            if (setting != null)
                SetIsOnWithoutNotify(setting.CurrentValue);
            ApplyIcon(isOn);
        }

        private void ApplyIcon(bool value)
        {
            if (icon == null)
            {
                return;
            }

            Sprite next = value ? onSprite : offSprite;

            if (next != null)
            {
                icon.sprite = next;
            }
        }

        private void PlayClip(AudioClipSO clip)
        {
            if (clip == null)
            {
                return;
            }

            EventBus<PlayAudioClipSO>.Raise(new PlayAudioClipSO
            {
                AudioClipSO = clip
            });
        }
    }
}
