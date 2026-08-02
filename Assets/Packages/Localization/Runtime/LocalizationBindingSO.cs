using MyToolz.EditorToolz;
using UnityEngine;

namespace MyToolz.Localization
{
    [CreateAssetMenu(fileName = "LocalizationBindingSO", menuName = "MyToolz/Localization/Binding")]
    public class LocalizationBindingSO : ScriptableObject
    {
        [SerializeField, Required] private LocalizationDatabaseSO database;
        [SerializeField, LocalizationKey(nameof(database))] private string key;

        public LocalizationDatabaseSO Database => database;

        public string Key => key;

        public string Resolve(LocalizationLanguageSO language)
            => database != null && database.TryTranslate(key, language, out string value) ? value : key;
    }
}
