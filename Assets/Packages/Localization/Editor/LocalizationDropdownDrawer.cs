#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MyToolz.Localization.Editor
{
    public abstract class LocalizationDropdownDrawer : PropertyDrawer
    {
        private const float WarningWidth = 20f;

        protected abstract string DatabaseFieldName { get; }

        protected abstract IReadOnlyList<string> GetOptions(LocalizationDatabaseSO database);

        protected virtual string EmptyLabel => "<Select>";

        protected virtual string MissingTooltip => "This value is not present in the current CSV.";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            LocalizationDatabaseSO database = ResolveDatabase(property, DatabaseFieldName);

            EditorGUI.BeginProperty(position, label, property);

            Rect controlRect = EditorGUI.PrefixLabel(position, label);

            if (database == null)
            {
                property.stringValue = EditorGUI.TextField(controlRect, property.stringValue);
                EditorGUI.EndProperty();
                return;
            }

            List<string> options = new List<string>(GetOptions(database));
            string current = property.stringValue;
            bool missing = !string.IsNullOrEmpty(current) && !options.Contains(current);

            Rect buttonRect = controlRect;
            if (missing)
            {
                buttonRect.width -= WarningWidth;
            }

            string display = string.IsNullOrEmpty(current) ? EmptyLabel : current;

            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(display), FocusType.Keyboard))
            {
                ShowMenu(buttonRect, property, options);
            }

            if (missing)
            {
                Rect warnRect = new Rect(buttonRect.xMax, controlRect.y, WarningWidth, controlRect.height);
                GUIContent warn = EditorGUIUtility.IconContent("console.warnicon.sml");
                warn.tooltip = MissingTooltip;
                GUI.Label(warnRect, warn);
            }

            EditorGUI.EndProperty();
        }

        private static LocalizationDatabaseSO ResolveDatabase(SerializedProperty property, string fieldName)
        {
            SerializedProperty sibling = FindSibling(property, fieldName);
            if (sibling != null &&
                sibling.propertyType == SerializedPropertyType.ObjectReference &&
                sibling.objectReferenceValue is LocalizationDatabaseSO fromField)
            {
                return fromField;
            }

            return FindSingleDatabase();
        }

        private static SerializedProperty FindSibling(SerializedProperty property, string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return null;
            }

            string path = property.propertyPath;
            int dot = path.LastIndexOf('.');
            string siblingPath = dot < 0 ? fieldName : path.Substring(0, dot + 1) + fieldName;
            return property.serializedObject.FindProperty(siblingPath);
        }

        private static LocalizationDatabaseSO FindSingleDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:LocalizationDatabaseSO");
            if (guids.Length == 0)
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<LocalizationDatabaseSO>(path);
        }

        private static void ShowMenu(Rect rect, SerializedProperty property, List<string> options)
        {
            SerializedObject serializedObject = property.serializedObject;
            string propertyPath = property.propertyPath;
            string current = property.stringValue;

            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("<None>"), string.IsNullOrEmpty(current),
                () => Assign(serializedObject, propertyPath, string.Empty));

            if (options.Count > 0)
            {
                menu.AddSeparator(string.Empty);
            }

            foreach (string option in options)
            {
                string captured = option;
                menu.AddItem(new GUIContent(option), option == current,
                    () => Assign(serializedObject, propertyPath, captured));
            }

            menu.DropDown(rect);
        }

        private static void Assign(SerializedObject serializedObject, string propertyPath, string value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            if (property == null)
            {
                return;
            }

            property.stringValue = value;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
