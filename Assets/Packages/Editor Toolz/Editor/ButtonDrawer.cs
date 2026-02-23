using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyToolz.EditorToolz;
using UnityEditor;
using UnityEngine;

namespace MyToolz.Editor
{
    [CustomEditor(typeof(UnityEngine.Object), editorForChildClasses: true)]
    [CanEditMultipleObjects]
    public class ButtonDrawer : UnityEditor.Editor
    {
        private const BindingFlags MethodFlags =
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;

        private static readonly Dictionary<Type, List<ButtonMethod>> Cache = new();

        private static readonly float[] ButtonHeights =
        {
            EditorGUIUtility.singleLineHeight,
            EditorGUIUtility.singleLineHeight + 6f,
            EditorGUIUtility.singleLineHeight + 14f,
        };

        private List<ButtonMethod> _methods;

        protected virtual void OnEnable()
        {
            _methods = ResolveButtonMethods(target.GetType());
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (_methods == null || _methods.Count == 0)
                return;

            EditorGUILayout.Space(6f);

            var separatorRect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(separatorRect, new Color(0f, 0f, 0f, 0.15f));

            EditorGUILayout.Space(4f);

            foreach (var bm in _methods)
                DrawButton(bm);
        }

        private void DrawButton(ButtonMethod bm)
        {
            bool playmode = Application.isPlaying;

            bool enabled = bm.Attribute.Mode switch
            {
                ButtonMode.PlaymodeOnly => playmode,
                ButtonMode.EditmodeOnly => !playmode,
                _ => true,
            };

            float height = ButtonHeights[(int)bm.Attribute.Size];
            var label = ResolveLabel(bm);

            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = height,
                fontStyle = FontStyle.Bold,
                fontSize = bm.Attribute.Size switch
                {
                    ButtonSize.Small => 10,
                    ButtonSize.Large => 13,
                    _ => 11,
                }
            };

            Color? tint = ParseHexColor(bm.Attribute.HexColor);
            Color prevBg = GUI.backgroundColor;

            if (tint.HasValue)
                GUI.backgroundColor = tint.Value;

            using (new EditorGUI.DisabledScope(!enabled))
            {
                if (GUILayout.Button(label, style))
                    InvokeMethod(bm.Method);
            }

            if (tint.HasValue)
                GUI.backgroundColor = prevBg;

            if (!enabled)
            {
                string reason = bm.Attribute.Mode == ButtonMode.PlaymodeOnly
                    ? "Play mode only"
                    : "Edit mode only";

                var helpRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                helpRect.xMin += 4f;
                EditorGUI.LabelField(helpRect, reason, EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void InvokeMethod(MethodInfo method)
        {
            bool isStatic = method.IsStatic;

            if (!isStatic)
                Undo.RecordObjects(targets, $"Button: {method.Name}");

            foreach (var t in targets)
            {
                if (t == null) continue;

                try
                {
                    method.Invoke(isStatic ? null : t, null);
                    EditorUtility.SetDirty(t);
                }
                catch (TargetInvocationException tie)
                {
                    Debug.LogException(tie.InnerException ?? tie, t);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, t);
                }
            }
        }

        private static string ResolveLabel(ButtonMethod bm)
        {
            if (!string.IsNullOrWhiteSpace(bm.Attribute.Name))
                return bm.Attribute.Name;

            return ObjectNames.NicifyVariableName(bm.Method.Name);
        }

        private static Color? ParseHexColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return null;

            string cleaned = hex.StartsWith("#") ? hex : "#" + hex;
            if (ColorUtility.TryParseHtmlString(cleaned, out Color c))
                return c;

            return null;
        }

        private static List<ButtonMethod> ResolveButtonMethods(Type type)
        {
            if (Cache.TryGetValue(type, out var cached))
                return cached;

            var hierarchy = new Stack<Type>();
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
                hierarchy.Push(t);

            var list = new List<ButtonMethod>();

            foreach (var t in hierarchy)
            {
                var found = t.GetMethods(MethodFlags)
                    .Select(m => (Method: m, Attr: m.GetCustomAttribute<ButtonAttribute>(inherit: true)))
                    .Where(x => x.Attr != null && IsValidSignature(x.Method))
                    .OrderBy(x => x.Method.MetadataToken)
                    .Select(x => new ButtonMethod(x.Method, x.Attr));

                list.AddRange(found);
            }

            Cache[type] = list;
            return list;
        }

        private static bool IsValidSignature(MethodInfo method)
        {
            if (method.GetParameters().Length != 0)
            {
                Debug.LogWarning($"[Button] '{method.DeclaringType?.Name}.{method.Name}' has parameters and will be skipped. Button methods must have no parameters.");
                return false;
            }

            if (method.ReturnType != typeof(void))
            {
                Debug.LogWarning($"[Button] '{method.DeclaringType?.Name}.{method.Name}' has a non-void return type and will be skipped.");
                return false;
            }

            return true;
        }

        private readonly struct ButtonMethod
        {
            public readonly MethodInfo Method;
            public readonly ButtonAttribute Attribute;

            public ButtonMethod(MethodInfo method, ButtonAttribute attribute)
            {
                Method = method;
                Attribute = attribute;
            }
        }
    }
}