using UnityEngine;
using System.IO;
using System;

namespace MyToolz.Utilities.Logging
{
    public static class LogFileWriter
    {
 #if !UNITY_EDITOR
        private static StreamWriter logWriter;
        private static string logFilePath;
        private static bool initialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitOnLoad()
        {
            if (initialized) return;
            Init();
        }

        private static void Init()
        {
            try
            {
                initialized = true;

                string logDirectory = Path.Combine(Application.dataPath, "Logs");
                if (!Directory.Exists(logDirectory))
                    Directory.CreateDirectory(logDirectory);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                logFilePath = Path.Combine(logDirectory, $"log_{timestamp}.txt");

                logWriter = new StreamWriter(logFilePath, true);
                logWriter.AutoFlush = true;

                Application.logMessageReceived += HandleLog;
                UnityEngine.Debug.Log("===== Log Started: " + DateTime.Now + " =====");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("LogFileWriter init failed: " + e.Message);
            }
        }

        private static void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (logWriter == null) return;

            string logEntry = $"[{DateTime.Now:HH:mm:ss}] {type}: {condition}";
            logWriter.WriteLine(logEntry);

            if (type == LogType.Error || type == LogType.Exception)
            {
                logWriter.WriteLine(stackTrace);
            }
        }

        public static void Shutdown()
        {
            if (!initialized) return;

            Application.logMessageReceived -= HandleLog;

            if (logWriter != null)
            {
                logWriter.WriteLine("===== Log Ended: " + DateTime.Now + " =====");
                logWriter.Close();
                logWriter.Dispose();
                logWriter = null;
            }

            initialized = false;
        }
#endif
    }
}
