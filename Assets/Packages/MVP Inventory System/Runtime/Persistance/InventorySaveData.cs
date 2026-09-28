using System;
using System.Collections.Generic;

namespace MyToolz.InventorySystem.Persistance
{
    [Serializable]
    public class InventorySaveData
    {
        public List<ItemEntry> items = new List<ItemEntry>();

        [Serializable]
        public class ItemEntry
        {
            /// <summary>Stable id (ItemSO GUID). Absent in 1.x saves, which resolve by <see cref="itemName"/>.</summary>
            public string itemId;
            public string itemName;
            public uint amount;
            public int gridIndex;
        }
    }
}
