using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Joonaxii.Collections
{
    public class BufferList : IList<long>
    {
        public int  Count         { get; private set; }
        public int  Capacity      { get; private set; }
        public int  ByteSize      { get => Count * BytesPerValue; }
        public byte BytesPerValue { get; private set; }

        private byte[] _buffer = null;

        public long this[int i] 
        {
            get => ReadValueAt(i * BytesPerValue);
            set => WriteValueAt(i * BytesPerValue, value);
        }

        public bool IsReadOnly => false;

        public BufferList() : this(1, 64) { }
        public BufferList(byte bytesPerValue) : this(bytesPerValue, 64) { }
        public BufferList(byte bytesPerValue, int capacity)
        {
            if(bytesPerValue < 1 | bytesPerValue > 8) 
            { throw new ArgumentOutOfRangeException(nameof(bytesPerValue), 
                "Value byte size is out of range! (Min 1, Max 8)"); }

            BytesPerValue = bytesPerValue;
            Capacity = (capacity < 1 ? 1 : capacity) * bytesPerValue;

            _buffer = new byte[Capacity];
            Count = 0;
        }

        public BufferList(BufferList other) : this(other, 1) { }
        public BufferList(BufferList other, byte bytesPerValue)
        {
            BytesPerValue = bytesPerValue;
            if (other.BytesPerValue == bytesPerValue)
            {
                _buffer = new byte[Capacity = other.Capacity];
                Count = other.Count;
                Buffer.BlockCopy(other._buffer, 0, _buffer, 0, Capacity);
                return;
            }

            Capacity = (other.Capacity / other.BytesPerValue) * BytesPerValue;
            _buffer = new byte[Capacity];
            Count = other.Count;

            for (int i = 0; i < other.Count; i++)
            {
                WriteValueAt(i * BytesPerValue, other[i]);
            }
        }

        public BufferList(IList<byte> other, byte bytesPerValue)
        {
            BytesPerValue = bytesPerValue;
            _buffer = new byte[Capacity = other.Count * 2];
            Count = other.Count;
            for (int i = 0; i < other.Count; i++)
            {
                WriteValueAt(i * BytesPerValue, other[i]);
            }
        }
        public BufferList(IList<short> other, byte bytesPerValue)
        {
            BytesPerValue = bytesPerValue;
            _buffer = new byte[Capacity = GetPowOf2Len(other.Count * BytesPerValue)];
            Count = other.Count;
            for (int i = 0; i < other.Count; i++)
            {
                WriteValueAt(i * BytesPerValue, other[i]);
            }
        }
        public BufferList(IList<int> other, byte bytesPerValue)
        {
            BytesPerValue = bytesPerValue;
            _buffer = new byte[Capacity = GetPowOf2Len(other.Count * BytesPerValue)];
            Count = other.Count;
            for (int i = 0; i < other.Count; i++)
            {
                WriteValueAt(i * BytesPerValue, other[i]);
            }
        }
        public BufferList(IList<long> other, byte bytesPerValue)
        {
            BytesPerValue = bytesPerValue;
            _buffer = new byte[Capacity = GetPowOf2Len(other.Count * BytesPerValue)];
            Count = other.Count;
            for (int i = 0; i < other.Count; i++)
            {
                WriteValueAt(i * BytesPerValue, other[i]);
            }
        }

        public void SetValueSize(byte bytesPerValue)
        {
            if(bytesPerValue == BytesPerValue | Count < 1) 
            {
                BytesPerValue = bytesPerValue; 
                return; 
            }

            int newLen = Count * bytesPerValue;
            ValidateBufferSize(newLen, true);

            byte prev = BytesPerValue;

            byte[] temp = new byte[_buffer.Length];
            Buffer.BlockCopy(_buffer, 0, temp, 0, temp.Length);

            BytesPerValue = BytesPerValue;
            for (int i = 0; i < Count; i++)
            {
                ulong val = 0;
                int startI = i * prev;
                for (int j = 0; j < prev; j++)
                {
                    val += ((ulong)temp[j + startI] << (j << 3));
                }
                WriteValueAt(i, (long)val);
            }
        }

        private int GetPowOf2Len(int count)
        {
            if((count & (count - 1)) == 0) { return count; }

            int p = 1;
            while(p < count) { p <<= 1; }
            return p;
        }

        public void WriteToStream(Stream stream, bool clear)
        {
            stream.Write(_buffer, 0, ByteSize);
            if (clear) { Clear(); }
        }

        public void Add(double item, bool signed)
        {
            ValidateBufferSize(Count + 1);
            long val;

            if (!signed) { item -= 0.5; }
            switch (BytesPerValue)
            {
                default:
                    ulong mx;
                    ulong signBit = 1UL << ((BytesPerValue << 3) - 1);
                    mx = (signBit - 1L);
                    ulong valU;

                    if (signed)
                    {
                        valU = (ulong)((mx - 1) * item);   
                        long valIn = (long)(item < 0 ? (~valU & mx) + 1 : (valU & mx));
                        val = item < 0 ? -valIn : valIn;
                        break;
                    }

                    val = (long)((mx << 1) * item);
                    break;

                case 1: val = (byte)((byte.MaxValue - 1) * item); break;
                case 2: val = (short)((ushort.MaxValue - 1) * item); break;
                case 4: val = (int)((uint.MaxValue - 1) * item); break;
                case 8: val = (long)((ulong.MaxValue - 1) * item); break;
            }
            WriteValueAt(Count * BytesPerValue, val);
            Count++;
        }


        public void Add(long item)
        {
            ValidateBufferSize(Count + 1);
            WriteValueAt(Count * BytesPerValue, item);
            Count++;
        }

        public void Clear()
        {
            Count = 0;
        }

        public bool Contains(long item) => IndexOf(item) >= 0;

        public void CopyTo(long[] array, int arrayIndex)
        {
            Buffer.BlockCopy(_buffer, 0, array, arrayIndex * 8, (array.Length - arrayIndex) * 8);
        }

        public void CopyTo(byte[] array, int arrayIndex)
        {
            Buffer.BlockCopy(_buffer, 0, array, arrayIndex, (array.Length - arrayIndex));
        }

        public int IndexOf(long item)
        {
            if (BytesPerValue == 1) { return Array.IndexOf(_buffer, (byte)item, 0, Count); }

            int ii = 0;
            int len = Count * BytesPerValue; 
            while (ii < len)
            {
                long value = ReadValueAt(ii);
                if(value == item) { return ii / BytesPerValue; }
                ii += BytesPerValue;
            }
            return -1;
        }

        public void Insert(int index, long item)
        {
            if(index == Count) { Add(item); return; }

            if(index < 0 | index > Count) { throw new IndexOutOfRangeException("Index out of range for buffer!"); }
            int bI = index * BytesPerValue;

            ValidateBufferSize(Count + 1);

            int countB = Count * BytesPerValue;
            int endHalf = countB - bI;

            Array.Copy(_buffer, bI, _buffer, bI + BytesPerValue, endHalf);
            WriteValueAt(bI, item);

            Count++;
        }
       
        private long ReadValueAt(int index)
        {
            ulong val = 0;
            for (int j = 0, i = 0; i < BytesPerValue; j += 8, i++)
            {
                val += ((ulong)_buffer[index + i] << j);
            }

            switch (BytesPerValue)
            {
                default:
                    ulong signBit = 1UL << ((BytesPerValue << 3) - 1);
                    ulong mask = (signBit - 1L);
                    bool isSigned = (val & (uint)signBit) != 0;

                    long valIn = (long)(isSigned ? (~val & mask) + 1 : (val & mask));
                    return isSigned ? -valIn : valIn;
                case 1: return (byte)val;
                case 2: return (short)val;         
                case 4: return (int)val;
                case 8: return (long)val;
            }
        }

        private void WriteValueAt(int index, long value)
        {
            ulong val = (ulong)value;
            for (int i = 0; i < BytesPerValue; i++)
            {
                _buffer[index + i] = (byte)((val >> (i << 3)) & 0xFFUL);
            }
        }

        private void ValidateBufferSize(int newCount, bool raw = false)
        {
            if (!raw)
            {
                newCount *= BytesPerValue;
            }

            int cap = Capacity;
            while (newCount >= cap)
            {
                cap <<= 1;
            }

            if(cap != Capacity) 
            {
                Capacity = cap;
                Array.Resize(ref _buffer, cap);
            }
        }

        public bool Remove(long item)
        {
            int id = IndexOf(item);
            if(id >= 0)
            {
                RemoveAt(id);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            if(index < 0 | index >= Count) { throw new IndexOutOfRangeException(); }
            int bI = index * BytesPerValue;
            int countB = Count * BytesPerValue;
            int endHalf = countB - bI - 1;

            Array.Copy(_buffer, bI + BytesPerValue, _buffer, bI, endHalf);
            Count--;
        }

        public IEnumerator<long> GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
