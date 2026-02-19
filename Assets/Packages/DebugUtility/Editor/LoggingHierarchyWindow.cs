#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MyToolz.Utilities.Debug;

namespace MyToolz.EditorToolz.Logging
{
    public sealed class LoggingHierarchyWindow : EditorWindow
    {
        private sealed class Node
        {
            public string Name;
            public string Path;
            public bool IsType;
            public readonly Dictionary<string, Node> Children = new();
        }

        private const float RowHeight = 18f;
        private const float IndentWidth = 16f;
        private const float FoldoutWidth = 14f;
        private const float ToggleWidth = 18f;

        private LogGateSettingsSO settings;
        private Node root;
        private readonly Dictionary<string, bool> _foldouts = new();
        private Vector2 scroll;
        private string assemblyPrefix = "Assembly-CSharp";

        [MenuItem("Tools/MyToolz/Logging/Logging Hierarchy")]
        private static void Open()
        {
            GetWindow<LoggingHierarchyWindow>("Logging Hierarchy");
        }

        private void OnEnable()
        {
            settings = LogGateSettingsAssetUtility.GetOrCreate();
            LogGate.Settings = settings;
            RebuildTree();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawGlobalToggle();

            using var scroll = new EditorGUILayout.ScrollViewScope(this.scroll);
            this.scroll = scroll.scrollPosition;

            if (root == null) return;

            foreach (var child in root.Children.Values.OrderBy(n => n.Name))
                DrawNode(child, 0);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Assembly:", GUILayout.Width(70));
                assemblyPrefix = GUILayout.TextField(assemblyPrefix, EditorStyles.toolbarTextField, GUILayout.Width(180));

                if (GUILayout.Button("Rebuild", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    RebuildTree();

                if (GUILayout.Button("Clear Cache", EditorStyles.toolbarButton, GUILayout.Width(85)))
                    LogGate.ClearCache();
            }
        }

        private void DrawGlobalToggle()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                bool current = GetEffective("*");
                bool next = EditorGUILayout.Toggle(current, GUILayout.Width(ToggleWidth));
                GUILayout.Label("Global logging enabled (*)", EditorStyles.boldLabel);

                if (next != current)
                {
                    settings.Set("*", next);
                    LogGate.ClearCache();
                    EditorUtility.SetDirty(settings);
                }
            }
        }

        private void DrawNode(Node node, int depth)
        {
            Rect row = GUILayoutUtility.GetRect(0, RowHeight, GUILayout.ExpandWidth(true));

            float x = row.x + depth * IndentWidth;

            if (node.Children.Count > 0)
            {
                bool open = _foldouts.TryGetValue(node.Path, out var o) && o;
                bool next = EditorGUI.Foldout(
                    new Rect(x, row.y, FoldoutWidth, row.height),
                    open,
                    GUIContent.none,
                    true
                );
                if (next != open) _foldouts[node.Path] = next;
            }

            x += FoldoutWidth;

            bool enabled = GetEffective(node.Path);
            bool nextEnabled = EditorGUI.Toggle(
                new Rect(x, row.y, ToggleWidth, row.height),
                enabled
            );

            if (nextEnabled != enabled)
            {
                ApplyToggle(node, nextEnabled);

                LogGate.ClearCache();
                EditorUtility.SetDirty(settings);
            }


            x += ToggleWidth + 2f;

            using (new EditorGUI.DisabledScope(!enabled))
            {
                EditorGUI.LabelField(
                    new Rect(x, row.y, row.width - x, row.height),
                    node.Name,
                    node.IsType ? EditorStyles.label : EditorStyles.boldLabel
                );
            }

            if (node.Children.Count > 0 &&
                _foldouts.TryGetValue(node.Path, out var expanded) &&
                expanded)
            {
                foreach (var child in node.Children.Values.OrderBy(n => n.Name))
                    DrawNode(child, depth + 1);
            }
        }

        private void ApplyToggle(Node node, bool enable)
        {
            if (node == null || root == null)
                return;

            if (enable)
            {
                // Enable everything along the path to this node (namespaces + type),
                // and optionally the whole subtree if the node is a namespace/group.
                EnablePath(node.Path);
                EnableSubtree(node);
                ExpandFoldoutsAlongPath(node.Path);
            }
            else
            {
                DisableSubtree(node);
                // Walk upwards and disable parents that no longer contain any enabled types.
                DisableEmptyAncestors(node.Path);
            }
        }

        private void EnablePath(string targetPath)
        {
            if (string.IsNullOrEmpty(targetPath))
                return;

            // Enable each prefix segment: A, A.B, A.B.C ...
            int dot = targetPath.IndexOf('.');
            while (dot > 0)
            {
                settings.Set(targetPath[..dot], true);
                dot = targetPath.IndexOf('.', dot + 1);
            }

            // And the full node path itself.
            settings.Set(targetPath, true);
        }

        private void EnableSubtree(Node node)
        {
            if (node == null)
                return;

            foreach (var n in EnumerateSubtree(node))
                settings.Set(n.Path, true);
        }

        private void DisableSubtree(Node node)
        {
            foreach (var n in EnumerateSubtree(node))
                settings.Set(n.Path, false);
        }

        private void DisableEmptyAncestors(string fromPath)
        {
            if (string.IsNullOrEmpty(fromPath) || root == null)
                return;

            string walk = ParentPath(fromPath);
            while (!string.IsNullOrEmpty(walk))
            {
                var n = FindNodeByPath(walk);
                if (n == null)
                    break;

                // If this ancestor still contains any enabled types, stop.
                if (CountEnabledTypes(n) > 0)
                    break;

                settings.Set(walk, false);
                walk = ParentPath(walk);
            }
        }

        private static string ParentPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            int dot = path.LastIndexOf('.');
            return dot > 0 ? path[..dot] : null;
        }

        private Node FindNodeByPath(string path)
        {
            if (root == null || string.IsNullOrEmpty(path))
                return null;

            var parts = path.Split('.');
            var current = root;
            for (int i = 0; i < parts.Length; i++)
            {
                if (!current.Children.TryGetValue(parts[i], out var next))
                    return null;

                current = next;
            }

            // Some nodes (types) store Path as full name; namespace nodes store cumulative prefix.
            // Either way, require exact match.
            return string.Equals(current.Path, path, StringComparison.Ordinal) ? current : null;
        }

        private int CountEnabledTypes(Node node)
        {
            int count = 0;
            foreach (var n in EnumerateSubtree(node))
            {
                if (!n.IsType)
                    continue;

                if (GetEffective(n.Path))
                    count++;
            }

            return count;
        }

        private IEnumerable<Node> EnumerateAllNodes()
        {
            if (root == null)
                yield break;

            foreach (var child in root.Children.Values)
                foreach (var n in EnumerateSubtree(child))
                    yield return n;
        }

        private static IEnumerable<Node> EnumerateSubtree(Node node)
        {
            if (node == null)
                yield break;

            yield return node;
            foreach (var child in node.Children.Values)
                foreach (var n in EnumerateSubtree(child))
                    yield return n;
        }

        private static bool IsOnPath(string nodePath, string targetPath)
        {
            if (string.IsNullOrEmpty(nodePath) || string.IsNullOrEmpty(targetPath))
                return false;

            if (nodePath.Length > targetPath.Length)
                return false;

            if (!targetPath.StartsWith(nodePath, StringComparison.Ordinal))
                return false;

            return nodePath.Length == targetPath.Length || targetPath[nodePath.Length] == '.';
        }

        private void ExpandFoldoutsAlongPath(string targetPath)
        {
            if (string.IsNullOrEmpty(targetPath))
                return;

            int dot = targetPath.IndexOf('.');
            while (dot > 0)
            {
                var prefix = targetPath[..dot];
                _foldouts[prefix] = true;
                dot = targetPath.IndexOf('.', dot + 1);
            }
        }


        private void RebuildTree()
        {
            root = new Node { Name = "ROOT" };
            _foldouts.Clear();

            foreach (var t in FindTypesCallingDebugUtility(assemblyPrefix))
                AddType(t);
        }

        private static readonly Regex DebugUtilityInvocationRegex =
            new(@"\b(?:MyToolz\.Utilities\.Debug\.)?DebugUtility\s*\.\s*[A-Za-z_]\w*\s*\(",
                RegexOptions.Compiled);

        private static readonly Regex StripCommentsAndStringsRegex =
            new(
                @"//.*?$|/\*.*?\*/|@""(?:""""|[^""])*""|""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*'",
                RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline);

        private static IEnumerable<Type> FindTypesCallingDebugUtility(string assemblyNamePrefix)
        {
            var guids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" });
            var yielded = new HashSet<Type>();

            for (int i = 0; i < guids.Length; i++)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                if (!assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                string text;
                try
                {
                    text = File.ReadAllText(assetPath);
                }
                catch
                {
                    continue;
                }

                text = StripCommentsAndStringsRegex.Replace(text, " ");
                if (!DebugUtilityInvocationRegex.IsMatch(text))
                    continue;

                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
                if (script == null)
                    continue;

                var type = script.GetClass();
                if (type == null || !type.IsClass || type.IsAbstract)
                    continue;

                if (!string.IsNullOrEmpty(assemblyNamePrefix) &&
                    !type.Assembly.GetName().Name.StartsWith(assemblyNamePrefix, StringComparison.Ordinal))
                    continue;

                if (yielded.Add(type))
                    yield return type;
            }
        }



        private void AddType(Type t)
        {
            var ns = string.IsNullOrEmpty(t.Namespace) ? "(NoNamespace)" : t.Namespace;
            var parts = ns.Split('.');
            var current = root;
            var path = "";

            foreach (var p in parts)
            {
                path = string.IsNullOrEmpty(path) ? p : $"{path}.{p}";
                if (!current.Children.TryGetValue(p, out var next))
                {
                    next = new Node { Name = p, Path = path };
                    current.Children[p] = next;
                }
                current = next;
            }

            current.Children[t.Name] = new Node
            {
                Name = t.Name,
                Path = t.FullName,
                IsType = true
            };
        }

        private bool GetEffective(string path)
        {
            if (settings.TryGet(path, out var v))
                return v;

            int dot = path.LastIndexOf('.');
            while (dot > 0)
            {
                path = path[..dot];
                if (settings.TryGet(path, out v))
                    return v;
                dot = path.LastIndexOf('.');
            }

            if (settings.TryGet("*", out v))
                return v;

            return settings.DefaultEnabled;
        }
    }
}
#endif
