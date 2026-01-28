using System;
using UnityEngine;

namespace MyToolz.EditorToolz
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class RequiredAttribute : PropertyAttribute
    {
        public readonly string Message;

        public RequiredAttribute(string message = null)
        {
            Message = message;
        }
    }
}
