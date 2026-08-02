using MyToolz.DesignPatterns.EventBus;
using TMPro;
using UnityEngine;

namespace MyToolz.Localization
{
    [AddComponentMenu("MyToolz/Localization/Localization Text")]
    public class LocalizationText : TextMeshProUGUI
    {
        [SerializeField] private LocalizationBindingSO binding;
        [SerializeField] private bool applyLanguageFont = true;

        private EventBinding<LanguageChanged> languageBinding;

        public LocalizationBindingSO Binding
        {
            get => binding;
            set
            {
                binding = value;
                Refresh();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (!Application.isPlaying)
            {
                return;
            }

            languageBinding ??= new EventBinding<LanguageChanged>(OnLanguageChanged);
            EventBus<LanguageChanged>.Register(languageBinding);
            Refresh();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (!Application.isPlaying || languageBinding == null)
            {
                return;
            }

            EventBus<LanguageChanged>.Deregister(languageBinding);
        }

        public void Refresh() => Apply(ResolveLanguage());

        private void OnLanguageChanged(LanguageChanged e) => Refresh();

        private void Apply(LocalizationLanguageSO language)
        {
            if (binding == null || binding.Database == null || language == null)
            {
                return;
            }

            if (applyLanguageFont && language.Font != null)
            {
                font = language.Font;
            }

            SetText(binding.Resolve(language));
        }

        private LocalizationLanguageSO ResolveLanguage()
        {
            if (Application.isPlaying && LocalizationManager.Instance != null)
            {
                return LocalizationManager.Instance.CurrentLanguage;
            }

            return binding != null && binding.Database != null ? binding.Database.DefaultLanguage : null;
        }

#if UNITY_EDITOR
        public void EditorPreview() => Apply(binding != null && binding.Database != null ? binding.Database.DefaultLanguage : null);
#endif
    }
}
