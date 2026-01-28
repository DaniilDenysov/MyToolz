using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;

namespace MyToolz.EditorToolz
{
    [InitializeOnLoad]
    public static class ButtonAttributeInspectorInjector
    {
        static ButtonAttributeInspectorInjector()
        {
            Editor.finishedDefaultHeaderGUI += DrawButtonsInHeader;
        }

        private static void DrawButtonsInHeader(Editor editor)
        {
            if (editor == null || editor.target == null) return;

            var targets = editor.targets;
            if (targets == null || targets.Length == 0) return;

            var type = editor.target.GetType();
            var methods = GetButtonMethods(type);

            if (methods.Count == 0) return;

            GUILayout.Space(4);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (var m in methods)
                {
                    var attr = m.Attribute;
                    var label = string.IsNullOrEmpty(attr.Name)
                        ? ObjectNames.NicifyVariableName(m.Method.Name)
                        : attr.Name;

                    using (new EditorGUI.DisabledScope(!IsMethodCallable(m.Method)))
                    {
                        if (GUILayout.Button(label))
                        {
                            InvokeForTargets(m.Method, targets);
                        }
                    }
                }
            }
        }

        private static void InvokeForTargets(MethodInfo method, UnityEngine.Object[] targets)
        {
            bool isStatic = method.IsStatic;

            if (!isStatic)
            {
                Undo.RecordObjects(targets, $"Invoke {method.Name}");
            }

            foreach (var t in targets)
            {
                try
                {
                    method.Invoke(isStatic ? null : t, null);
                    EditorUtility.SetDirty(t);
                }
                catch (TargetInvocationException tie)
                {
                    Debug.LogException(tie.InnerException ?? tie);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private static bool IsMethodCallable(MethodInfo method)
        {
            return method.GetParameters().Length == 0 && method.ReturnType == typeof(void);
        }

        private static readonly Dictionary<Type, List<ButtonMethod>> Cache = new();

        private static List<ButtonMethod> GetButtonMethods(Type type)
        {
            if (Cache.TryGetValue(type, out var cached))
                return cached;

            var hierarchy = GetTypeHierarchyBaseToDerived(type);

            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            var list = new List<ButtonMethod>(32);

            foreach (var t in hierarchy)
            {
                var methods = t.GetMethods(flags)
                    .Select(m => new { Method = m, Attr = m.GetCustomAttribute<ButtonAttribute>(true) })
                    .Where(x => x.Attr != null)
                    .OrderBy(x => x.Method.MetadataToken);

                foreach (var x in methods)
                    list.Add(new ButtonMethod(x.Method, x.Attr));
            }

            Cache[type] = list;
            return list;
        }

        private static IEnumerable<Type> GetTypeHierarchyBaseToDerived(Type type)
        {
            var stack = new Stack<Type>();
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
                stack.Push(t);

            while (stack.Count > 0)
                yield return stack.Pop();
        }

        private readonly struct ButtonMethod
        {
            public MethodInfo Method { get; }
            public ButtonAttribute Attribute { get; }

            public ButtonMethod(MethodInfo method, ButtonAttribute attribute)
            {
                Method = method;
                Attribute = attribute;
            }
        }
    }
}
#endif

namespace MyToolz.EditorToolz
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class ButtonAttribute : Attribute
    {
        public string Name { get; }

        public ButtonAttribute(string name = null)
        {
            Name = name;
        }
    }
}