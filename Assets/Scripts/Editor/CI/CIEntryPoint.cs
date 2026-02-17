#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MyToolz.CI
{
    public static class CIEntryPoint
    {
        /// <summary>
        /// Exports one unitypackage per folder in Assets/Packages/<Tool>/ (must contain package.json).
        /// Optionally merges tool-to-tool deps (via package.json dependencies on com.mytoolz.*)
        /// and also merges "project dependencies" discovered by AssetDatabase.GetDependencies.
        ///
        /// Args:
        ///  -mytoolzOutDir <path>                (default: build_output)
        ///  -mytoolzMergeDeps <true|false>       (default: true)
        ///  -mytoolzIncludeProjectDeps <true|false> (default: true)
        /// </summary>
        public static void ExportAllToolsUnderAssetsPackages()
        {
            var outDir = GetArg("-mytoolzOutDir") ?? "build_output";
            var mergeDeps = IsTrue(GetArg("-mytoolzMergeDeps"), defaultValue: true);
            var includeProjectDeps = IsTrue(GetArg("-mytoolzIncludeProjectDeps"), defaultValue: true);

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
                Log("ExportAllToolsUnderAssetsPackages started");
                Log($"Unity={Application.unityVersion}, Platform={Application.platform}");
                Log($"outDir={outDir}");
                Log($"mergeDeps={mergeDeps}, includeProjectDeps={includeProjectDeps}");

                var packagesRoot = "Assets/Packages";
                if (!AssetDatabase.IsValidFolder(packagesRoot))
                    throw new DirectoryNotFoundException($"Folder not found: {packagesRoot}");

                // Discover tool folders (Assets/Packages/<Tool>/package.json)
                var toolFolders = Directory.GetDirectories(packagesRoot)
                    .Select(d => d.Replace('\\', '/'))
                    .Where(d => File.Exists(Path.Combine(d, "package.json")))
                    .Select(d => d.Substring(packagesRoot.Length + 1)) // folder name only
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray();

                if (toolFolders.Length == 0)
                    throw new Exception($"No packages found under {packagesRoot} (expected Assets/Packages/<Tool>/package.json).");

                Log($"Discovered {toolFolders.Length} tool(s): {string.Join(", ", toolFolders)}");

                // Call your existing exporter
                MyToolzUnityPackageExporter.ExportFolders(
                    toolFolders,
                    outDir,
                    mergeDeps,
                    includeProjectDeps,
                    Log,
                    LogErr
                );

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Log("ExportAllToolsUnderAssetsPackages completed successfully");
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

        private static bool IsTrue(string s, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(s)) return defaultValue;
            s = s.Trim().ToLowerInvariant();
            return s is "1" or "true" or "yes" or "y";
        }
    }
}
#endif
