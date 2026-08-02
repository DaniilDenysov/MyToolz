#if UNITY_EDITOR
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace MyToolz.Localization.Editor
{
    [CustomEditor(typeof(LocalizationText)), CanEditMultipleObjects]
    public class LocalizationTextEditor : TMP_EditorPanelUI
    {
        private SerializedProperty bindingProp;
        private SerializedProperty applyFontProp;

        protected override void OnEnable()
        {
            base.OnEnable();
            bindingProp = serializedObject.FindProperty("binding");
            applyFontProp = serializedObject.FindProperty("applyLanguageFont");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(bindingProp);
            EditorGUILayout.PropertyField(applyFontProp);
            serializedObject.ApplyModifiedProperties();

            DrawPreview();

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }

        private void DrawPreview()
        {
            if (bindingProp.objectReferenceValue is not LocalizationBindingSO binding || binding.Database == null)
            {
                return;
            }

            LocalizationLanguageSO language = binding.Database.DefaultLanguage;
            string preview = language != null ? binding.Resolve(language) : binding.Key;
            string languageName = language != null ? language.DisplayName : "no language";

            EditorGUILayout.HelpBox($"Preview ({languageName}): {preview}", MessageType.None);

            if (GUILayout.Button("Preview In Scene"))
            {
                foreach (Object target in targets)
                {
                    if (target is LocalizationText text)
                    {
                        Undo.RecordObject(text, "Preview Localization");
                        text.EditorPreview();
                        EditorUtility.SetDirty(text);
                    }
                }
            }
        }
    }
}
#endif
