using MyToolz.DesignPatterns.EventBus;
using System;
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
        private object[] arguments;
        // Font authored on this component, restored for languages that do not define their own font.
        private TMP_FontAsset authoredFont;
        private bool authoredFontCaptured;

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

        /// <summary>
        /// Sets the format arguments substituted into the resolved (localized) string, which is treated
        /// as a composite format template - e.g. a binding that resolves to "Current score: {0}" with
        /// argument <c>0</c> renders "Current score: 0". The arguments are remembered, so a later
        /// refresh (a language change or a re-enable) re-applies them instead of erasing the caller's
        /// dynamic value. Pass no arguments to render the template verbatim.
        /// </summary>
        public void SetArguments(params object[] args)
        {
            arguments = args;
            Refresh();
        }

        private void OnLanguageChanged(LanguageChanged e) => Refresh();

        private void Apply(LocalizationLanguageSO language)
        {
            if (binding == null || binding.Database == null || language == null)
            {
                return;
            }

            if (applyLanguageFont)
            {
                if (!authoredFontCaptured)
                {
                    authoredFont = font;
                    authoredFontCaptured = true;
                }

                // Without this, switching from a language with a custom font to one without keeps
                // the previous language's font.
                TMP_FontAsset target = language.Font != null ? language.Font : authoredFont;
                if (target != null && font != target)
                {
                    font = target;
                }
            }

            SetText(Format(binding.Resolve(language)));
        }

        // Applies the stored arguments to the resolved template. A template with no placeholders (or no
        // stored arguments) is returned unchanged; a malformed template is returned as-is rather than
        // throwing, so a bad key never breaks rendering.
        private string Format(string value)
        {
            if (arguments == null || arguments.Length == 0)
            {
                return value;
            }

            try
            {
                return string.Format(value, arguments);
            }
            catch (FormatException)
            {
                return value;
            }
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
