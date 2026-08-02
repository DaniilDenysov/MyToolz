using MyToolz.DesignPatterns.EventBus;

namespace MyToolz.Localization
{
    public struct ChangeLanguageRequest : IEvent
    {
        public LocalizationLanguageSO Language;
    }

    public struct LanguageChanged : IEvent
    {
        public LocalizationLanguageSO Language;
    }
}
