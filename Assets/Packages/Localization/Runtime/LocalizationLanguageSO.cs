using TMPro;
using UnityEngine;

namespace MyToolz.Localization
{
    [CreateAssetMenu(fileName = "LocalizationLanguageSO", menuName = "MyToolz/Localization/Language")]
    public class LocalizationLanguageSO : ScriptableObject
    {
        [Header("Source")]
        [Tooltip("Database whose CSV language columns fill the Code dropdown. " +
                 "Leave empty to use the only LocalizationDatabaseSO in the project.")]
        [SerializeField] private LocalizationDatabaseSO database;

        [Header("Identity")]
        [SerializeField] private string displayName;
        [Tooltip("Stable id used to persist the selected language and matched against the CSV. " +
                 "Pick it from the database's language columns. Defaults to the asset name.")]
        [SerializeField, LocalizationLanguage] private string code;

        [Header("Presentation")]
        [SerializeField] private Sprite flag;
        [Tooltip("Optional font swapped in for this language (e.g. a CJK-capable font).")]
        [SerializeField] private TMP_FontAsset font;

        public LocalizationDatabaseSO Database => database;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        public string Code => string.IsNullOrEmpty(code) ? name : code;

        public Sprite Flag => flag;

        public TMP_FontAsset Font => font;
    }
}
