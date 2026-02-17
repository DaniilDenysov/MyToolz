#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MyToolz.CI
{
    public static class CIEntryPoint
    {
        /// <summary>
        /// Wrapper for CI that guarantees:
        /// - Full exception + stack trace is written to OUT_DIR/Exporter-ci.log
        /// - Non-zero exit code on failure
        /// </summary>
        public static void ExportUnityPackages()
        {
            // Resolve output directory early (so logging works even if exporter fails)
            var outDir = GetArg("-mytoolzOutDir") ?? Environment.GetEnvironmentVariable("OUT_DIR") ?? "build_output";
            try { Directory.CreateDirectory(outDir); } catch { /* ignore */ }

            var exporterLogPath = Path.Combine(outDir, "Exporter-ci.log");
            using var logWriter = new StreamWriter(exporterLogPath, append: false);
            void Log(string msg)
            {
                var line = $"[{DateTime.UtcNow:O}] {msg}";
                logWriter.WriteLine(line);
                logWriter.Flush();
                Debug.Log(line);
            }
            void LogErr(string msg)
            {
                var line = $"[{DateTime.UtcNow:O}] ERROR: {msg}";
                logWriter.WriteLine(line);
                logWriter.Flush();
                Debug.LogError(line);
            }

            Log("CIEntryPoint.ExportUnityPackages started.");
            Log($"UnityVersion={Application.unityVersion}, Platform={Application.platform}");

            try
            {
                // Force full stack traces even if Unity defaults differ
                Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
                Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
            }
            catch { /* ignore */ }

            try
            {
                // Delegate to your real exporter method.
                // IMPORTANT: keep using the updated exporter that understands -mytoolzModulesFile etc.
                Log("Invoking MyToolzUnityPackageExporter.ExportPerModule_WithMergedDeps...");
                MyToolzUnityPackageExporter.ExportPerModule_WithMergedDeps();

                Log("Export completed successfully.");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                LogErr("Export failed with exception:");
                LogErr(ex.ToString()); // includes stack trace
                // Also write a marker so you can grep quickly
                logWriter.WriteLine("CI_EXPORT_FAILED=1");
                logWriter.Flush();

                // Make Unity process fail loudly
                EditorApplication.Exit(1);
            }
        }

        private static string GetArg(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == key)
                    return args[i + 1];
            }
            return null;
        }
    }
}
#endif
