
namespace Joonaxii.Collections.Unmanaged
{
    public unsafe class FixedUnmanagedHeap<T> where T : unmanaged
    {
        public bool IsValid { get => _heap != null & _heapPtrs != null; }

        public T* HeapPtr { get => _heap; }
        public T this[int h, int i] 
        { 
            get => IsValid ? *(_heap + _heapPtrs[h].start + i) : default; 
            set
            {
                if(!IsValid)
                *(_heap + _heapPtrs[h].start + i) = value;
            }
        }

        private T* _heap;
        private int _heapUsed;
        private int _heapCapacity;

        private FixedHeapPointer* _heapPtrs;
        private int _heapPointers = 0;

        public FixedUnmanagedHeap(int initHeapCapacity)
        {
            _heapUsed = 0;
            _heapPointers = 0;
            _heapPtrs = null;

        }
    }
}
