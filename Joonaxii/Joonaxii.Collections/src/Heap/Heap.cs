using System;
using System.Runtime.InteropServices;

namespace Joonaxii.Collections
{
    public class Heap<T> where T : struct
    {
        internal const int DEFAULT_INIT_SIZE = 1024;

        public T[] RawHeap { get => _heap; }
        private T[] _heap;
        private int _heapPos;

        private int _originalCapacity;

        public HeapRegion[] Regions { get => _heapRegions.GetItems(); }
        private StructList<HeapRegion> _heapRegions;
        private readonly int _size;

        public T this[int i]
        {
            get => _heap[i];
            set => _heap[i] = value;
        }

        public T this[int i, int j]
        {
            get => _heap[_heapRegions[i].Start + j];
            set => _heap[_heapRegions[i].Start + j] = value;
        }

        public Heap() : this(DEFAULT_INIT_SIZE) { }
        public Heap(int capacity)
        {
            _heapRegions = new StructList<HeapRegion>(256);
            _heap = new T[_originalCapacity = capacity];
            _size = Marshal.SizeOf<T>();
        }

        public HeapRegion GetRegion(int i) => _heapRegions[i];

        private void ValidateBuffer(int req)
        {
            int cap = _heap.Length;
            while (req > cap)
            {
                cap <<= 1;
            }

            if (_heap.Length != cap)
            {
                Array.Resize(ref _heap, cap);
            }
        }

        public int ReserveRegion(int count, int capacity)
        {
            ValidateBuffer(_heapPos + count);

            _heapRegions.Add(new HeapRegion(_heapPos, count, capacity));
            _heapPos += capacity;
            return _heapRegions.Count - 1;
        }

        public int UpdateRegion(int at, int count)
        {
            unsafe
            {
                fixed (HeapRegion* hPtr = _heapRegions.GetItems())
                {
                    var reg = hPtr + at;
                    if (reg->Capacity >= count)
                    {
                        reg->Length = count;
                        return at;
                    }

                    int cap;
                    cap = reg->Capacity;
                    while (cap < count)
                    {
                        cap <<= 1;
                    }
                    int from = reg->Start + reg->Capacity;

                    ValidateBuffer(_heapPos + (cap - reg->Capacity));
                    if (at < _heapRegions.Count - 1)
                    {
                        Buffer.BlockCopy(_heap, from * _size, _heap, (reg->Start + cap) * _size, (_heapPos - from) * _size);
                    }

                    reg->Capacity = cap;
                    reg->Length = count;
                    return at;
                }

            }
        }

        public void RestoreToDefault()
        {
            Array.Resize(ref _heap, _originalCapacity);
            Clear();
        }

        public void ClearRegions()
        {
            if (_heapRegions.Count < 1) { return; }
            unsafe
            {
                fixed (HeapRegion* heap = _heapRegions.GetItems())
                {
                    HeapRegion* hPtr = heap;
                    for (int i = 0; i < _heapRegions.Count; i++, hPtr++)
                    {
                        hPtr->Length = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Do not use yet, not implemented!
        /// </summary>
        /// <param name="flags"></param>
        public void TrimRegions(HeapTrimFlags flags) => throw new NotImplementedException("Not implemented yet!");

        public void Clear()
        {
            _heapPos = 0;
            _heapRegions.Clear();
        }
    }
}
