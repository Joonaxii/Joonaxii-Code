using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Joonaxii.MathJX;

namespace Joonaxii.Collections
{
    public class FixedBitArray
    {
        public int Capacity { get => _capacity; }
        public int Count { get => _bitCount; }

        private int _capacity;
        private int _bitCount;

        public bool this[int i]
        {
            get
            {
                int byteI = i >> 3;
                int bitI = 1 << (i - (byteI << 3));
                if (IsPinned)
                {
                    unsafe
                    {
                        return (*(_ptr + byteI) & bitI) != 0;
                    }
                }
                return (_data[byteI] & bitI) != 0;
            }
            set
            {
                int byteI = i >> 3;
                int bitI = 1 << (i - (byteI << 3));

                if (IsPinned)
                {
                    unsafe
                    {
                        if (value)
                        {
                            *(_ptr + byteI) |= (byte)bitI;
                        }
                        else
                        {
                            *(_ptr + byteI) &= (byte)~bitI;
                        }
                        return;
                    }
                }

                if (value)
                {
                    _data[byteI] |= (byte)bitI;
                }
                else
                {
                    _data[byteI] &= (byte)~bitI;
                }
            }
        }

        public bool IsPinned { get => _handle.IsAllocated; }

        public IntPtr Pointer { get => _intPtr; }
        public unsafe byte* RawPointer { get => _ptr; }

        private IntPtr _intPtr;
        private unsafe byte* _ptr;
        private byte[] _data;
        private GCHandle _handle;

        public FixedBitArray(int bits) : this(bits, false) { }

        public FixedBitArray(int bits, bool defaultValue)
        {
            _bitCount = bits;
            bits = bits < 8 ? 8 : bits;
            _capacity = Maths.NextDivisbleBy(bits, 8) >> 3;
            _data = new byte[_capacity];

            Pin();
            unsafe
            {
                BufferUtils.Memset(_ptr, (byte)(defaultValue ? 0xFF : 0xFF), _data.Length);
            }
            UnPin();
        }

        public IntPtr Pin()
        {
            if (IsPinned) { return _intPtr; }
            _handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
            _intPtr = _handle.AddrOfPinnedObject();

            unsafe
            {
                _ptr = (byte*)_intPtr;
            }
            return _intPtr;
        }

        public void UnPin()
        {
            if (!IsPinned) { return; }
            _handle.Free();
            _intPtr = IntPtr.Zero;
            unsafe
            {
                _ptr = null;
            }
        }

        public void Set(int i, bool value) => this[i] = value;

        public int FindNextOf(bool value, bool? set = null)
        {
            int id = -1;
            bool wasPinned = IsPinned;

            Pin();
            byte breakOut = 0;
            int len = 0;
            int bit = 0;
            unsafe
            {

                byte* ptr = _ptr;

                while (len++ < _capacity)
                {
                    byte b = *ptr++;
                    for (int i = 0; i < 8; i++)
                    {
                        if (bit >= _bitCount) { breakOut = 0x2; break; }
                        bit++;

                        if (((b & (1 << i)) != 0) == value)
                        {
                            bit = i;
                            breakOut = 0x1;
                            break;
                        }
                    }
                    if (breakOut != 0) { break; }
                }

                if (breakOut == 0x1)
                {
                    int byteI = (len - 1);
                    id = (byteI << 3) + bit;
                    if (set != null)
                    {
                        if (set.Value)
                        {
                            *(_ptr + byteI) |= (byte)(1 << bit);
                        }
                        else
                        {
                            *(_ptr + byteI) &= (byte)~(1 << bit);
                        }
                    }
                }
            }

            if (!wasPinned)
            {
                UnPin();
            }
            return id;
        }

        public void SetAll(bool value)
        {
            bool keepPin = IsPinned;
            Pin();

            unsafe
            {
                BufferUtils.Memset(_ptr, value ? (byte)1 : (byte)0, _data.Length);
            }

            if (!keepPin)
            {
                UnPin();
            }
        }
    }
}
