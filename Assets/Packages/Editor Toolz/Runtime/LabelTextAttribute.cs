using System;

namespace MyToolz.EditorToolz
{
    /// <summary>
    /// Overrides the label shown in the inspector for a field, mirroring Odin's
    /// <c>[LabelText]</c>. The field's tooltip (if any) is preserved.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class LabelTextAttribute : Attribute
    {
        public string Text { get; }

        public LabelTextAttribute(string text)
        {
            Text = text;
        }
    }
}
