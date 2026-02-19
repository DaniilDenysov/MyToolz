using System;
using UnityEngine;

namespace MyToolz.EditorToolz
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class ShowIfAttribute : PropertyAttribute
    {
        public string MemberName { get; }
        public string CompareValue { get; }

        public ShowIfAttribute(string memberName)
        {
            MemberName = memberName;
        }

        public ShowIfAttribute(string memberName, string compareValue)
        {
            MemberName = memberName;
            CompareValue = compareValue;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class HideIfAttribute : PropertyAttribute
    {
        public string MemberName { get; }
        public string CompareValue { get; }

        public HideIfAttribute(string memberName)
        {
            MemberName = memberName;
        }

        public HideIfAttribute(string memberName, string compareValue)
        {
            MemberName = memberName;
            CompareValue = compareValue;
        }
    }
}
