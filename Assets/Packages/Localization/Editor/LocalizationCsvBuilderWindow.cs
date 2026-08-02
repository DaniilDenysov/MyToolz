#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MyToolz.Localization.Editor
{
    public class LocalizationCsvBuilderWindow : EditorWindow
    {
        [Serializable]
        private class KeyRow
        {
            public string Key;
            public List<string> Values = new List<string>();
        }

        private const float CellWidth = 130f;
        private const float HeaderWidth = 150f;

        private static readonly string[] TemplateKeys = { "title", "play", "settings", "exit" };

        private static readonly char[] Windows1251High =
        {
            '\u0402', '\u0403', '\u201A', '\u0453', '\u201E', '\u2026', '\u2020', '\u2021',
            '\u20AC', '\u2030', '\u0409', '\u2039', '\u040A', '\u040C', '\u040B', '\u040F',
            '\u0452', '\u2018', '\u2019', '\u201C', '\u201D', '\u2022', '\u2013', '\u2014',
            '\uFFFD', '\u2122', '\u0459', '\u203A', '\u045A', '\u045C', '\u045B', '\u045F',
            '\u00A0', '\u040E', '\u045E', '\u0408', '\u00A4', '\u0490', '\u00A6', '\u00A7',
            '\u0401', '\u00A9', '\u0404', '\u00AB', '\u00AC', '\u00AD', '\u00AE', '\u0407',
            '\u00B0', '\u00B1', '\u0406', '\u0456', '\u0491', '\u00B5', '\u00B6', '\u00B7',
            '\u0451', '\u2116', '\u0454', '\u00BB', '\u0458', '\u0405', '\u0455', '\u0457',
        };

        [SerializeField] private List<string> languages = new List<string>();
        [SerializeField] private List<KeyRow> rows = new List<KeyRow>();
        [SerializeField] private LocalizationCsvOrientation orientation = LocalizationCsvOrientation.KeysAsRows;
        [SerializeField] private LocalizationDatabaseSO database;
        [SerializeField] private TextAsset sourceCsv;
        [SerializeField] private DefaultAsset targetFolder;
        [SerializeField] private Vector2 gridScroll;

        private Action pendingAction;

        [MenuItem("Tools/MyToolz/Localization/CSV Builder")]
        public static void Open() => GetWindow<LocalizationCsvBuilderWindow>("Localization CSV");

        private void OnEnable() => minSize = new Vector2(520f, 420f);

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space();
            DrawSettings();
            EditorGUILayout.Space();
            DrawOrientationInfo();
            DrawGrid();
            EditorGUILayout.Space();
            DrawUtilities();

            RunPendingAction();
        }

        private void RunPendingAction()
        {
            if (pendingAction == null)
            {
                return;
            }

            Action action = pendingAction;
            pendingAction = null;

            try
            {
                action();
            }
            catch (IOException ioException)
            {
                Debug.LogException(ioException);
                ShowNotification(new GUIContent("File is locked — close it in Excel and retry"));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ShowNotification(new GUIContent("Action failed — see the Console"));
            }

            Repaint();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("New Template", EditorStyles.toolbarButton))
            {
                pendingAction = NewTemplate;
            }

            if (GUILayout.Button("Load", EditorStyles.toolbarButton))
            {
                pendingAction = Load;
            }

            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
            {
                pendingAction = Save;
            }

            if (GUILayout.Button("Detect Orientation", EditorStyles.toolbarButton))
            {
                pendingAction = DetectOrientation;
            }

            if (GUILayout.Button("Swap Orientation", EditorStyles.toolbarButton))
            {
                pendingAction = SwapOrientation;
            }

            if (GUILayout.Button("Create SOs", EditorStyles.toolbarButton))
            {
                pendingAction = GenerateAssets;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSettings()
        {
            orientation = (LocalizationCsvOrientation)EditorGUILayout.EnumPopup("Orientation", orientation);
            database = (LocalizationDatabaseSO)EditorGUILayout.ObjectField("Database (optional)", database, typeof(LocalizationDatabaseSO), false);
            sourceCsv = (TextAsset)EditorGUILayout.ObjectField("CSV (optional)", sourceCsv, typeof(TextAsset), false);
            targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("Target Folder (optional)", targetFolder, typeof(DefaultAsset), false);

            if (GUILayout.Button("Fetch Languages From Project"))
            {
                FetchLanguagesFromProject();
            }
        }

        private void DrawOrientationInfo()
        {
            string info = orientation == LocalizationCsvOrientation.KeysAsRows
                ? "Rows = keys (words), Columns = languages."
                : "Rows = languages, Columns = keys (words).";

            EditorGUILayout.HelpBox(info, MessageType.Info);
        }

        private void DrawGrid()
        {
            EnsureRectangular();

            gridScroll = EditorGUILayout.BeginScrollView(gridScroll, GUILayout.ExpandHeight(true));

            if (orientation == LocalizationCsvOrientation.KeysAsRows)
            {
                DrawGridKeysAsRows();
            }
            else
            {
                DrawGridLanguagesAsRows();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Language", GUILayout.Width(140f)))
            {
                AddLanguage("New Language");
            }

            if (GUILayout.Button("Add Key", GUILayout.Width(140f)))
            {
                AddKey("new_key");
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGridKeysAsRows()
        {
            int removeLanguage = -1;
            int removeKey = -1;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Key \\ Language", EditorStyles.boldLabel, GUILayout.Width(HeaderWidth));

            for (int li = 0; li < languages.Count; li++)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(CellWidth));
                languages[li] = EditorGUILayout.TextField(languages[li]);
                if (GUILayout.Button("Remove", EditorStyles.miniButton))
                {
                    removeLanguage = li;
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();

            for (int r = 0; r < rows.Count; r++)
            {
                EditorGUILayout.BeginHorizontal();

                rows[r].Key = EditorGUILayout.TextField(rows[r].Key, GUILayout.Width(HeaderWidth - 22f));
                if (GUILayout.Button("x", GUILayout.Width(20f)))
                {
                    removeKey = r;
                }

                for (int li = 0; li < languages.Count; li++)
                {
                    rows[r].Values[li] = EditorGUILayout.TextField(rows[r].Values[li], GUILayout.Width(CellWidth));
                }

                EditorGUILayout.EndHorizontal();
            }

            if (removeLanguage >= 0)
            {
                RemoveLanguage(removeLanguage);
            }

            if (removeKey >= 0)
            {
                RemoveKey(removeKey);
            }
        }

        private void DrawGridLanguagesAsRows()
        {
            int removeLanguage = -1;
            int removeKey = -1;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Language \\ Key", EditorStyles.boldLabel, GUILayout.Width(HeaderWidth));

            for (int r = 0; r < rows.Count; r++)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(CellWidth));
                rows[r].Key = EditorGUILayout.TextField(rows[r].Key);
                if (GUILayout.Button("Remove", EditorStyles.miniButton))
                {
                    removeKey = r;
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();

            for (int li = 0; li < languages.Count; li++)
            {
                EditorGUILayout.BeginHorizontal();

                languages[li] = EditorGUILayout.TextField(languages[li], GUILayout.Width(HeaderWidth - 22f));
                if (GUILayout.Button("x", GUILayout.Width(20f)))
                {
                    removeLanguage = li;
                }

                for (int r = 0; r < rows.Count; r++)
                {
                    rows[r].Values[li] = EditorGUILayout.TextField(rows[r].Values[li], GUILayout.Width(CellWidth));
                }

                EditorGUILayout.EndHorizontal();
            }

            if (removeKey >= 0)
            {
                RemoveKey(removeKey);
            }

            if (removeLanguage >= 0)
            {
                RemoveLanguage(removeLanguage);
            }
        }

        private void DrawUtilities()
        {
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Sort Keys")) SortKeys();
            if (GUILayout.Button("Dedupe Keys")) DedupeKeys();
            if (GUILayout.Button("Remove Empty Keys")) RemoveEmptyKeys();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fill Empty With Key")) FillEmptyWithKey();
            if (GUILayout.Button("Trim All")) TrimAll();
            if (GUILayout.Button("Clear") && EditorUtility.DisplayDialog("Clear", "Clear the whole grid?", "Clear", "Cancel")) Clear();
            EditorGUILayout.EndHorizontal();
        }

        private void NewTemplate()
        {
            languages = new List<string>();
            rows = new List<KeyRow>();

            FetchLanguagesFromProject();

            if (languages.Count == 0)
            {
                languages.Add("English");
                languages.Add("Ukrainian");
            }

            foreach (string key in TemplateKeys)
            {
                AddKey(key);
            }
        }

        private void FetchLanguagesFromProject()
        {
            string[] guids = AssetDatabase.FindAssets("t:LocalizationLanguageSO");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LocalizationLanguageSO language = AssetDatabase.LoadAssetAtPath<LocalizationLanguageSO>(path);

                if (language != null && !languages.Contains(language.DisplayName))
                {
                    AddLanguage(language.DisplayName);
                }
            }
        }

        private void SwapOrientation()
        {
            orientation = orientation == LocalizationCsvOrientation.KeysAsRows
                ? LocalizationCsvOrientation.LanguagesAsRows
                : LocalizationCsvOrientation.KeysAsRows;
        }

        private void Load()
        {
            TextAsset asset = sourceCsv;
            LocalizationCsvOrientation orient = orientation;

            if (asset == null && database != null)
            {
                asset = database.Csv;
                orient = database.Orientation;
            }

            string text;
            if (asset != null)
            {
                sourceCsv = asset;
                text = ReadCsvText(AssetFullPath(asset));
            }
            else
            {
                string absolute = EditorUtility.OpenFilePanel("Load CSV", Application.dataPath, "csv");
                if (string.IsNullOrEmpty(absolute))
                {
                    return;
                }

                text = ReadCsvText(absolute);
            }

            if (LocalizationOrientationDetector.TryDetect(LocalizationCsvParser.Parse(text), BuildProjectKnownLanguages(), out LocalizationCsvOrientation detected))
            {
                orient = detected;
            }

            LoadFromCsv(text, orient);
        }

        private void DetectOrientation()
        {
            string text = ResolveSourceText();
            if (string.IsNullOrEmpty(text))
            {
                ShowNotification(new GUIContent("Assign a CSV or Database to inspect"));
                return;
            }

            if (LocalizationOrientationDetector.TryDetect(LocalizationCsvParser.Parse(text), BuildProjectKnownLanguages(), out LocalizationCsvOrientation detected))
            {
                orientation = detected;
                ShowNotification(new GUIContent($"Detected: {detected}"));
            }
            else
            {
                ShowNotification(new GUIContent("Orientation inconclusive"));
            }
        }

        private string ResolveSourceText()
        {
            if (sourceCsv != null)
            {
                return ReadCsvText(AssetFullPath(sourceCsv));
            }

            return database != null && database.Csv != null ? ReadCsvText(AssetFullPath(database.Csv)) : null;
        }

        private static string AssetFullPath(TextAsset asset) => Path.GetFullPath(AssetDatabase.GetAssetPath(asset));

        private static string ReadCsvText(string fullPath)
        {
            byte[] bytes = ReadAllBytesShared(fullPath);

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return new UTF8Encoding(false).GetString(bytes, 3, bytes.Length - 3);
            }

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
            }

            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
            }

            try
            {
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return ReadAnsi(bytes);
            }
        }

        private static string ReadAnsi(byte[] bytes)
        {
            char[] chars = new char[bytes.Length];

            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                if (b < 0x80)
                {
                    chars[i] = (char)b;
                }
                else if (b < 0xC0)
                {
                    chars[i] = Windows1251High[b - 0x80];
                }
                else
                {
                    chars[i] = (char)(0x0410 + (b - 0xC0));
                }
            }

            return new string(chars);
        }

        private static byte[] ReadAllBytesShared(string fullPath)
        {
            using (FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] buffer = new byte[stream.Length];
                int offset = 0;

                while (offset < buffer.Length)
                {
                    int read = stream.Read(buffer, offset, buffer.Length - offset);
                    if (read <= 0)
                    {
                        break;
                    }

                    offset += read;
                }

                return buffer;
            }
        }

        private void GenerateAssets()
        {
            TextAsset csvAsset = sourceCsv != null ? sourceCsv : (database != null ? database.Csv : null);
            if (csvAsset == null)
            {
                ShowNotification(new GUIContent("Assign a CSV (or a database with a CSV) first"));
                return;
            }

            sourceCsv = csvAsset;
            Load();
            EnsureRectangular();

            if (languages.Count == 0 && rows.Count == 0)
            {
                ShowNotification(new GUIContent("The CSV appears to be empty"));
                return;
            }

            string folder = ResolveTargetFolder(csvAsset);
            if (string.IsNullOrEmpty(folder))
            {
                ShowNotification(new GUIContent("Invalid target folder"));
                return;
            }

            int keyCount = 0;
            foreach (KeyRow row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.Key))
                {
                    keyCount++;
                }
            }

            bool confirmed = EditorUtility.DisplayDialog("Create Localization SOs",
                $"Create/update a database, {languages.Count} language asset(s) and {keyCount} binding asset(s) in \"{folder}\"?\n\nExisting matching assets are reused, not duplicated.",
                "Create", "Cancel");

            if (!confirmed)
            {
                return;
            }

            string languageFolder = EnsureSubFolder(folder, "Languages");
            string bindingFolder = EnsureSubFolder(folder, "Bindings");

            List<LocalizationLanguageSO> languageAssets = new List<LocalizationLanguageSO>();
            foreach (string languageName in languages)
            {
                languageAssets.Add(FindOrCreateLanguage(languageName, languageFolder));
            }

            LocalizationDatabaseSO db = database != null ? database : FindDatabaseAsset(csvAsset);
            if (db == null)
            {
                db = CreateInstance<LocalizationDatabaseSO>();
                AssetDatabase.CreateAsset(db, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{csvAsset.name}Database.asset"));
            }

            ConfigureDatabase(db, csvAsset, languageAssets);
            database = db;

            foreach (LocalizationLanguageSO language in languageAssets)
            {
                LinkLanguageToDatabase(language, db);
            }

            HashSet<string> processedKeys = new HashSet<string>();
            foreach (KeyRow row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Key) || !processedKeys.Add(row.Key))
                {
                    continue;
                }

                FindOrCreateBinding(row.Key, db, bindingFolder);
            }

            db.Reload();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ShowNotification(new GUIContent($"Created SOs in {folder}"));
        }

        private string ResolveTargetFolder(TextAsset csv)
        {
            if (targetFolder != null)
            {
                string folderPath = AssetDatabase.GetAssetPath(targetFolder);
                if (AssetDatabase.IsValidFolder(folderPath))
                {
                    return folderPath;
                }
            }

            string csvPath = AssetDatabase.GetAssetPath(csv);
            string directory = Path.GetDirectoryName(csvPath);
            return string.IsNullOrEmpty(directory) ? "Assets" : directory.Replace('\\', '/');
        }

        private static string EnsureSubFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }

            return path;
        }

        private void ConfigureDatabase(LocalizationDatabaseSO db, TextAsset csv, List<LocalizationLanguageSO> languageAssets)
        {
            SerializedObject serialized = new SerializedObject(db);
            serialized.FindProperty("csv").objectReferenceValue = csv;
            serialized.FindProperty("orientation").enumValueIndex = (int)orientation;

            SerializedProperty languagesProperty = serialized.FindProperty("languages");
            languagesProperty.arraySize = languageAssets.Count;
            for (int i = 0; i < languageAssets.Count; i++)
            {
                languagesProperty.GetArrayElementAtIndex(i).objectReferenceValue = languageAssets[i];
            }

            SerializedProperty defaultLanguage = serialized.FindProperty("defaultLanguage");
            if (defaultLanguage.objectReferenceValue == null && languageAssets.Count > 0)
            {
                defaultLanguage.objectReferenceValue = languageAssets[0];
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(db);
        }

        private static void LinkLanguageToDatabase(LocalizationLanguageSO language, LocalizationDatabaseSO db)
        {
            if (language == null || language.Database == db)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(language);
            serialized.FindProperty("database").objectReferenceValue = db;
            serialized.ApplyModifiedProperties();
        }

        private static LocalizationLanguageSO FindOrCreateLanguage(string languageName, string folder)
        {
            LocalizationLanguageSO existing = FindLanguageAsset(languageName);
            if (existing != null)
            {
                return existing;
            }

            LocalizationLanguageSO language = CreateInstance<LocalizationLanguageSO>();
            AssetDatabase.CreateAsset(language, AssetDatabase.GenerateUniqueAssetPath($"{folder}/Language_{Sanitize(languageName)}.asset"));

            SerializedObject serialized = new SerializedObject(language);
            serialized.FindProperty("displayName").stringValue = languageName;
            serialized.FindProperty("code").stringValue = languageName;
            serialized.ApplyModifiedProperties();
            return language;
        }

        private static LocalizationBindingSO FindOrCreateBinding(string key, LocalizationDatabaseSO db, string folder)
        {
            LocalizationBindingSO existing = FindBindingAsset(key, db);
            if (existing != null)
            {
                return existing;
            }

            LocalizationBindingSO binding = CreateInstance<LocalizationBindingSO>();
            AssetDatabase.CreateAsset(binding, AssetDatabase.GenerateUniqueAssetPath($"{folder}/Binding_{Sanitize(key)}.asset"));

            SerializedObject serialized = new SerializedObject(binding);
            serialized.FindProperty("database").objectReferenceValue = db;
            serialized.FindProperty("key").stringValue = key;
            serialized.ApplyModifiedProperties();
            return binding;
        }

        private static LocalizationLanguageSO FindLanguageAsset(string languageName)
        {
            string target = languageName?.Trim();
            if (string.IsNullOrEmpty(target))
            {
                return null;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:LocalizationLanguageSO"))
            {
                LocalizationLanguageSO language = AssetDatabase.LoadAssetAtPath<LocalizationLanguageSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (language == null)
                {
                    continue;
                }

                if (NameMatches(language.Code, target) || NameMatches(language.DisplayName, target) || NameMatches(language.name, target))
                {
                    return language;
                }
            }

            return null;
        }

        private static LocalizationBindingSO FindBindingAsset(string key, LocalizationDatabaseSO db)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:LocalizationBindingSO"))
            {
                LocalizationBindingSO binding = AssetDatabase.LoadAssetAtPath<LocalizationBindingSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (binding != null && binding.Database == db && binding.Key == key)
                {
                    return binding;
                }
            }

            return null;
        }

        private static LocalizationDatabaseSO FindDatabaseAsset(TextAsset csv)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:LocalizationDatabaseSO"))
            {
                LocalizationDatabaseSO db = AssetDatabase.LoadAssetAtPath<LocalizationDatabaseSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (db != null && db.Csv == csv)
                {
                    return db;
                }
            }

            return null;
        }

        private static bool NameMatches(string value, string target)
            => !string.IsNullOrEmpty(value) && string.Equals(value.Trim(), target, StringComparison.OrdinalIgnoreCase);

        private static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Unnamed";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }

            return builder.ToString();
        }

        private static HashSet<string> BuildProjectKnownLanguages()
        {
            HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string guid in AssetDatabase.FindAssets("t:LocalizationLanguageSO"))
            {
                LocalizationLanguageSO language = AssetDatabase.LoadAssetAtPath<LocalizationLanguageSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (language == null)
                {
                    continue;
                }

                AddKnown(set, language.Code);
                AddKnown(set, language.DisplayName);
                AddKnown(set, language.name);
            }

            return set;
        }

        private static void AddKnown(HashSet<string> set, string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                set.Add(name.Trim());
            }
        }

        private void LoadFromCsv(string text, LocalizationCsvOrientation orient)
        {
            languages.Clear();
            rows.Clear();

            List<string[]> grid = LocalizationCsvParser.Parse(text);
            if (grid.Count == 0)
            {
                orientation = orient;
                return;
            }

            string[] headerRow = grid[0];

            if (orient == LocalizationCsvOrientation.KeysAsRows)
            {
                for (int c = 1; c < headerRow.Length; c++)
                {
                    languages.Add(headerRow[c]);
                }

                for (int i = 1; i < grid.Count; i++)
                {
                    string[] row = grid[i];
                    if (row.Length < 1 || string.IsNullOrEmpty(row[0]))
                    {
                        continue;
                    }

                    KeyRow keyRow = new KeyRow { Key = row[0], Values = new List<string>() };
                    for (int c = 0; c < languages.Count; c++)
                    {
                        keyRow.Values.Add(c + 1 < row.Length ? row[c + 1] : string.Empty);
                    }

                    rows.Add(keyRow);
                }
            }
            else
            {
                for (int c = 1; c < headerRow.Length; c++)
                {
                    rows.Add(new KeyRow { Key = headerRow[c], Values = new List<string>() });
                }

                for (int i = 1; i < grid.Count; i++)
                {
                    string[] row = grid[i];
                    if (row.Length < 1 || string.IsNullOrEmpty(row[0]))
                    {
                        continue;
                    }

                    languages.Add(row[0]);
                    for (int k = 0; k < rows.Count; k++)
                    {
                        rows[k].Values.Add(k + 1 < row.Length ? row[k + 1] : string.Empty);
                    }
                }
            }

            orientation = orient;
            EnsureRectangular();
        }

        private void Save()
        {
            string path = ResolveSavePath();
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            File.WriteAllText(Path.GetFullPath(path), BuildCsv(), new UTF8Encoding(true));
            AssetDatabase.ImportAsset(path);

            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            sourceCsv = asset;

            if (database != null)
            {
                SerializedObject serialized = new SerializedObject(database);
                serialized.FindProperty("csv").objectReferenceValue = asset;
                serialized.FindProperty("orientation").enumValueIndex = (int)orientation;
                serialized.ApplyModifiedProperties();
                database.Reload();
                EditorUtility.SetDirty(database);
            }

            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent($"Saved {Path.GetFileName(path)}"));
        }

        private string ResolveSavePath()
        {
            if (database != null && database.Csv != null)
            {
                return AssetDatabase.GetAssetPath(database.Csv);
            }

            if (sourceCsv != null)
            {
                return AssetDatabase.GetAssetPath(sourceCsv);
            }

            return EditorUtility.SaveFilePanelInProject("Save Localization CSV", "Localizations", "csv", "Choose where to save the CSV");
        }

        private string BuildCsv()
        {
            EnsureRectangular();
            StringBuilder builder = new StringBuilder();

            if (orientation == LocalizationCsvOrientation.KeysAsRows)
            {
                List<string> headerCells = new List<string> { "key" };
                headerCells.AddRange(languages);
                builder.Append(JoinRow(headerCells)).Append('\n');

                foreach (KeyRow row in rows)
                {
                    List<string> cells = new List<string> { row.Key };
                    cells.AddRange(row.Values);
                    builder.Append(JoinRow(cells)).Append('\n');
                }
            }
            else
            {
                List<string> headerCells = new List<string> { "language" };
                foreach (KeyRow row in rows)
                {
                    headerCells.Add(row.Key);
                }
                builder.Append(JoinRow(headerCells)).Append('\n');

                for (int li = 0; li < languages.Count; li++)
                {
                    List<string> cells = new List<string> { languages[li] };
                    foreach (KeyRow row in rows)
                    {
                        cells.Add(li < row.Values.Count ? row.Values[li] : string.Empty);
                    }
                    builder.Append(JoinRow(cells)).Append('\n');
                }
            }

            return builder.ToString();
        }

        private static string JoinRow(List<string> cells)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < cells.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }
                builder.Append(Escape(cells[i]));
            }
            return builder.ToString();
        }

        private static string Escape(string field)
        {
            field ??= string.Empty;

            bool needsQuotes = field.IndexOf(',') >= 0 || field.IndexOf('"') >= 0
                || field.IndexOf('\n') >= 0 || field.IndexOf('\r') >= 0;

            return needsQuotes ? "\"" + field.Replace("\"", "\"\"") + "\"" : field;
        }

        private void AddLanguage(string name)
        {
            languages.Add(name ?? string.Empty);
            foreach (KeyRow row in rows)
            {
                row.Values.Add(string.Empty);
            }
        }

        private void RemoveLanguage(int index)
        {
            if (index < 0 || index >= languages.Count)
            {
                return;
            }

            languages.RemoveAt(index);
            foreach (KeyRow row in rows)
            {
                if (index < row.Values.Count)
                {
                    row.Values.RemoveAt(index);
                }
            }
        }

        private void AddKey(string key)
        {
            KeyRow row = new KeyRow { Key = key ?? string.Empty, Values = new List<string>() };
            for (int i = 0; i < languages.Count; i++)
            {
                row.Values.Add(string.Empty);
            }
            rows.Add(row);
        }

        private void RemoveKey(int index)
        {
            if (index >= 0 && index < rows.Count)
            {
                rows.RemoveAt(index);
            }
        }

        private void EnsureRectangular()
        {
            foreach (KeyRow row in rows)
            {
                row.Values ??= new List<string>();

                while (row.Values.Count < languages.Count)
                {
                    row.Values.Add(string.Empty);
                }

                while (row.Values.Count > languages.Count)
                {
                    row.Values.RemoveAt(row.Values.Count - 1);
                }
            }
        }

        private void SortKeys() => rows.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));

        private void DedupeKeys()
        {
            HashSet<string> seen = new HashSet<string>();
            rows.RemoveAll(row => !seen.Add(row.Key));
        }

        private void RemoveEmptyKeys() => rows.RemoveAll(row => string.IsNullOrWhiteSpace(row.Key));

        private void FillEmptyWithKey()
        {
            foreach (KeyRow row in rows)
            {
                for (int i = 0; i < row.Values.Count; i++)
                {
                    if (string.IsNullOrEmpty(row.Values[i]))
                    {
                        row.Values[i] = row.Key;
                    }
                }
            }
        }

        private void TrimAll()
        {
            for (int i = 0; i < languages.Count; i++)
            {
                languages[i] = languages[i]?.Trim();
            }

            foreach (KeyRow row in rows)
            {
                row.Key = row.Key?.Trim();
                for (int i = 0; i < row.Values.Count; i++)
                {
                    row.Values[i] = row.Values[i]?.Trim();
                }
            }
        }

        private void Clear()
        {
            languages.Clear();
            rows.Clear();
        }
    }
}
#endif
