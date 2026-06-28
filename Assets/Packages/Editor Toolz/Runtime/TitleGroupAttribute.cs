using System;

namespace MyToolz.EditorToolz
{
    /// <summary>
    /// Draws a bold title with a horizontal rule above the members that share the
    /// same <see cref="Title"/>, mirroring Odin's <c>[TitleGroup]</c>. Unlike a
    /// foldout, a title group is always visible (non-collapsible). Nested titles are
    /// supported through a <c>"Parent/Child"</c> group path.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class TitleGroupAttribute : Attribute
    {
        public string Title { get; }
        public string Subtitle { get; }
        public float Order { get; set; }

        public TitleGroupAttribute(string title, string subtitle = null, float order = 0f)
        {
            Title = title ?? string.Empty;
            Subtitle = subtitle;
            Order = order;
        }
    }
}
