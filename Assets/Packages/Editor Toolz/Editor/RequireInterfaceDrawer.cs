#if UNITY_EDITOR
using System;
using MyToolz.EditorToolz;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MyToolz.Editor
{
    [CustomPropertyDrawer(typeof(RequireInterfaceAttribute))]
    public sealed class RequireInterfaceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var required = (RequireInterfaceAttribute)attribute;

            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.HelpBox(position,
                    $"[RequireInterface] is only valid on UnityEngine.Object reference fields.",
                    MessageType.Error);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            Object assigned = EditorGUI.ObjectField(
                position, label, property.objectReferenceValue, required.InterfaceType, true);

            property.objectReferenceValue = ResolveInterface(assigned, required.InterfaceType);

            EditorGUI.EndProperty();
        }

        private static Object ResolveInterface(Object candidate, Type interfaceType)
        {
            if (candidate == null)
                return null;

            if (interfaceType.IsInstanceOfType(candidate))
                return candidate;

            if (candidate is GameObject gameObject)
                return gameObject.GetComponent(interfaceType);

            if (candidate is Component component)
                return component.GetComponent(interfaceType);

            return null;
        }
    }
}
#endif
