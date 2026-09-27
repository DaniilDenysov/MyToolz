#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;

namespace MyToolz.UI.Kit.Editor
{
    [CustomEditor(typeof(UIStrongToggle)), CanEditMultipleObjects]
    public sealed class UIStrongToggleEditor : ToggleEditor
    {
        private SerializedProperty setting;
        private SerializedProperty icon;
        private SerializedProperty onSprite;
        private SerializedProperty offSprite;
        private SerializedProperty clickClip;
        private SerializedProperty pointerEnterClip;
        private SerializedProperty pointerDownClip;
        private SerializedProperty disableClip;

        protected override void OnEnable()
        {
            base.OnEnable();
            setting = serializedObject.FindProperty("setting");
            icon = serializedObject.FindProperty("icon");
            onSprite = serializedObject.FindProperty("onSprite");
            offSprite = serializedObject.FindProperty("offSprite");
            clickClip = serializedObject.FindProperty("clickClip");
            pointerEnterClip = serializedObject.FindProperty("pointerEnterClip");
            pointerDownClip = serializedObject.FindProperty("pointerDownClip");
            disableClip = serializedObject.FindProperty("disableClip");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Setting", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(setting);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Icon Skin", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(icon);
            EditorGUILayout.PropertyField(onSprite);
            EditorGUILayout.PropertyField(offSprite);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audio (optional - leave empty for no sound)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(clickClip);
            EditorGUILayout.PropertyField(pointerEnterClip);
            EditorGUILayout.PropertyField(pointerDownClip);
            EditorGUILayout.PropertyField(disableClip);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
