using System;
using System.Collections.Generic;
using MyToolz.EditorToolz;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.Localization
{
    [CreateAssetMenu(fileName = "LocalizationDatabaseSO", menuName = "MyToolz/Localization/Database")]
    public class LocalizationDatabaseSO : ScriptableObject
    {
        [Header("Source")]
        [SerializeField, Required] private TextAsset csv;

        [Tooltip("KeysAsRows: header row names the languages, each row is a key. " +
                 "LanguagesAsRows: header row names the keys, each row is a language.")]
        [SerializeField] private LocalizationCsvOrientation orientation = LocalizationCsvOrientation.KeysAsRows;

        [Header("Languages")]
        [Tooltip("Languages available in the game. Each is matched to its CSV column/row by Code, display name or asset name; " +
                 "list order is only used as a fallback when the CSV header names no language.")]
        [SerializeField] private List<LocalizationLanguageSO> languages = new();
        [SerializeField] private LocalizationLanguageSO defaultLanguage;

        private readonly Dictionary<string, string[]> table = new();
        private readonly List<string> keys = new();
        private readonly Dictionary<string, int> columnByHeader = new(StringComparer.OrdinalIgnoreCase);
        private string[] header = Array.Empty<string>();
        private bool parsed;
        private bool warnedPositionalFallback;

        public TextAsset Csv => csv;

        public LocalizationCsvOrientation Orientation => orientation;

        public IReadOnlyList<LocalizationLanguageSO> Languages => languages;

        public IReadOnlyList<string> Keys
        {
            get
            {
                EnsureParsed();
                return keys;
            }
        }

        public IReadOnlyList<string> Header
        {
            get
            {
                EnsureParsed();
                return header;
            }
        }

        public LocalizationLanguageSO DefaultLanguage
            => defaultLanguage != null ? defaultLanguage : (languages.Count > 0 ? languages[0] : null);

        public bool Contains(LocalizationLanguageSO language) => IndexOfLanguage(language) >= 0;

        // Languages are identified by Code: another asset instance (e.g. loaded through Addressables)
        // with the same Code is the same language.
        private int IndexOfLanguage(LocalizationLanguageSO language)
        {
            if (language == null)
            {
                return -1;
            }

            for (int i = 0; i < languages.Count; i++)
            {
                LocalizationLanguageSO candidate = languages[i];
                if (candidate == null)
                {
                    continue;
                }

                if (candidate == language || candidate.Code == language.Code)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// CSV column (or row, for LanguagesAsRows) holding <paramref name="language"/>'s translations:
        /// matched by Code, then display name, then asset name against the CSV header. Falls back to
        /// the language's position in <see cref="Languages"/> only when the header names no language.
        /// </summary>
        public int GetColumn(LocalizationLanguageSO language)
        {
            int index = IndexOfLanguage(language);
            if (index < 0)
            {
                return -1;
            }

            EnsureParsed();

            if (TryGetHeaderColumn(language.Code, out int column) ||
                TryGetHeaderColumn(language.DisplayName, out column) ||
                TryGetHeaderColumn(language.name, out column))
            {
                return column;
            }

            if (HeaderNamesAnyLanguage())
            {
                return -1;
            }

            if (!warnedPositionalFallback)
            {
                warnedPositionalFallback = true;
                DebugUtility.LogWarning(this,
                    "The CSV header does not name any configured language, so languages map to columns by list order. " +
                    "Set each language's Code to its CSV column name to make the mapping order-independent.");
            }

            return index;
        }

        private bool TryGetHeaderColumn(string name, out int column)
        {
            column = -1;
            return !string.IsNullOrWhiteSpace(name) && columnByHeader.TryGetValue(name.Trim(), out column);
        }

        private bool HeaderNamesAnyLanguage()
        {
            foreach (LocalizationLanguageSO language in languages)
            {
                if (language != null &&
                    (TryGetHeaderColumn(language.Code, out _) || TryGetHeaderColumn(language.DisplayName, out _) || TryGetHeaderColumn(language.name, out _)))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryTranslate(string key, LocalizationLanguageSO language, out string value)
        {
            value = null;

            if (string.IsNullOrEmpty(key) || language == null)
            {
                return false;
            }

            int column = GetColumn(language);
            if (column < 0)
            {
                return false;
            }

            if (!table.TryGetValue(key, out string[] row) || column >= row.Length)
            {
                return false;
            }

            value = row[column];
            return !string.IsNullOrEmpty(value);
        }

        public void Reload()
        {
            parsed = false;
            EnsureParsed();
        }

        private void EnsureParsed()
        {
            if (parsed)
            {
                return;
            }

            parsed = true;
            table.Clear();
            keys.Clear();
            columnByHeader.Clear();
            header = Array.Empty<string>();
            warnedPositionalFallback = false;

            if (csv == null || string.IsNullOrEmpty(csv.text))
            {
                DebugUtility.LogWarning(this, "No CSV assigned or the file is empty.");
                return;
            }

            List<string[]> rows = LocalizationCsvParser.Parse(csv.text);
            if (rows.Count == 0)
            {
                return;
            }

            if (orientation == LocalizationCsvOrientation.LanguagesAsRows)
            {
                ParseLanguagesAsRows(rows);
            }
            else
            {
                ParseKeysAsRows(rows);
            }

            for (int i = 0; i < header.Length; i++)
            {
                string name = header[i]?.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                if (!columnByHeader.TryAdd(name, i))
                {
                    DebugUtility.LogWarning(this, $"CSV names the language \"{name}\" more than once; the first occurrence is used.");
                }
            }

            ValidateLanguages();
        }

        private void ValidateLanguages()
        {
            HashSet<string> codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (LocalizationLanguageSO language in languages)
            {
                if (language == null)
                {
                    DebugUtility.LogWarning(this, "The language list contains an empty entry.");
                    continue;
                }

                if (!codes.Add(language.Code))
                {
                    DebugUtility.LogError(this,
                        $"Language code \"{language.Code}\" is used by more than one language. Codes identify languages in saves and CSV columns and must be unique.");
                }
            }
        }

        private void ParseKeysAsRows(List<string[]> rows)
        {
            string[] headerRow = rows[0];
            if (headerRow.Length > 1)
            {
                header = new string[headerRow.Length - 1];
                Array.Copy(headerRow, 1, header, 0, header.Length);
            }

            for (int i = 1; i < rows.Count; i++)
            {
                string[] row = rows[i];
                if (row.Length < 2 || string.IsNullOrEmpty(row[0]))
                {
                    continue;
                }

                if (table.ContainsKey(row[0]))
                {
                    DebugUtility.LogWarning(this, $"Duplicate localization key \"{row[0]}\" on CSV row {i + 1}; the first definition is kept.");
                    continue;
                }

                string[] values = new string[row.Length - 1];
                Array.Copy(row, 1, values, 0, values.Length);
                table[row[0]] = values;
                keys.Add(row[0]);
            }
        }

        private void ParseLanguagesAsRows(List<string[]> rows)
        {
            string[] headerRow = rows[0];
            if (headerRow.Length <= 1)
            {
                return;
            }

            List<string[]> languageRows = new List<string[]>();
            for (int i = 1; i < rows.Count; i++)
            {
                string[] row = rows[i];
                if (row.Length >= 1 && !string.IsNullOrEmpty(row[0]))
                {
                    languageRows.Add(row);
                }
            }

            header = new string[languageRows.Count];
            for (int li = 0; li < languageRows.Count; li++)
            {
                header[li] = languageRows[li][0];
            }

            for (int k = 1; k < headerRow.Length; k++)
            {
                string keyName = headerRow[k];
                if (string.IsNullOrEmpty(keyName))
                {
                    continue;
                }

                if (table.ContainsKey(keyName))
                {
                    DebugUtility.LogWarning(this, $"Duplicate localization key \"{keyName}\" in CSV column {k + 1}; the first definition is kept.");
                    continue;
                }

                string[] values = new string[languageRows.Count];
                for (int li = 0; li < languageRows.Count; li++)
                {
                    string[] row = languageRows[li];
                    values[li] = k < row.Length ? row[k] : string.Empty;
                }

                table[keyName] = values;
                keys.Add(keyName);
            }
        }

        [Button("Reload From CSV")]
        private void ReloadFromCsv()
        {
            Reload();

            foreach (LocalizationLanguageSO language in languages)
            {
                if (language != null && GetColumn(language) < 0)
                {
                    DebugUtility.LogWarning(this, $"Language \"{language.Code}\" has no matching CSV column; its texts fall back to the key.");
                }
            }

            DebugUtility.Log(this, $"Loaded {keys.Count} key(s) across {header.Length} language(s).");
        }

#if UNITY_EDITOR
        [Button("Fetch Languages From CSV")]
        private void FetchLanguagesFromCsv()
        {
            Reload();

            if (header.Length == 0)
            {
                DebugUtility.LogWarning(this, "CSV has no language columns to fetch.");
                return;
            }

            LocalizationLanguageSO[] all = LoadAllLanguages();
            List<LocalizationLanguageSO> fetched = new List<LocalizationLanguageSO>(header.Length);
            int matched = 0;

            foreach (string columnName in header)
            {
                LocalizationLanguageSO match = FindLanguage(all, columnName);
                fetched.Add(match);

                if (match != null)
                {
                    matched++;
                    AssignDatabase(match);
                }
                else
                {
                    DebugUtility.LogWarning(this, $"No LocalizationLanguageSO matches CSV column \"{columnName}\".");
                }
            }

            languages = fetched;
            UnityEditor.EditorUtility.SetDirty(this);

            DebugUtility.Log(this, $"Fetched {matched}/{header.Length} language(s) from the CSV header.");
        }

        private void AssignDatabase(LocalizationLanguageSO language)
        {
            if (language.Database == this)
            {
                return;
            }

            UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(language);
            serialized.FindProperty("database").objectReferenceValue = this;
            serialized.ApplyModifiedProperties();
        }

        private static LocalizationLanguageSO[] LoadAllLanguages()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:LocalizationLanguageSO");
            LocalizationLanguageSO[] result = new LocalizationLanguageSO[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                result[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<LocalizationLanguageSO>(path);
            }

            return result;
        }

        private static LocalizationLanguageSO FindLanguage(LocalizationLanguageSO[] all, string columnName)
        {
            string target = columnName?.Trim();
            if (string.IsNullOrEmpty(target))
            {
                return null;
            }

            foreach (LocalizationLanguageSO language in all)
            {
                if (language == null)
                {
                    continue;
                }

                if (Matches(language.Code, target) || Matches(language.DisplayName, target) || Matches(language.name, target))
                {
                    return language;
                }
            }

            return null;
        }

        private static bool Matches(string value, string target)
            => !string.IsNullOrEmpty(value) && string.Equals(value.Trim(), target, StringComparison.OrdinalIgnoreCase);

        [Button("Detect Orientation")]
        private void DetectOrientation()
        {
            if (csv == null || string.IsNullOrEmpty(csv.text))
            {
                DebugUtility.LogWarning(this, "No CSV assigned or the file is empty.");
                return;
            }

            List<string[]> rows = LocalizationCsvParser.Parse(csv.text);
            HashSet<string> known = BuildKnownLanguageNames(LoadAllLanguages());

            if (LocalizationOrientationDetector.TryDetect(rows, known, out LocalizationCsvOrientation detected))
            {
                orientation = detected;
                UnityEditor.EditorUtility.SetDirty(this);
                Reload();
                DebugUtility.Log(this, $"Detected orientation: {detected}.");
            }
            else
            {
                DebugUtility.LogWarning(this,
                    "Could not detect orientation — no CSV cell matched a known language name. Create the LocalizationLanguageSO assets first.");
            }
        }

        private static HashSet<string> BuildKnownLanguageNames(LocalizationLanguageSO[] all)
        {
            HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (LocalizationLanguageSO language in all)
            {
                if (language == null)
                {
                    continue;
                }

                AddName(set, language.Code);
                AddName(set, language.DisplayName);
                AddName(set, language.name);
            }

            return set;
        }

        private static void AddName(HashSet<string> set, string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                set.Add(name.Trim());
            }
        }
#endif
    }
}
