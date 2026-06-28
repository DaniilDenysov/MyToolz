using System;

namespace MyToolz.EditorToolz
{
    /// <summary>
    /// Draws a small grey label at the trailing edge of a field, mirroring Odin's
    /// <c>[SuffixLabel]</c>. Useful for units such as <c>"s"</c> or <c>"m/s"</c>.
    /// When <see cref="Overlay"/> is <c>true</c> the suffix is drawn on top of the
    /// field's right edge instead of after it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class SuffixLabelAttribute : Attribute
    {
        public string Label { get; }
        public bool Overlay { get; }

        public SuffixLabelAttribute(string label, bool overlay = false)
        {
            Label = label;
            Overlay = overlay;
        }
    }
}
