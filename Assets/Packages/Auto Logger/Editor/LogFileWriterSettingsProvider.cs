#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace MyToolz.Utilities.AutoLogger.Editor
{
    public static class LogFileWriterSettingsProvider
    {
        private const string SettingsPath = "Project/LogFileWriter";
        private const string AssetFolder = "Assets/Resources";
        private const string AssetPath = "Assets/Resources/LogFileWriterPreferences.asset";

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new SettingsProvider(SettingsPath, SettingsScope.Project)
            {
                label = "Log File Writer",
                guiHandler = _ => DrawGUI(),
                keywords = new[] { "log", "logger", "logfile", "debug", "autoLogger" }
            };
        }

        private static void DrawGUI()
        {
            var prefs = LoadOrCreate();
            var so = new SerializedObject(prefs);
            so.Update();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Log File Writer", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("Path", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("customLogDirectory"),
                new GUIContent("Custom Log Directory",
                    "Leave empty to use Application.persistentDataPath/Logs"));

            string resolvedDir = prefs.ResolveLogDirectory();
            EditorGUILayout.LabelField("Resolved path:", resolvedDir, EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Open Log Folder", GUILayout.Width(160)))
            {
                if (!Directory.Exists(resolvedDir))
                    Directory.CreateDirectory(resolvedDir);
                EditorUtility.RevealInFinder(resolvedDir);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Retention", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("maxLogFiles"),
                new GUIContent("Max Log Files",
                    "Oldest files are deleted when this limit is exceeded. 0 = unlimited."));

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Features", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("trackSceneChanges"));
            EditorGUILayout.PropertyField(so.FindProperty("trackStatistics"));
            EditorGUILayout.PropertyField(so.FindProperty("trackFps"));

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("FPS Sampling", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledGroupScope(!prefs.trackFps))
            {
                EditorGUILayout.PropertyField(so.FindProperty("fpsSampleIntervalSeconds"),
                    new GUIContent("Sample Interval (s)"));
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(prefs);
        }

        private static LogFileWriterPreferences LoadOrCreate()
        {
            var existing = AssetDatabase.LoadAssetAtPath<LogFileWriterPreferences>(AssetPath);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder(AssetFolder))
                AssetDatabase.CreateFolder("Assets", "Resources");

            var asset = ScriptableObject.CreateInstance<LogFileWriterPreferences>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log($"[LogFileWriter] Created preferences asset at {AssetPath}");
            return asset;
        }
    }
}
#endif
