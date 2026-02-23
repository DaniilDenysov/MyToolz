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
            public string itemName;
            public uint amount;
            public int gridIndex;
        }
    }
}
