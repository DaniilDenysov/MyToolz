#if UNITY_EDITOR
using MyToolz.UI.Layout;
using UnityEditor;

namespace MyToolz.UI.Kit.Editor
{
    [CustomEditor(typeof(SwitchButton)), CanEditMultipleObjects]
    public sealed class SwitchButtonEditor : UIStrongButtonEditor
    {
        private SerializedProperty setting;
        private SerializedProperty icon;
        private SerializedProperty strategy;

        protected override void OnEnable()
        {
            base.OnEnable();
            setting = serializedObject.FindProperty("setting");
            icon = serializedObject.FindProperty("icon");
            strategy = serializedObject.FindProperty("strategy");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Switch Button", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(setting);
            EditorGUILayout.PropertyField(icon);
            EditorGUILayout.PropertyField(strategy);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
