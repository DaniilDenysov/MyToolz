#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MyToolz.CI
{
    public static class CIEntryPoint
    {
        public static void ExportWholeRepoUnityPackage()
        {
            var outDir = GetArg("-mytoolzOutDir") ?? "build_output";
            var packageName = GetArg("-mytoolzPackageName") ?? $"MyToolz-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

            Directory.CreateDirectory(outDir);

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

            try
            {
                Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
                Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
            }
            catch { /* ignore */ }

            try
            {
                Log("ExportWholeRepoUnityPackage started");
                Log($"Unity={Application.unityVersion}, Platform={Application.platform}");
                Log($"outDir={outDir}");
                Log($"packageName={packageName}");

                // Unity packages can only contain Assets/ content
                const string rootToExport = "Assets";
                if (!AssetDatabase.IsValidFolder(rootToExport))
                    throw new DirectoryNotFoundException("Assets folder not found (unexpected).");

                var outputPath = Path.Combine(outDir, $"{SanitizeFileName(packageName)}.unitypackage");
                Log($"Exporting '{rootToExport}' -> {outputPath}");

                // IncludeDependencies exports all referenced assets too (safe for "whole Assets")
                AssetDatabase.ExportPackage(
                    rootToExport,
                    outputPath,
                    ExportPackageOptions.Interactive | ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies
                );

                Log("Export completed successfully.");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                LogErr("Export failed with exception:");
                LogErr(ex.ToString());
                logWriter.WriteLine("CI_EXPORT_FAILED=1");
                logWriter.Flush();
                EditorApplication.Exit(1);
            }
        }

        private static string GetArg(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == key)
                    return args[i + 1];
            return null;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
#endif
