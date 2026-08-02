#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace MyToolz.Localization.Editor
{
    [CustomPropertyDrawer(typeof(LocalizationKeyAttribute))]
    public sealed class LocalizationKeyDrawer : LocalizationDropdownDrawer
    {
        protected override string DatabaseFieldName => ((LocalizationKeyAttribute)attribute).DatabaseField;

        protected override string EmptyLabel => "<Select key>";

        protected override string MissingTooltip => "This key is not present in the current CSV.";

        protected override IReadOnlyList<string> GetOptions(LocalizationDatabaseSO database)
        {
            database.Reload();
            return database.Keys;
        }
    }
}
#endif
