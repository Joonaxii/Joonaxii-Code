using Joonaxii.MathJX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Joonaxii.Collections
{
    public class BufferList : PinnableList<byte>
    {
        private const int MAX_STACK_ALLOC = 8192 << 3;
        protected override int Padding => 8;

        public override int Count { get => _countVal; }
        public int ByteSize { get => _count; }
        public byte BytesPerValue { get; private set; }
      
        private int _countVal;

        public new ulong this[int i]
        {
            get => ReadValueAt(i * BytesPerValue);
            set => WriteValueAt(i * BytesPerValue, value);
        }

        public BufferList() : this(1, 64) { }
        public BufferList(byte bytesPerValue) : this(bytesPerValue, 64) { }
        public BufferList(byte bytesPerValue, int capacity) : base(capacity * bytesPerValue)
        {
            if (bytesPerValue < 1 | bytesPerValue > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(bytesPerValue),
                  "Value byte size is out of range! (Min 1, Max 8)");
            }

            BytesPerValue = bytesPerValue;
            _count = 0;
            _countVal = 0;
        }

        public BufferList(BufferList other) : this(other, 1) { }
        public BufferList(BufferList other, byte bytesPerValue)
        {
            BytesPerValue = bytesPerValue;

            if (other.BytesPerValue == bytesPerValue)
            {
                _capacity = other._capacity;
                _capacityPad = _capacity + Padding;
                _items = new byte[_capacityPad];

                BufferUtils.Memcpy(other._items, _items, _capacity);
                _count = other._count;
                return;
            }

            _capacity = (other._capacity / other.BytesPerValue) * BytesPerValue;
            _capacityPad = _capacity + Padding;
            _items = new byte[_capacityPad];
            _count = other._count;
            _countVal = other._countVal;

            Pin();
            for (int i = 0; i < other.Count; i++)
            {
                WriteValueAt(i * BytesPerValue, other[i]);
            }
            UnPin();
        }

        public BufferList(Stream stream, byte bytesPerValue, int bytesToRead)
        {
            BytesPerValue = bytesPerValue;

            _capacity = bytesToRead;
            _capacityPad = _capacity + Padding;
            _items = new byte[_capacityPad];
            _count = bytesToRead;
            _countVal = bytesToRead / bytesPerValue;

            stream.Read(_items, 0, _count);
        }

        public BufferList(IList<byte> other, byte bytesPerValue)
        {
            FromIList(other.Count, bytesPerValue);
            Pin();
            for (int i = 0; i < other.Count; i++)
            {
                WriteValueAt(i * BytesPerValue, other[i]);
            }
            UnPin();
        }
        public BufferList(IList<short> other, byte bytesPerValue)
        {
            FromIList(other.Count, bytesPerValue);
            Pin();
            for (int i = 0, j = 0; i < other.Count; i++, j+=BytesPerValue)
            {
                WriteValueAt(j, (ushort)other[i]);
            }
            UnPin();
        }
        public BufferList(IList<int> other, byte bytesPerValue)
        {
            FromIList(other.Count, bytesPerValue);
            Pin();
            for (int i = 0; i < other.Count; i++)
            {
                WriteValueAt(i * BytesPerValue, (uint)other[i]);
            }
            UnPin();
        }
        public BufferList(IList<long> other, byte bytesPerValue)
        {
            FromIList(other.Count, bytesPerValue);
            Pin();
            for (int i = 0; i < other.Count; i++)
            {
                WriteValueAt(i * BytesPerValue, (ulong)other[i]);
            }
            UnPin();
        }

        private void FromIList(int count, byte bpv)
        {
            BytesPerValue = bpv;

            _capacity = Maths.NextPowerOf2(count * bpv);
            _capacityPad = _capacity + Padding;

            _items = new byte[_capacityPad];
            _count = count * bpv;
            _countVal = count;
        }

        public void SetValueSize(byte bytesPerValue)
        {
            if (bytesPerValue == BytesPerValue | _count < 1)
            {
                BytesPerValue = bytesPerValue;
                return;
            }

            byte prev = BytesPerValue;
            int newLen = _countVal * bytesPerValue;
            ValidateBuffer(newLen, true);
            BytesPerValue = bytesPerValue;
            _count = newLen;

            unsafe
            { 
                bool isPinned = IsPinned;
                if (!isPinned) { Pin(); }

                byte* ptrTemp;
                bool onHeap = _items.Length > MAX_STACK_ALLOC;

                if (onHeap)
                {
                    ptrTemp = (byte*)Marshal.AllocHGlobal(_items.Length);
                }
                else
                {
                    byte* ptrT = stackalloc byte[_items.Length];
                    ptrTemp = ptrT;
                }
                BufferUtils.Memcpy(ptrTemp, RawPointer, _items.Length);
                BytesPerValue = BytesPerValue;

                for (int i = 0; i < _countVal; i++)
                {
                    WriteValueAt(i, ConvertBytes(prev, ptrTemp));
                    ptrTemp += prev;
                }

                if (!isPinned)
                {
                    UnPin();
                }

                if (onHeap)
                {
                    Marshal.FreeHGlobal((IntPtr)ptrTemp);
                }
            }

            if (prev > bytesPerValue)
            {
                Trim(true);
            }
        }

        private int GetPowOf2Len(int count)
        {
            if ((count & (count - 1)) == 0) { return count; }

            int p = 1;
            while (p < count) { p <<= 1; }
            return p;
        }

        public void WriteToStream(Stream stream, bool clear)
        {
            stream.Write(_items, 0, ByteSize);
            if (clear) { Clear(); }
        }

        public override void Clear()
        {
            base.Clear();
            _countVal = 0;
        }

        public void Add(double item, bool signed)
        {
            ValidateBuffer(_count + BytesPerValue, true);
            ulong val;

            if (signed)
            {
                item++;
                item *= 0.5f;
            }

            switch (BytesPerValue)
            {
                default: val =(ulong)(0xFE * item); break;
                case 2: val = (ulong)(0xFF_FE * item); break;
                case 3: val = (ulong)(0xFF_FF_FE * item); break;
                case 4: val = (ulong)(0xFF_FF_FF_FE * item); break;
                case 5: val = (ulong)(0xFF_FF_FF_FF_FE * item); break;
                case 6: val = (ulong)(0xFF_FF_FF_FF_FFF_FE * item); break;
                case 7: val = (ulong)(0xFF_FF_FF_FF_FF_FF_FE * item); break;
                case 8: val = (ulong)(0xFF_FF_FF_FF_FF_FF_FF_FE * item); break;
            }

            WriteValueAt(_count, val);
            _count+=BytesPerValue;
            _countVal++;
        }

        public override void Add(byte item) => Add(item);
        public void Add(ulong item)
        {
            ValidateBuffer(_count + BytesPerValue, true);

            WriteValueAt(_count, item);
            _count+=BytesPerValue;
            _countVal++;
        }

        public bool Contains(ulong item) => IndexOf(item) >= 0;

        public int IndexOf(ulong item)
        {
            if (BytesPerValue == 1)
            {
                unsafe
                {
                    return base.IndexOf(*(byte*)item);
                }
            }

            int ii = 0;
            int len = Count * BytesPerValue;
            while (ii < len)
            {
                ulong value = ReadValueAt(ii);
                if (value == item) { return ii / BytesPerValue; }
                ii += BytesPerValue;
            }
            return -1;
        }

        public void Insert(int index, ulong item)
        {
            if (index == _countVal) { Add(item); return; }

            if (index < 0 | index > Count) { throw new IndexOutOfRangeException("Index out of range for buffer!"); }
            int bI = index * BytesPerValue;

            ValidateBuffer((_countVal + 1) * ByteSize, true);

            int endHalf = _count - bI;
            unsafe
            {
                if (IsPinned)
                {
                    BufferUtils.Memshft(RawPointer + bI, BytesPerValue, endHalf);
                }
                else
                {
                    fixed(byte* ptr = _items)
                    {
                        BufferUtils.Memshft(ptr + bI, BytesPerValue, endHalf);
                    }
                }
            }
            WriteValueAt(bI, item);

            _count += BytesPerValue;
            _countVal++;
        }

        /// <summary>
        /// Reads a value at an index from the internal byte buffer and returns a value
        /// based on the BytesPerValue variable.
        /// </summary>
        /// <param name="index">Index to read from, assumed to be in "byte space".</param>
        private unsafe ulong ReadValueAt(int index)
        {
            //If we're pinned, just pass the RawPointer + the index to ConvertBytes
            if (IsPinned) { return ConvertBytes(BytesPerValue, RawPointer + index); }

            //If we're not pinned, pin the internal buffer 
            //and pass the pointer + the index to ConvertBytes
            fixed (byte* ptr = _items)
            {
                return ConvertBytes(BytesPerValue, ptr + index);
            }
        }

        private unsafe ulong ConvertBytes(int bpv, byte* value)
        {
            switch (bpv)
            {
                default: return *(ulong*)value & 0xFF;
                case 2:  return *(ulong*)value & 0xFF_FF;
                case 3:  return *(ulong*)value & 0xFF_FF_FF;
                case 4:  return *(ulong*)value & 0xFF_FF_FF_FF;
                case 5:  return *(ulong*)value & 0xFF_FF_FF_FF_FF;
                case 6:  return *(ulong*)value & 0xFF_FF_FF_FF_FF_FF;
                case 7:  return *(ulong*)value & 0xFF_FF_FF_FF_FF_FF_FF;
                case 8:  return *(ulong*)value;
            }
        }

        private unsafe void WriteValueAt(int index, ulong value)
        {
            int v = BytesPerValue;
            if (IsPinned)
            {
                var ptr = RawPointer + index;
                while (v-- > 0)
                {
                    *ptr++ = (byte)(value & 0xFFUL);
                    value >>= 8;
                }
                return;
            }

            int ii = index;
            while (v-- > 0)
            {
                _items[ii++] = (byte)(value & 0xFFUL);
                value >>= 8;
            }
        }

        public bool Remove(ulong item)
        {
            int id = IndexOf(item);
            if (id >= 0)
            {
                RemoveAt(id);
                return true;
            }
            return false;
        }

        public override void RemoveAt(int index)
        {
            index *= BytesPerValue;
            if (index < 0 | index >= Count) { throw new IndexOutOfRangeException(); }

            if (BytesPerValue == 1)
            {
                base.RemoveAt(index);
                return;
            }
            base.RemoveRange(index, BytesPerValue);
        }
    }
}
