using System;

namespace MyToolz.EditorToolz
{
    public enum ButtonSize
    {
        Small,
        Normal,
        Large,
    }

    public enum ButtonMode
    {
        Always,
        PlaymodeOnly,
        EditmodeOnly,
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class ButtonAttribute : Attribute
    {
        public string Name { get; }
        public ButtonSize Size { get; }
        public ButtonMode Mode { get; }
        public string HexColor { get; }

        public ButtonAttribute(
            string name = null,
            ButtonSize size = ButtonSize.Normal,
            ButtonMode mode = ButtonMode.Always,
            string hexColor = null)
        {
            Name = name;
            Size = size;
            Mode = mode;
            HexColor = hexColor;
        }
    }
}