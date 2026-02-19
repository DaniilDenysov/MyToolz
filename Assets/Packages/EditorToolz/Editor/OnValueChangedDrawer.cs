#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyToolz.EditorToolz;
using UnityEditor;
using UnityEngine;

namespace MyToolz.Editor
{
    [CustomPropertyDrawer(typeof(OnValueChangedAttribute), true)]
    internal class OnValueChangedDrawer : PropertyDrawer
    {
        private static readonly HashSet<string> InitialInvoked = new HashSet<string>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (OnValueChangedAttribute)attribute;

            EditorGUI.BeginProperty(position, label, property);

            bool changed;
            object newValue = null;

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, property, label, attr.IncludeChildren);
            changed = EditorGUI.EndChangeCheck();

            if (changed)
                newValue = TryGetManagedValue(property);

            if (attr.InvokeOnInitialDraw)
                TryInvokeInitialOnce(property, attr);

            if (changed)
                InvokeCallbacks(property, attr, newValue);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (OnValueChangedAttribute)attribute;
            return EditorGUI.GetPropertyHeight(property, label, attr.IncludeChildren);
        }

        private static void TryInvokeInitialOnce(SerializedProperty property, OnValueChangedAttribute attr)
        {
            if (string.IsNullOrWhiteSpace(attr.MethodName))
                return;

            var so = property.serializedObject;
            if (so == null) return;

            foreach (var t in so.targetObjects)
            {
                if (t == null) continue;

                var key = $"{t.GetInstanceID()}|{property.propertyPath}|{attr.MethodName}";
                if (InitialInvoked.Contains(key))
                    continue;

                InitialInvoked.Add(key);

                var val = TryGetManagedValue(property);

                InvokeOnTarget(t, property, attr.MethodName, val);
            }
        }

        private static void InvokeCallbacks(SerializedProperty property, OnValueChangedAttribute attr, object newValue)
        {
            if (string.IsNullOrWhiteSpace(attr.MethodName))
                return;

            var so = property.serializedObject;
            if (so == null) return;

            so.ApplyModifiedProperties();

            foreach (var t in so.targetObjects)
            {
                if (t == null) continue;
                InvokeOnTarget(t, property, attr.MethodName, newValue);
                EditorUtility.SetDirty(t);
            }
        }

        private static void InvokeOnTarget(UnityEngine.Object target, SerializedProperty property, string methodName, object newValue)
        {
            var type = target.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var methods = type
                .GetMethods(flags)
                .Where(m => string.Equals(m.Name, methodName, StringComparison.Ordinal))
                .ToArray();

            if (methods.Length == 0)
            {
                Debug.LogWarning($"[OnValueChanged] Method '{methodName}' not found on {type.Name} (property: {property.propertyPath}).", target);
                return;
            }

            MethodInfo best = null;
            object[] args = null;

            var fieldType = GetFieldTypeFromPropertyPath(type, property.propertyPath);

            best = methods.FirstOrDefault(m => m.ReturnType == typeof(void) && m.GetParameters().Length == 0);
            if (best != null)
            {
                SafeInvoke(best, target, Array.Empty<object>(), property);
                return;
            }

            if (fieldType != null)
            {
                best = methods.FirstOrDefault(m =>
                {
                    if (m.ReturnType != typeof(void)) return false;
                    var ps = m.GetParameters();
                    if (ps.Length != 1) return false;
                    return ps[0].ParameterType == fieldType;
                });

                if (best != null)
                {
                    args = new[] { CoerceValue(newValue, fieldType, property) };
                    SafeInvoke(best, target, args, property);
                    return;
                }
            }

            best = methods.FirstOrDefault(m =>
            {
                if (m.ReturnType != typeof(void)) return false;
                var ps = m.GetParameters();
                return ps.Length == 2 && ps[0].ParameterType == typeof(string) && ps[1].ParameterType == typeof(object);
            });

            if (best != null)
            {
                args = new object[] { property.propertyPath, newValue };
                SafeInvoke(best, target, args, property);
                return;
            }

            best = methods.FirstOrDefault(m =>
            {
                if (m.ReturnType != typeof(void)) return false;
                var ps = m.GetParameters();
                return ps.Length == 1 && ps[0].ParameterType == typeof(object);
            });

            if (best != null)
            {
                args = new object[] { newValue };
                SafeInvoke(best, target, args, property);
                return;
            }

            best = methods.FirstOrDefault(m =>
            {
                if (m.ReturnType != typeof(void)) return false;
                var ps = m.GetParameters();
                if (ps.Length != 1) return false;

                var pType = ps[0].ParameterType;
                if (newValue == null) return !pType.IsValueType || Nullable.GetUnderlyingType(pType) != null;
                return pType.IsInstanceOfType(newValue) || CanCoerce(newValue.GetType(), pType);
            });

            if (best != null)
            {
                var pType = best.GetParameters()[0].ParameterType;
                args = new[] { CoerceValue(newValue, pType, property) };
                SafeInvoke(best, target, args, property);
                return;
            }

            Debug.LogWarning($"[OnValueChanged] No compatible overload for '{methodName}' on {type.Name}. " +
                             $"Supported: void M(), void M(T), void M(object), void M(string, object). (property: {property.propertyPath})",
                target);
        }

        private static void SafeInvoke(MethodInfo method, UnityEngine.Object target, object[] args, SerializedProperty property)
        {
            try
            {
                method.Invoke(target, args);
            }
            catch (TargetInvocationException tie)
            {
                Debug.LogException(tie.InnerException ?? tie, target);
            }
            catch (Exception e)
            {
                Debug.LogException(e, target);
            }
        }

        private static object TryGetManagedValue(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer: return p.longValue;
                case SerializedPropertyType.Boolean: return p.boolValue;
                case SerializedPropertyType.Float: return p.doubleValue;
                case SerializedPropertyType.String: return p.stringValue;
                case SerializedPropertyType.Color: return p.colorValue;
                case SerializedPropertyType.ObjectReference: return p.objectReferenceValue;
                case SerializedPropertyType.Enum: return p.enumValueIndex;
                case SerializedPropertyType.Vector2: return p.vector2Value;
                case SerializedPropertyType.Vector3: return p.vector3Value;
                case SerializedPropertyType.Vector4: return p.vector4Value;
                case SerializedPropertyType.Rect: return p.rectValue;
                case SerializedPropertyType.Bounds: return p.boundsValue;
                case SerializedPropertyType.Quaternion: return p.quaternionValue;
                case SerializedPropertyType.Vector2Int: return p.vector2IntValue;
                case SerializedPropertyType.Vector3Int: return p.vector3IntValue;
                case SerializedPropertyType.RectInt: return p.rectIntValue;
                case SerializedPropertyType.BoundsInt: return p.boundsIntValue;
#if UNITY_2021_2_OR_NEWER
                case SerializedPropertyType.ManagedReference:
                    return null;
#endif
                default:
                    return null;
            }
        }

        private static Type GetFieldTypeFromPropertyPath(Type hostType, string propertyPath)
        {
            try
            {
                var current = hostType;
                var parts = propertyPath.Split('.');
                FieldInfo field = null;

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];

                    if (part == "Array")
                    {
                        i += 2;
                        continue;
                    }
                    if (part.StartsWith("data[", StringComparison.Ordinal))
                        continue;

                    field = GetFieldInHierarchy(current, part);
                    if (field == null) return null;

                    current = field.FieldType;
                }

                return field?.FieldType;
            }
            catch
            {
                return null;
            }
        }

        private static FieldInfo GetFieldInHierarchy(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                var f = t.GetField(name, flags);
                if (f != null) return f;
            }
            return null;
        }

        private static bool CanCoerce(Type from, Type to)
        {
            if (to.IsAssignableFrom(from)) return true;

            if (IsNumeric(from) && IsNumeric(to)) return true;

            if (to.IsEnum && IsNumeric(from)) return true;

            return false;
        }

        private static bool IsNumeric(Type type)
        {
            if (type == null)
                return false;

            type = Nullable.GetUnderlyingType(type) ?? type;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }


        private static object CoerceValue(object value, Type targetType, SerializedProperty context)
        {
            if (value == null)
            {
                if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                    return null;

                return Activator.CreateInstance(targetType);
            }

            var fromType = value.GetType();
            if (targetType.IsAssignableFrom(fromType))
                return value;

            try
            {
                if (targetType.IsEnum && value is IConvertible)
                {
                    var intVal = Convert.ToInt32(value);
                    return Enum.ToObject(targetType, intVal);
                }

                if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(targetType))
                    return Convert.ChangeType(value, targetType);

                return value;
            }
            catch
            {
                return value;
            }
        }
    }
}
#endif
