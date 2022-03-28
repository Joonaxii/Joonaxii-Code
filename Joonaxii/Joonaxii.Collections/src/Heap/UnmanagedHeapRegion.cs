using System;
using System.Runtime.InteropServices;

namespace Joonaxii.Collections
{
    [StructLayout(LayoutKind.Sequential, Size = 24)]
    public unsafe struct UnmanagedHeapRegion : IEquatable<UnmanagedHeapRegion>
    {
        public byte* Pointer { get => _ptr; }
        public byte* End { get => _ptr + _capacityByte; }

        public int Index { get => _index; }
        public int Length { get => _length; internal set => _length = value; }
        public int Capacity { get => _capacity; }
        public int ByteCapacity { get => _capacityByte; }

        #region Private Fields
        private byte* _ptr;
        private int _index;
        private int _length;
        private int _capacity;
        private int _capacityByte;
        #endregion

        public UnmanagedHeapRegion(byte* start, int index, int length, int capacity, int capacitySize)
        {
            _ptr = start + index;
            _capacityByte = capacitySize;

            _index = index;
            _length = length;
            _capacity = capacity;
        }

        internal void SetCapacity(int capacity, int size)
        {
            _capacity = capacity;
            _capacityByte = capacity * size;
        }

        internal void SetIndex(byte* heapStart, int index)
        {
            _ptr = heapStart + index;
            _index = index;
        }

        internal void ShiftIndex(byte* heapStart, int shift)
        {
            _index += shift;
            _ptr = heapStart + _index;
        }

        public override string ToString() => $"{_index}, {_length}, {_capacity}, {_capacityByte}";
        public bool Equals(UnmanagedHeapRegion other) => _index == other._index & _length == other._length & _capacity == other._capacity;
    }
}