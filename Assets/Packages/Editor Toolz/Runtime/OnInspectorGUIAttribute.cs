using System;

namespace MyToolz.EditorToolz
{
    /// <summary>
    /// Marks a parameterless method that draws custom IMGUI inside the inspector,
    /// mirroring Odin's <c>[OnInspectorGUI]</c>. The method is invoked at its
    /// position in the member order (respecting <see cref="PropertyOrderAttribute"/>
    /// and any group it belongs to). When placed on a field/property the optional
    /// <see cref="Prepend"/>/<see cref="Append"/> callbacks are invoked around it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class OnInspectorGUIAttribute : Attribute
    {
        public string Prepend { get; }
        public string Append { get; }

        public OnInspectorGUIAttribute() { }

        public OnInspectorGUIAttribute(string append)
        {
            Append = append;
        }

        public OnInspectorGUIAttribute(string prepend, string append)
        {
            Prepend = prepend;
            Append = append;
        }
    }
}
