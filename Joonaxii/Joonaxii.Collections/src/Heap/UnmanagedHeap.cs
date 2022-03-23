using Joonaxii.MathJX;
using System;
using System.Runtime.InteropServices;

namespace Joonaxii.Collections
{
    public unsafe class UnmanagedHeap<T> : IDisposable where T : unmanaged
    {
        internal const int DEFAULT_REG_SIZE = 64;

        public T* HeapPtr { get => _heapPtr; }
        public int Capacity { get => _heapCapacity; }

        private IntPtr _heap = IntPtr.Zero;
        private int _heapTailPos = 0;
        private T* _heapPtr = null;
        private int _heapCapacity;

        public UnmanagedHeapRegion* Regions { get => _heapRegions; }
        private UnmanagedHeapRegion* _heapRegions;
        private int _originalRegCapacity;
        private int _regionCount;

        public T this[int i]
        {
            get => *(_heapPtr + i);
            set => *(_heapPtr + i) = value;
        }

        public T this[int i, int j]
        {
            get => *(T*)(_heapRegions[i].Pointer + j);
            set => *(T*)(_heapRegions[i].Pointer + j) = value;
        }

        public UnmanagedHeap(int heapRegions) : this(heapRegions, heapRegions * DEFAULT_REG_SIZE, DEFAULT_REG_SIZE) { }
        public UnmanagedHeap(int heapRegions, int initialRegionSize) : this(heapRegions, heapRegions * initialRegionSize, initialRegionSize) { }
        internal UnmanagedHeap(int heapRegions, int capacity, int initialRegionSize)
        {
            _originalRegCapacity = initialRegionSize;
            _heapCapacity = capacity;

            _regionCount = heapRegions;
            _heapRegions = (UnmanagedHeapRegion*)Marshal.AllocHGlobal(heapRegions * sizeof(UnmanagedHeapRegion));

            _heap = Marshal.AllocHGlobal(_heapCapacity * sizeof(T));
            _heapPtr = (T*)_heap;

            var hp = _heapRegions;
            int regS = initialRegionSize * sizeof(T);
            byte* heapBtr = (byte*)_heapPtr;
            for (int i = 0; i < heapRegions; i++)
            {
                *hp++ = new UnmanagedHeapRegion(heapBtr, i * regS, 0, initialRegionSize, regS);
            }
            _heapTailPos = capacity;
        }
        ~UnmanagedHeap() { Free(); }

        public UnmanagedHeapRegion* GetRegion(int i) => &_heapRegions[i];

        public void AddToRegion(int region, T value)
        {
            var reg = _heapRegions + region;
            T* ptr = (T*)reg->Pointer;

            if (reg->Length >= reg->Capacity)
            {
                var cap = reg->Capacity;
                var bCap = reg->ByteCapacity;
                var newCap = cap << 1;

                int diff = newCap - cap;
                if (ValidateBuffer(_heapTailPos + diff))
                {
                    ReadjustRegionPointers();
                    ptr = (T*)reg->Pointer;
                }

                reg->SetCapacity(newCap, sizeof(T));

                var nReg = reg + 1;
                int diffS = diff * sizeof(T);

                byte* heapBtr = (byte*)_heapPtr;
                BufferUtils.Memshft(reg->End, diffS, (_heapTailPos * sizeof(T)) - (reg->Index + bCap));

                int len = _regionCount - region - 1;
                while (len-- > 0)
                {
                    nReg++->ShiftIndex(heapBtr, diffS);
                }
                _heapTailPos += diff;
            }
            *(ptr + reg->Length++) = value;
        }

        public void RestoreToDefault()
        {
            Resize(_originalRegCapacity * _regionCount);

            FindHeapTail();
            ReadjustRegionPointers();
        }

        public void ClearRegions()
        {
            UnmanagedHeapRegion* hPtr = _heapRegions;
            for (int i = 0; i < _regionCount; i++)
            {
                hPtr++->Length = 0;
            }
        }

        public void TrimHeap(HeapTrimFlags flags)
        {
            int newCapacity = 0;
            var hp = _heapRegions;
            var hpPrev = _heapRegions - 1;

            bool forceP2 = (flags & HeapTrimFlags.ForceP2Regions) != 0;
            bool retain = (flags & HeapTrimFlags.Retain) != 0;

            byte* heapPtr = (byte*)_heapPtr;
            for (int i = 0; i < _regionCount; i++, hp++, hpPrev++)
            {
                int cap = hp->Length < _originalRegCapacity ? _originalRegCapacity : hp->Length;
                if (forceP2)
                {
                    cap = Maths.NextPowerOf2Bitwise(cap);
                }

                if (i != 0 & retain)
                {
                    BufferUtils.Memcpy(hp->Pointer, hpPrev->End, sizeof(T) * hp->Length);
                }

                hp->SetCapacity(cap, sizeof(T));
                hp->SetIndex(heapPtr, newCapacity);
                newCapacity += cap;
            }
            _heapTailPos = newCapacity;

            if ((flags & HeapTrimFlags.ForcePowOf2) != 0 & !forceP2)
            {
                newCapacity = Maths.NextPowerOf2Bitwise(newCapacity);
            }
            Resize(newCapacity);
        }

        private bool ValidateBuffer(int required)
        {
            if (required <= _heapCapacity) { return false; }
            int heap = _heapCapacity;
            while (heap < required)
            {
                heap <<= 1;
            }
            return Resize(heap);
        }

        private bool Resize(int newCapacity)
        {
            if(newCapacity == _heapCapacity) { return false; }

            _heapCapacity = newCapacity;
            _heap = Marshal.ReAllocHGlobal(_heap, new IntPtr(_heapCapacity * sizeof(T)));
            _heapPtr = (T*)_heap;
            return true;
        }

        private void Free()
        {
            if (_heapRegions != null)
            {
                Marshal.FreeHGlobal((IntPtr)_heapRegions);
                _heapRegions = null;
            }

            if (_heap != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_heap);
                _heap = IntPtr.Zero;
                _heapPtr = null;
            }
        }

        private void ReadjustRegionPointers()
        {
            var hp = _heapRegions;
            byte* heap = (byte*)_heapPtr;
            int ind = 0;
            for (int i = 0; i < _regionCount; i++, hp++)
            {
                hp->SetIndex(heap, ind);
                ind += hp->ByteCapacity;
            }
        }

        private void FindHeapTail()
        {
            _heapTailPos = 0;
            var hp = _heapRegions;
            for (int i = 0; i < _regionCount; i++, hp++)
            {
                _heapTailPos += hp->Capacity;
            }
        }

        public void Dispose()
        {
            Free();
        }
    }
}
