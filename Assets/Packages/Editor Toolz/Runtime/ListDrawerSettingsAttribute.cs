using System;

namespace MyToolz.EditorToolz
{
    /// <summary>
    /// Marker for list/array fields that mirrors the most common members of Odin's
    /// <c>[ListDrawerSettings]</c> so existing call sites compile unchanged. Unity's
    /// default inspector already renders reorderable lists, so the settings here are
    /// honoured on a best-effort basis by the unified inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ListDrawerSettingsAttribute : Attribute
    {
        public bool DraggableItems = true;
        public bool ShowIndexLabels = false;
        public bool ShowPagingControls = true;
        public bool ShowItemCount = true;
        public bool HideAddButton = false;
        public bool HideRemoveButton = false;
        public bool Expanded = false;
        public bool IsReadOnly = false;
        public int NumberOfItemsPerPage = 0;
        public string ListElementLabelName;
        public string OnTitleBarGUI;
        public string CustomAddFunction;
        public string CustomRemoveElementFunction;
        public string CustomRemoveIndexFunction;
        public string ElementColor;
    }
}
