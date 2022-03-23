using System;
using System.Runtime.InteropServices;

namespace Joonaxii.Collections
{
    [StructLayout(LayoutKind.Sequential, Size = 12)]
    public struct HeapRegion : IEquatable<HeapRegion>
    {
        public int Start { get => _start; internal set => _start = value; }
        public int Length { get => _length; internal set => _length = value; }
        public int Capacity { get => _capacity; internal set => _capacity = value; }

        private int _start;
        private int _length;
        private int _capacity;

        public HeapRegion(int start, int length, int capacity)
        {
            _start = start;
            this._length = length;
            _capacity = capacity;
        } 
        public bool Equals(HeapRegion other) => _start == other._start & _length == other._length & _capacity == other._capacity;
    }
}