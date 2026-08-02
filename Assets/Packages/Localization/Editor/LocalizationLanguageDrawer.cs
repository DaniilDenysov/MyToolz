#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace MyToolz.Localization.Editor
{
    [CustomPropertyDrawer(typeof(LocalizationLanguageAttribute))]
    public sealed class LocalizationLanguageDrawer : LocalizationDropdownDrawer
    {
        protected override string DatabaseFieldName => ((LocalizationLanguageAttribute)attribute).DatabaseField;

        protected override string EmptyLabel => "<Select language>";

        protected override string MissingTooltip => "This language is not present in the current CSV.";

        protected override IReadOnlyList<string> GetOptions(LocalizationDatabaseSO database)
        {
            database.Reload();
            return database.Header;
        }
    }
}
#endif
