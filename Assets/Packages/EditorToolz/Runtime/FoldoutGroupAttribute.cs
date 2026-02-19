using System;

namespace MyToolz.EditorToolz
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class FoldoutGroupAttribute : Attribute
    {
        public string GroupName { get; }
        public bool ExpandedByDefault { get; }

        public FoldoutGroupAttribute(string groupName, bool expandedByDefault = true)
        {
            GroupName = groupName ?? string.Empty;
            ExpandedByDefault = expandedByDefault;
        }
    }
}
