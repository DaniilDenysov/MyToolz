using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.ScriptableObjects.Utilites.Debug
{
    public enum FontStyle
    {
        Default,
        Bold,
        Italic
        //UnderLine,
        //StrikeThrough,
        //Size
    }

    [CreateAssetMenu(fileName = "DebugUtilityMessage", menuName = "MyToolz/Debug/DebugUtilityMessage")]
    public class DebugUtilityMessageSO : ScriptableObject
    {
        [SerializeField] private Color color = Color.green;
        [SerializeField] private FontStyle fontStyle = FontStyle.Default;
        [SerializeField] private float fontSize = 12.5f;

        public Color Color => color;
        public FontStyle FontStyle => fontStyle;
        public float FontSize => fontSize;
    }
}
