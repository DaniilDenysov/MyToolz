using System;

namespace MyToolz.EditorToolz
{
    /// <summary>
    /// Clamps a numeric field so it never drops below <see cref="Value"/>, mirroring
    /// Odin's <c>[MinValue]</c>. Unlike <see cref="UnityEngine.RangeAttribute"/> this
    /// keeps the regular field control instead of forcing a slider.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class MinValueAttribute : Attribute
    {
        public double Value { get; }

        public MinValueAttribute(double value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Clamps a numeric field so it never exceeds <see cref="Value"/>, mirroring
    /// Odin's <c>[MaxValue]</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class MaxValueAttribute : Attribute
    {
        public double Value { get; }

        public MaxValueAttribute(double value)
        {
            Value = value;
        }
    }
}
