using Joonaxii.MathJX;
using System.Runtime.InteropServices;
using System;

namespace Joonaxii.Hashing
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public unsafe struct Bit128
    {
        public static readonly Bit256 minValue = new Bit256();
        public static readonly Bit256 maxValue = new Bit256(128);

        [FieldOffset(0)] private fixed byte _bytes[16];

        [FieldOffset(0)] private ulong _a;
        [FieldOffset(64)] private ulong _b;

        public Bit128(int bits)
        {
            _a = _b = 0;
            for (int i = 0; i < Math.Min(bits, 128); i++)
            {
                SetBit((byte)i, true);
            }
        }

        public Bit128(bool[] bits)
        {
            _a = _b = 0;
            for (int i = 0; i < Math.Min(bits.Length, 128); i++)
            {
                SetBit((byte)i, bits[i]);
            }
        }

        public Bit128(params RangeInt[] bitRanges)
        {
            _a = _b = 0;
            for (int i = 0; i < bitRanges.Length; i++)
            {
                var r = bitRanges[i];
                for (int j = r.start; j < r.end; j++)
                {
                    if (j < 0) { continue; }
                    if (j > 255) { break; }

                    SetBit((byte)j, true);
                }
            }
        }

        public Bit128(params byte[] bits)
        {
            _a = _b = 0;
            for (int i = 0; i < bits.Length; i++)
            {
                SetBit(bits[i], true);
            }
        }

        public void SetBit(byte i, bool value)
        {
            if (i > 63) { _b = _b.SetBit(i - 64, value); return; }
            _a = _a.SetBit(i, value);
        }

        public bool IsBitSet(int i)
        {
            if (i > 63) { return _b.IsBitSet(i - 64); }
            return _a.IsBitSet(i);
        }

        public bool IsBitSet(byte i)
        {
            if (i > 63) { return _b.IsBitSet(i - 64); }
            return _a.IsBitSet(i);
        }

        public bool IsBitSet(char i)
        {
            if (i > 63) { return _b.IsBitSet(i - 64); }
            return _a.IsBitSet(i);
        }

        public string ToString(char separator, bool addLetters)
        {
            if (addLetters) { return $"{separator}[B]: {Convert.ToString((long)_b, 2).PadLeft(64, '0')}{separator}[A]: {Convert.ToString((long)_a, 2).PadLeft(64, '0')}"; }
            return $"{separator}{Convert.ToString((long)_b, 2).PadLeft(64, '0')}{separator}{Convert.ToString((long)_a, 2).PadLeft(64, '0')}";
        }

        public override string ToString() => ToString(' ', false);

        public void CopyTo(byte[] bytes)
        {
            int len = bytes.Length < 128 ? bytes.Length : 128;
            fixed (byte* b = _bytes)
            {
                for (int i = 0; i < len; i++)
                {
                    bytes[i] = b[i];
                }
            }
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[128];
            fixed (byte* b = _bytes)
            {
                for (int i = 0; i < 128; i++)
                {
                    bytes[i] = b[i];
                }
            }
            return bytes;
        }
    }
}