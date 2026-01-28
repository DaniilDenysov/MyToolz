#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MyToolz.EditorToolz
{
    [CustomPropertyDrawer(typeof(RequiredAttribute))]
    public sealed class RequiredDrawer : PropertyDrawer
    {
        private const float HelpBoxHeight = 32f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (IsValid(property))
                return EditorGUI.GetPropertyHeight(property, label, true);

            return EditorGUI.GetPropertyHeight(property, label, true) + HelpBoxHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var required = (RequiredAttribute)attribute;

            var fieldRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUI.GetPropertyHeight(property, label, true)
            );

            EditorGUI.PropertyField(fieldRect, property, label, true);

            if (IsValid(property))
                return;

            var helpRect = new Rect(
                position.x,
                fieldRect.yMax + 2f,
                position.width,
                HelpBoxHeight
            );

            string message = string.IsNullOrEmpty(required.Message)
                ? $"{label.text} is required."
                : required.Message;

            EditorGUI.HelpBox(helpRect, message, MessageType.Error);
        }

        private static bool IsValid(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue != null;

                case SerializedPropertyType.String:
                    return !string.IsNullOrEmpty(property.stringValue);

                case SerializedPropertyType.ArraySize:
                    return property.arraySize > 0;

                default:
                    return true;
            }
        }
    }
}
#endif
