using System;
using UnityEngine;

namespace MyToolz.EditorToolz
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public sealed class ShowInInspectorAttribute : PropertyAttribute
    {
        public bool ReadOnly { get; }

        public ShowInInspectorAttribute(bool readOnly = false)
        {
            ReadOnly = readOnly;
        }
    }
}
