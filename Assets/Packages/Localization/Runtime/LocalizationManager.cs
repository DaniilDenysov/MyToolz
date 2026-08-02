using System;
using System.Collections.Generic;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.Singleton;
using MyToolz.EditorToolz;
using MyToolz.ScriptableObjects.GameSettings;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.Localization
{
    [AddComponentMenu("MyToolz/Localization/Localization Manager")]
    public class LocalizationManager : PublicSingleton<LocalizationManager>
    {
        [Header("Localization")]
        [SerializeField, Required] private LocalizationDatabaseSO database;
        [SerializeField] private LocalizationLanguageSO defaultLanguage;
        [Tooltip("Persists the selected language across sessions via GameSettings (stores the language code). Register it in the SettingsPresenter so it is saved and loaded.")]
        [SerializeField] private StringSettingSO languageSetting;

        [ShowInInspector, ReadOnly] private LocalizationLanguageSO currentLanguage;

        private EventBinding<ChangeLanguageRequest> changeBinding;

        public LocalizationDatabaseSO Database => database;

        public LocalizationLanguageSO CurrentLanguage => currentLanguage;

        public IReadOnlyList<LocalizationLanguageSO> Languages
            => database != null ? database.Languages : Array.Empty<LocalizationLanguageSO>();

        protected override void OnSingletonAwake()
        {
            if (database == null)
            {
                DebugUtility.LogError(this, "No translation database assigned.");
                return;
            }

            database.Reload();

            changeBinding = new EventBinding<ChangeLanguageRequest>(OnChangeLanguageRequest);
            EventBus<ChangeLanguageRequest>.Register(changeBinding);

            currentLanguage = ResolveInitialLanguage();
            Broadcast();
        }

        protected override void OnSingletonDestroy()
        {
            if (changeBinding != null)
            {
                EventBus<ChangeLanguageRequest>.Deregister(changeBinding);
            }
        }

        public void SetLanguage(LocalizationLanguageSO language)
        {
            if (language == null || database == null || !database.Contains(language))
            {
                DebugUtility.LogWarning(this, "Requested language is null or not part of the database.");
                return;
            }

            if (language == currentLanguage)
            {
                return;
            }

            currentLanguage = language;
            Persist();
            Broadcast();

            DebugUtility.Log(this, $"Language changed to {currentLanguage.DisplayName}.");
        }

        public string Translate(string key)
            => database != null && database.TryTranslate(key, currentLanguage, out string value) ? value : key;

        public bool TryTranslate(string key, out string value)
        {
            value = null;
            return database != null && database.TryTranslate(key, currentLanguage, out value);
        }

        private void OnChangeLanguageRequest(ChangeLanguageRequest request) => SetLanguage(request.Language);

        private void Broadcast() => EventBus<LanguageChanged>.Raise(new LanguageChanged { Language = currentLanguage });

        private LocalizationLanguageSO ResolveInitialLanguage()
        {
            if (languageSetting != null)
            {
                string savedCode = languageSetting.CurrentValue;
                if (!string.IsNullOrEmpty(savedCode))
                {
                    foreach (LocalizationLanguageSO language in database.Languages)
                    {
                        if (language != null && language.Code == savedCode)
                        {
                            return language;
                        }
                    }
                }
            }

            if (defaultLanguage != null && database.Contains(defaultLanguage))
            {
                return defaultLanguage;
            }

            return database.DefaultLanguage;
        }

        private void Persist()
        {
            if (languageSetting == null || currentLanguage == null)
            {
                return;
            }

            languageSetting.SetCurrentValue(currentLanguage.Code);
        }

        [Button("Next Language", mode: ButtonMode.PlaymodeOnly)]
        private void NextLanguage()
        {
            IReadOnlyList<LocalizationLanguageSO> all = Languages;
            if (all.Count == 0)
            {
                return;
            }

            int index = IndexOf(all, currentLanguage);
            SetLanguage(all[(index + 1) % all.Count]);
        }

        private static int IndexOf(IReadOnlyList<LocalizationLanguageSO> list, LocalizationLanguageSO item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == item)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
