#if UNITY_EDITOR
using MyToolz.EditorToolz;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MyToolz.Editor
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    [CustomPropertyDrawer(typeof(HideIfAttribute))]
    internal sealed class ConditionalVisibilityDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!ShouldDraw(property))
                return;

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!ShouldDraw(property))
                return 0f;

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private bool ShouldDraw(SerializedProperty property)
        {
            var showAttr = attribute as ShowIfAttribute;
            var hideAttr = attribute as HideIfAttribute;

            bool result = true;

            foreach (var target in property.serializedObject.targetObjects)
            {
                if (target == null)
                    continue;

                bool condition = EvaluateCondition(target, property);

                if (showAttr != null)
                    result &= condition;

                if (hideAttr != null)
                    result &= !condition;
            }

            return result;
        }

        private bool EvaluateCondition(object target, SerializedProperty property)
        {
            var attr = attribute;
            string memberName = null;
            object compareValue = null;

            if (attr is ShowIfAttribute s)
            {
                memberName = s.MemberName;
                compareValue = s.CompareValue;
            }
            else if (attr is HideIfAttribute h)
            {
                memberName = h.MemberName;
                compareValue = h.CompareValue;
            }

            if (string.IsNullOrWhiteSpace(memberName))
                return true;

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            var type = target.GetType();

            var field = type.GetField(memberName, flags);
            if (field != null)
            {
                var value = field.GetValue(target);
                return Compare(value, compareValue);
            }

            var prop = type.GetProperty(memberName, flags);
            if (prop != null && prop.CanRead)
            {
                var value = prop.GetValue(target);
                return Compare(value, compareValue);
            }

            var method = type.GetMethod(memberName, flags);
            if (method != null && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
            {
                var value = method.Invoke(target, null);
                return (bool)value;
            }

            Debug.LogWarning($"[ShowIf/HideIf] Member '{memberName}' not found on {type.Name}", target as UnityEngine.Object);
            return true;
        }

        private bool Compare(object value, object compareValue)
        {
            if (compareValue == null)
            {
                if (value is bool b)
                    return b;

                return value != null;
            }

            if (value == null)
                return false;

            if (value.GetType().IsEnum)
                return value.Equals(compareValue);

            return Equals(value, compareValue);
        }
    }
}
#endif
