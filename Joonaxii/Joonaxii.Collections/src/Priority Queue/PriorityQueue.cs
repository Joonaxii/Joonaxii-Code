using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Joonaxii.Collections.PriorityQueue
{
    public class PriorityQueue<T> where T : IPriorityQueueNode
    {
        public int Count { get => _heapSize; }

        private T[] _heap;
        private int _heapSize = 0;
        private bool _isMinHeap;

        public PriorityQueue(bool isMinHeap, ICollection<T> collection)
        {
            _isMinHeap = isMinHeap;
            _heap = new T[collection.Count + 1];
            _heapSize = 0;
            foreach (var item in collection)
            {
                Enqueue(item);
            }
        }

        public PriorityQueue() : this(true, 32) { }
        public PriorityQueue(bool isMinHeap) : this(isMinHeap, 32) { }
        public PriorityQueue(bool isMinHeap, int capacity)
        {
            _isMinHeap = isMinHeap;
            _heap = new T[capacity + 1];
        }

        public void Enqueue(T val)
        {
            if (_heap.Length <= (_heapSize + 1)) { Array.Resize(ref _heap, Count * 2 + 1); }

            _heapSize++;
            _heap[_heapSize] = val;

            if (_isMinHeap) { BuildHeapMin(_heapSize); return; }
            BuildHeapMax(_heapSize);
        }

        public T Dequeue()
        {
            T ret = _heap[1];
            _heap[1] = _heap[_heapSize];
            _heap[_heapSize] = default(T);

            _heapSize--;

            if (_isMinHeap)
            {
                MinHeapify(1);
                return ret; 
            }
            MaxHeapify(1);
            return ret;
        }

        public T Top() => _heapSize > 0 ? _heap[1] : default(T);

        private void Swap(int i, int j)
        {
            var temp = _heap[i];
            _heap[i] = _heap[j];
            _heap[j] = temp;
        }

        private void BuildHeapMin(int i)
        {
            while (i > 1)
            {
                int parent = i >> 1;
                if (_heap[i].Priority < _heap[parent].Priority)
                {
                    Swap(i, parent);
                }
                i = parent;
            }
        }

        private void BuildHeapMax(int i)
        {
            while (i > 1)
            {
                int parent = i >> 1;
                if (_heap[i].Priority > _heap[parent].Priority)
                {
                    Swap(i, parent);
                }
                i = parent;
            }
        }

        private void MaxHeapify(int i)
        {
            int left = i << 1;
            if(_heapSize < left) { return; }
            if(_heapSize == left)
            {
                if(_heap[i].Priority < _heap[left].Priority)
                {
                    Swap(i, left);
                }
                return;
            }

            int right = left + 1;
            int small = _heap[left].Priority > _heap[right].Priority ? left : right;

            if(_heap[i].Priority < _heap[small].Priority)
            {
                Swap(i, small);
            }

            i = small;
            while (i < _heapSize)
            {
                left = i << 1;
                if (_heapSize < left) { break; }

                if (_heapSize == left)
                {
                    if (_heap[i].Priority < _heap[left].Priority)
                    {
                        Swap(i, left);
                    }
                    break;
                }

                right = left + 1;
                small = _heap[left].Priority > _heap[right].Priority ? left : right;

                if (_heap[i].Priority < _heap[small].Priority)
                {
                    Swap(i, small);
                }
                i = small;
            }
        }

        private void MinHeapify(int i)
        {
            int left = i << 1;
            if(_heapSize < left) { return; }
            if(_heapSize == left)
            {
                if(_heap[i].Priority > _heap[left].Priority)
                {
                    Swap(i, left);
                }
                return;
            }

            int right = left + 1;
            int small = _heap[left].Priority < _heap[right].Priority ? left : right;

            if(_heap[i].Priority > _heap[small].Priority)
            {
                Swap(i, small);
            }

            i = small;
            while (i < _heapSize)
            {
                left = i << 1;
                if (_heapSize < left) { break; }

                if (_heapSize == left)
                {
                    if (_heap[i].Priority > _heap[left].Priority)
                    {
                        Swap(i, left);
                    }
                    break;
                }

                right = left + 1;
                small = _heap[left].Priority < _heap[right].Priority ? left : right;

                if (_heap[i].Priority > _heap[small].Priority)
                {
                    Swap(i, small);
                }
                i = small;
            }
        }

        public void PrintQueue(StringBuilder sb)
        {
            sb.AppendLine($"Queue Heap Size: {_heapSize - 1}");
            for (int i = 1; i <= _heapSize; i++)
            {
                sb.AppendLine($"   -{_heap[i]}");
            }
        }
    }
}