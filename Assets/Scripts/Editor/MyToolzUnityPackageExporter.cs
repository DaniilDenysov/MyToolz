#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MyToolzUnityPackageExporter
{
    public static void ExportFolders(
        string[] toolFolderNames,          // ["EventBus","UIManagement",...]
        string outDir,
        bool mergeToolDeps,
        bool includeProjectDeps,
        Action<string> log,
        Action<string> logErr)
    {
        // Read all package.json metadata for the discovered tools
        var metas = new Dictionary<string, PackageMeta>(StringComparer.Ordinal); // folder -> meta
        var nameToFolder = new Dictionary<string, string>(StringComparer.Ordinal); // com.mytoolz.* -> folder

        foreach (var folder in toolFolderNames)
        {
            var root = $"Assets/Packages/{folder}";
            var pkgJsonPath = Path.Combine(root, "package.json");
            if (!File.Exists(pkgJsonPath))
            {
                logErr($"Missing package.json: {pkgJsonPath}");
                continue;
            }

            var meta = PackageMeta.Load(folder, root, pkgJsonPath);
            metas[folder] = meta;

            if (!string.IsNullOrWhiteSpace(meta.PackageName))
                nameToFolder[meta.PackageName] = folder;
        }

        foreach (var folder in toolFolderNames)
        {
            if (!metas.TryGetValue(folder, out var meta))
                continue;

            // 1) Decide which tool folders to include (merge internal tool-to-tool deps)
            var foldersToInclude = new HashSet<string>(StringComparer.Ordinal) { folder };

            if (mergeToolDeps)
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
                            continue; // dependency not in this repo/tools set

                        if (foldersToInclude.Add(depFolder))
                            stack.Push(depFolder);
                    }
                }
            }

            var exportRoots = foldersToInclude
                .Select(f => metas[f].RootFolder)
                .Distinct()
                .ToArray();

            // 2) Collect ALL assets inside included roots
            var rootGuids = AssetDatabase.FindAssets("", exportRoots);
            var rootPaths = rootGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(p => p.Replace('\\', '/'))
                .Where(p => exportRoots.Any(r => p.StartsWith(r, StringComparison.Ordinal)))
                .Distinct()
                .ToArray();

            // 3) Optionally pull "project dependencies" (assets referenced outside the tool folders)
            //    This is the safety net that usually avoids compilation errors after import.
            IEnumerable<string> allPaths = rootPaths;

            if (includeProjectDeps)
            {
                var deps = AssetDatabase.GetDependencies(rootPaths, recursive: true)
                    .Select(p => p.Replace('\\', '/'))
                    .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal)) // unitypackage can only export Assets/*
                    .Where(p => !p.StartsWith("Assets/Packages/", StringComparison.Ordinal) // keep package roots already included
                                || exportRoots.Any(r => p.StartsWith(r, StringComparison.Ordinal)))
                    .Distinct()
                    .ToArray();

                // Important: also include any dependency that lives in Assets/Packages but outside current roots
                // ONLY when mergeToolDeps is on, otherwise you can accidentally “leak” other tools in.
                if (mergeToolDeps)
                {
                    deps = deps
                        .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal))
                        .Distinct()
                        .ToArray();
                }
                else
                {
                    // If mergeToolDeps is false, strictly prevent pulling other tool folders:
                    deps = deps
                        .Where(p => !p.StartsWith("Assets/Packages/", StringComparison.Ordinal)
                                    || exportRoots.Any(r => p.StartsWith(r, StringComparison.Ordinal)))
                        .Distinct()
                        .ToArray();
                }

                allPaths = rootPaths.Concat(deps).Distinct();
            }

            Directory.CreateDirectory(outDir);
            var outPath = Path.Combine(outDir, $"MyToolz-{folder}-{meta.Version}.unitypackage");

            log($"Exporting '{folder}' -> {outPath}");
            log($"  Included roots: {string.Join(", ", exportRoots)}");
            log($"  Files exported: {allPaths.Count()}");

            AssetDatabase.ExportPackage(
                allPaths.ToArray(),
                outPath,
                ExportPackageOptions.Recurse
            );

            if (!File.Exists(outPath))
                logErr($"Unity did not create the unitypackage at: {outPath}");
            else
                log($"Exported OK: {outPath}");
        }
    }

    private sealed class PackageMeta
    {
        public string FolderName = "";
        public string RootFolder = "";
        public string PackageName = "";
        public string Version = "0.0.0";
        public List<string> InternalDependencyNames = new();

        public static PackageMeta Load(string folderName, string rootFolder, string pkgJsonPath)
        {
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

            // Parse dependencies keys; keep com.mytoolz.* as "internal"
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
