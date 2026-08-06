using UnityEditor;
using UnityEditor.UI;

namespace MyToolz.GameSettings
{
    [CustomEditor(typeof(AbstractButtonCarousel), true), CanEditMultipleObjects]
    public class ButtonCarouselEditor : ButtonEditor
    {
        private static readonly string[] ButtonProperties =
        {
            "m_Script",
            "m_Navigation",
            "m_Transition",
            "m_Colors",
            "m_SpriteState",
            "m_AnimationTriggers",
            "m_Interactable",
            "m_TargetGraphic",
            "m_OnClick"
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, ButtonProperties);
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}
