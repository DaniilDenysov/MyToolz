using System;
using System.Collections.Generic;

namespace MyToolz.DesignPatterns.MVP.View
{
    public interface ICollectionView<T>
    {
        event Action<int> OnItemSelected;
        event Action<int> OnItemRemoved;
        void PopulateList(IReadOnlyList<T> items);
        void AddItem(T item);
        void RemoveItemAt(int index);
        void Clear();
    }
}
