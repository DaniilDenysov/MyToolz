#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MyToolz.Core;
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
                settings.Set(node.Path, nextEnabled);
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

        private void RebuildTree()
        {
            root = new Node { Name = "ROOT" };
            _foldouts.Clear();

            var types = new HashSet<Type>();
            TypeCache.GetTypesDerivedFrom<MonoBehaviourPlus>().ToList().ForEach(t => types.Add(t));
            TypeCache.GetTypesDerivedFrom<ScriptableObjectPlus>().ToList().ForEach(t => types.Add(t));
            TypeCache.GetTypesDerivedFrom<ObjectPlus>().ToList().ForEach(t => types.Add(t));

            foreach (var t in types.Where(t =>
                         t != null &&
                         t.IsClass &&
                         !t.IsAbstract &&
                         t.Assembly.GetName().Name.StartsWith(assemblyPrefix)))
            {
                AddType(t);
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
