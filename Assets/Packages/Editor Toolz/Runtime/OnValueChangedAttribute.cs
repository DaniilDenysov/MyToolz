using System;
using UnityEngine;

namespace MyToolz.EditorToolz
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class OnValueChangedAttribute : PropertyAttribute
    {
        public string MethodName { get; }
        public bool IncludeChildren { get; }
        public bool InvokeOnInitialDraw { get; }

        public OnValueChangedAttribute(
            string methodName,
            bool includeChildren = true,
            bool invokeOnInitialDraw = false)
        {
            MethodName = methodName ?? string.Empty;
            IncludeChildren = includeChildren;
            InvokeOnInitialDraw = invokeOnInitialDraw;
        }
    }
}
