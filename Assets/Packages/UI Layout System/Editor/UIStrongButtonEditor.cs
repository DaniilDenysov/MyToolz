#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace MyToolz.UI.Layout
{
    /// <summary>
    /// Inspector for <see cref="UIStrongButton"/>. Because UIStrongButton derives from Button, Unity's
    /// ButtonEditor claims its inspector (and Odin steps aside for uGUI custom editors) - so the
    /// binding-status box and the extra fields are drawn here, on top of the stock Button inspector.
    /// </summary>
    [CustomEditor(typeof(UIStrongButton)), CanEditMultipleObjects]
    public class UIStrongButtonEditor : ButtonEditor
    {
        private SerializedProperty requireBinding;
        private SerializedProperty disableWhenBroken;
        private SerializedProperty clickClip;
        private SerializedProperty pointerEnterClip;
        private SerializedProperty pointerDownClip;
        private SerializedProperty disableClip;

        protected override void OnEnable()
        {
            base.OnEnable();
            requireBinding = serializedObject.FindProperty("requireBinding");
            disableWhenBroken = serializedObject.FindProperty("disableWhenBroken");
            clickClip = serializedObject.FindProperty("clickClip");
            pointerEnterClip = serializedObject.FindProperty("pointerEnterClip");
            pointerDownClip = serializedObject.FindProperty("pointerDownClip");
            disableClip = serializedObject.FindProperty("disableClip");
        }

        public override void OnInspectorGUI()
        {
            DrawBindingStatus();

            serializedObject.Update();
            EditorGUILayout.PropertyField(requireBinding);
            EditorGUILayout.PropertyField(disableWhenBroken);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audio (optional - leave empty for no sound)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(clickClip);
            EditorGUILayout.PropertyField(pointerEnterClip);
            EditorGUILayout.PropertyField(pointerDownClip);
            EditorGUILayout.PropertyField(disableClip);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }

        private void DrawBindingStatus()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox("Binding status is shown when a single button is selected.", MessageType.None);
                return;
            }

            var strong = (UIStrongButton)target;
            var errors = new List<string>();
            var warnings = new List<string>();
            strong.Audit(errors, warnings, editTime: !Application.isPlaying);

            if (errors.Count > 0)
                EditorGUILayout.HelpBox(string.Join("\n", errors), MessageType.Error);
            else if (warnings.Count > 0)
                EditorGUILayout.HelpBox(string.Join("\n", warnings), MessageType.Warning);
            else
                EditorGUILayout.HelpBox("Binding healthy.", MessageType.Info);

            EditorGUILayout.Space();
        }
    }
}
#endif
