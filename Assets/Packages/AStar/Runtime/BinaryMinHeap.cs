using System;

namespace MyToolz.Algorithms.AStar
{
    public class BinaryMinHeap<T>
    {
        private T[] _items;
        private float[] _priorities;
        private int _count;

        public int Count => _count;

        public BinaryMinHeap(int capacity = 64)
        {
            capacity = Math.Max(capacity, 4);
            _items = new T[capacity];
            _priorities = new float[capacity];
        }

        public void Enqueue(T item, float priority)
        {
            if (_count == _items.Length)
                Grow();

            _items[_count] = item;
            _priorities[_count] = priority;
            SiftUp(_count);
            _count++;
        }

        public T Dequeue()
        {
            var item = _items[0];
            _count--;

            if (_count > 0)
            {
                _items[0] = _items[_count];
                _priorities[0] = _priorities[_count];
                SiftDown(0);
            }

            _items[_count] = default;
            return item;
        }

        public void Clear()
        {
            Array.Clear(_items, 0, _count);
            _count = 0;
        }

        private void Grow()
        {
            int newCap = _items.Length * 2;
            Array.Resize(ref _items, newCap);
            Array.Resize(ref _priorities, newCap);
        }

        private void SiftUp(int i)
        {
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (_priorities[i] >= _priorities[parent])
                    break;

                Swap(i, parent);
                i = parent;
            }
        }

        private void SiftDown(int i)
        {
            while (true)
            {
                int left = (i << 1) + 1;
                if (left >= _count)
                    break;

                int right = left + 1;
                int smallest = (right < _count && _priorities[right] < _priorities[left])
                    ? right
                    : left;

                if (_priorities[i] <= _priorities[smallest])
                    break;

                Swap(i, smallest);
                i = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            (_items[a], _items[b]) = (_items[b], _items[a]);
            (_priorities[a], _priorities[b]) = (_priorities[b], _priorities[a]);
        }
    }
}
