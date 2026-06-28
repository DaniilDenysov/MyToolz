using System;

namespace MyToolz.EditorToolz
{
    /// <summary>
    /// Controls the draw order of a member relative to its siblings, mirroring Odin's
    /// <c>[PropertyOrder]</c>. Lower values are drawn first; members without the
    /// attribute default to <c>0</c> and keep their declaration order on ties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class PropertyOrderAttribute : Attribute
    {
        public float Order { get; }

        public PropertyOrderAttribute(float order = 0f)
        {
            Order = order;
        }
    }
}
