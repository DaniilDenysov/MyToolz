using UnityEngine;
using System.IO;

namespace MyToolz.Utilities.AutoLogger
{
    public class LogFileWriterPreferences : ScriptableObject
    {
        private const string ResourcePath = "LogFileWriterPreferences";

        [Header("Path")]
        [Tooltip("Leave empty to use Application.persistentDataPath/Logs")]
        public string customLogDirectory = "";

        [Header("Retention")]
        [Tooltip("Maximum number of log files to keep. Oldest are deleted first. 0 = unlimited.")]
        public int maxLogFiles = 10;

        [Header("Features")]
        public bool trackSceneChanges = true;
        public bool trackStatistics = true;
        public bool trackFps = true;

        [Header("FPS Sampling")]
        [Tooltip("How often FPS is sampled, in seconds.")]
        [Range(0.1f, 5f)]
        public float fpsSampleIntervalSeconds = 1f;

        private static LogFileWriterPreferences instance;

        public static LogFileWriterPreferences Load()
        {
            if (instance != null) return instance;
            instance = Resources.Load<LogFileWriterPreferences>(ResourcePath);
            if (instance == null)
            {
                instance = CreateInstance<LogFileWriterPreferences>();
                UnityEngine.Debug.LogWarning("[LogFileWriter] No LogFileWriterPreferences asset found in Resources. Using defaults.");
            }
            return instance;
        }

        public string ResolveLogDirectory()
        {
            if (!string.IsNullOrWhiteSpace(customLogDirectory))
                return customLogDirectory;
            return Path.Combine(Application.persistentDataPath, "Logs");
        }
    }
}
