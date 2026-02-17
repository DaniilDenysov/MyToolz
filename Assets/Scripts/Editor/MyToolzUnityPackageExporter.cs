#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


/// <summary>
/// Generated with ChatGPT for automation purposes
/// </summary>
public static class MyToolzUnityPackageExporter
{
    public static void ExportPerModule_WithMergedDeps()
    {
        try
        {
            // Prefer command line args (most reliable in CI), then fall back to env vars.
            // Args supported:
            //   -mytoolzModulesFile <path-to-json-array>
            //   -mytoolzModules <json-array>
            //   -mytoolzOutDir <output-folder>
            //   -mytoolzMergeDeps <true|false>

            var modulesJson = "[]";

            var modulesFile = GetArg("-mytoolzModulesFile");
            if (!string.IsNullOrWhiteSpace(modulesFile))
            {
                if (!File.Exists(modulesFile))
                {
                    Debug.LogError($"Modules file not found: {modulesFile}");
                    EditorApplication.Exit(1);
                    return;
                }
                modulesJson = File.ReadAllText(modulesFile);
            }
            else
            {
                modulesJson = GetArg("-mytoolzModules")
                              ?? Environment.GetEnvironmentVariable("MODULES")
                              ?? "[]";
            }

            var outDir = GetArg("-mytoolzOutDir")
                         ?? Environment.GetEnvironmentVariable("OUT_DIR")
                         ?? "build_output";

            var mergeDeps = IsTrue(GetArg("-mytoolzMergeDeps")
                                   ?? Environment.GetEnvironmentVariable("MERGE_DEPS"));

            Debug.Log($"[MyToolzUnityPackageExporter] outDir='{outDir}', mergeDeps={mergeDeps}");
            Debug.Log($"[MyToolzUnityPackageExporter] modulesJson='{modulesJson}'");

            var moduleFolders = ParseJsonStringArray(modulesJson);
            if (moduleFolders.Length == 0)
            {
                Debug.LogError("No modules provided. Provide -mytoolzModulesFile, -mytoolzModules, or set env MODULES to a JSON array.");
                EditorApplication.Exit(1);
                return;
            }

            Directory.CreateDirectory(outDir);

            // Read all package.json metadata
            var metas = new Dictionary<string, PackageMeta>(); // folder -> meta
            var nameToFolder = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var folder in moduleFolders)
            {
                var root = $"Assets/Packages/{folder}";
                var pkgJsonPath = Path.Combine(root, "package.json");
                if (!File.Exists(pkgJsonPath))
                {
                    Debug.LogError($"Missing package.json: {pkgJsonPath}");
                    continue;
                }

                var meta = PackageMeta.Load(folder, root, pkgJsonPath);
                metas[folder] = meta;

                // Map package name -> folder, e.g. com.mytoolz.eventbus -> EventBus
                if (!string.IsNullOrWhiteSpace(meta.PackageName))
                    nameToFolder[meta.PackageName] = folder;
            }

            foreach (var folder in moduleFolders)
            {
                if (!metas.TryGetValue(folder, out var meta))
                    continue;

                // Compute transitive internal deps (by com.mytoolz.* names)
                var foldersToInclude = new HashSet<string>(StringComparer.Ordinal) { folder };

                if (mergeDeps)
                {
                    var stack = new Stack<string>();
                    stack.Push(folder);

                    while (stack.Count > 0)
                    {
                        var cur = stack.Pop();
                        var curMeta = metas[cur];

                        foreach (var depName in curMeta.InternalDependencyNames)
                        {
                            if (!nameToFolder.TryGetValue(depName, out var depFolder))
                                continue; // dependency not in this repo/modules list

                            if (foldersToInclude.Add(depFolder))
                                stack.Push(depFolder);
                        }
                    }
                }

                // Collect asset paths from folders
                var exportRoots = foldersToInclude
                    .Select(f => metas[f].RootFolder)
                    .Distinct()
                    .ToArray();

                // Export all assets under those roots (stable and explicit)
                var guids = AssetDatabase.FindAssets("", exportRoots);
                var paths = guids.Select(AssetDatabase.GUIDToAssetPath)
                                 .Where(p => exportRoots.Any(r => p.StartsWith(r, StringComparison.Ordinal)))
                                 .Distinct()
                                 .ToArray();

                var outPath = Path.Combine(outDir, $"MyToolz-{folder}-{meta.Version}{(mergeDeps ? "-with-deps" : "")}.unitypackage");

                AssetDatabase.ExportPackage(
                    paths,
                    outPath,
                    ExportPackageOptions.Recurse // we already explicitly included dependency folders
                );

                Debug.Log($"Exported: {outPath}");
            }

            AssetDatabase.Refresh();
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    private static string? GetArg(string key)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], key, StringComparison.Ordinal))
                return args[i + 1];
        }
        return null;
    }

    private static bool IsTrue(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim().ToLowerInvariant();
        return s is "1" or "true" or "yes" or "y";
    }

    private static string[] ParseJsonStringArray(string json)
    {
        // minimal parser for ["A","B"]
        var list = new List<string>();
        bool inStr = false;
        var cur = "";
        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '"' && (i == 0 || json[i - 1] != '\\'))
            {
                inStr = !inStr;
                if (!inStr)
                {
                    if (!string.IsNullOrWhiteSpace(cur)) list.Add(cur);
                    cur = "";
                }
                continue;
            }
            if (inStr) cur += c;
        }
        return list.ToArray();
    }

    private sealed class PackageMeta
    {
        public string FolderName = "";
        public string RootFolder = ""; // Assets/Packages/<Folder>
        public string PackageName = ""; // com.mytoolz.something
        public string Version = "0.0.0";
        public List<string> InternalDependencyNames = new(); // com.mytoolz.*

        public static PackageMeta Load(string folderName, string rootFolder, string pkgJsonPath)
        {
            // lightweight parse; avoids bringing JSON libs
            var text = File.ReadAllText(pkgJsonPath);

            string GetString(string key)
            {
                var k = $"\"{key}\"";
                var idx = text.IndexOf(k, StringComparison.Ordinal);
                if (idx < 0) return "";
                idx = text.IndexOf(':', idx);
                if (idx < 0) return "";
                idx++;
                while (idx < text.Length && char.IsWhiteSpace(text[idx])) idx++;
                if (idx >= text.Length || text[idx] != '"') return "";
                idx++;
                var end = text.IndexOf('"', idx);
                if (end < 0) return "";
                return text.Substring(idx, end - idx);
            }

            var meta = new PackageMeta
            {
                FolderName = folderName,
                RootFolder = rootFolder.Replace('\\', '/'),
                PackageName = GetString("name"),
                Version = GetString("version")
            };

            // parse dependencies object and keep com.mytoolz.* keys
            var depsKey = "\"dependencies\"";
            var dIdx = text.IndexOf(depsKey, StringComparison.Ordinal);
            if (dIdx >= 0)
            {
                var brace = text.IndexOf('{', dIdx);
                if (brace >= 0)
                {
                    int depth = 0;
                    int i = brace;
                    int start = brace;
                    for (; i < text.Length; i++)
                    {
                        if (text[i] == '{') depth++;
                        else if (text[i] == '}')
                        {
                            depth--;
                            if (depth == 0) { i++; break; }
                        }
                    }
                    var obj = text.Substring(start, i - start);

                    // very simple: find all quoted keys and filter
                    for (int j = 0; j < obj.Length; j++)
                    {
                        if (obj[j] != '"') continue;
                        int k = obj.IndexOf('"', j + 1);
                        if (k < 0) break;
                        var key = obj.Substring(j + 1, k - (j + 1));
                        if (key.StartsWith("com.mytoolz.", StringComparison.Ordinal))
                            meta.InternalDependencyNames.Add(key);
                        j = k;
                    }
                }
            }

            return meta;
        }
    }
}
#endif
