using UnityEngine;
using System.IO;

namespace MyToolz.Utilities.AutoLogger
{
    public class LogFileWriterPreferences : ScriptableObject
    {
        private const string ResourcePath = "LogFileWriterPreferences";

        [Header("Activation")]
        [Tooltip("Master switch. When off, the logger never hooks the console or touches the disk.")]
        public bool enabled = true;

        [Tooltip("Also run in non-development (release) player builds. Development builds always follow the master switch.")]
        public bool enabledInReleaseBuilds = true;

        [Tooltip("Run in WebGL builds. The browser has no thread pool and every flush is an IndexedDB write, so this is off by default.")]
        public bool enabledOnWebGL;

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

        [Tooltip("Maximum number of distinct messages the frequency statistics remember. Further distinct messages are counted as 'other'.")]
        [Min(16)]
        public int maxTrackedMessages = 512;

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

        /// <summary>Whether the logger should run in the current player, given the switches above.</summary>
        public bool IsActiveForThisPlayer()
        {
            if (!enabled)
                return false;

            if (!enabledInReleaseBuilds && !UnityEngine.Debug.isDebugBuild)
                return false;

            if (!enabledOnWebGL && Application.platform == RuntimePlatform.WebGLPlayer)
                return false;

            return true;
        }

        public string ResolveLogDirectory()
        {
            if (!string.IsNullOrWhiteSpace(customLogDirectory))
                return customLogDirectory;
            return Path.Combine(Application.persistentDataPath, "Logs");
        }
    }
}
