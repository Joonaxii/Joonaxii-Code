using System;

using System.Collections.Generic;

namespace Joonaxii.Text.Compression
{
    public class PriorityQueue<T> where T : IComparable<T>
    {
        private T[] _heap;
        private IComparer<T> _comparer;

        public int Count { get; private set; }

        public PriorityQueue(ICollection<T> collection, IComparer<T> comparer = null)
        {
            _comparer = comparer == null ? Comparer<T>.Default : _comparer;
            _heap = new T[collection.Count];
            foreach (var item in collection)
            {
                Enqueue(item, true);
            }
            Array.Sort(_heap, 0, Count, _comparer);
        }

        public PriorityQueue() : this(32) { }
        public PriorityQueue(int capacity)
        {
            _heap = new T[capacity];
        }

        public void Enqueue(T val, bool doNotSort = false)
        {
            if(_heap.Length <= Count) { Array.Resize(ref _heap, Count * 2); }
            _heap[Count] = val;
            Count++;

            if (doNotSort)
            {
                Array.Sort(_heap, 0, Count, _comparer);
            }
        }

        public T Dequeue()
        {
            var value = Peek();
            Array.ConstrainedCopy(_heap, 1, _heap, 0, --Count);
            return value;
        }

        public T Peek() => Count > 0 ? _heap[0] : default(T);

        public T PeekAt(int index)
        {
            if(index < 0 || index >= Count) { return default(T); }
            return _heap[index];
        }
    }
}