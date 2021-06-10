using Joonaxii.MathX;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Joonaxii.IO
{
    public struct Bit256
    {
        public static readonly Bit256 minValue = new Bit256();
        public static readonly Bit256 maxValue = new Bit256(256);

        private ulong _a;
        private ulong _b;
        private ulong _c;
        private ulong _d;

        public Bit256(int bits)
        {
            _a = _b = _c = _d = 0;
            for (int i = 0; i < Math.Min(bits, 256); i++)
            {
                SetBit((byte)i, true);
            }
        }

        public Bit256(bool[] bits)
        {
            _a = _b = _c = _d = 0;
            for (int i = 0; i < Math.Min(bits.Length, 256); i++)
            {
                SetBit((byte)i, bits[i]);
            }
        }

        public Bit256(params Range[] bitRanges)
        {
            _a = _b = _c = _d = 0;
            for (int i = 0; i < bitRanges.Length; i++)
            {
                var r = bitRanges[i];
                for (int j = r.start; j < r.end; j++)
                {
                    if(j < 0) { continue; }
                    if(j > 255) { break; }

                    SetBit((byte)j, true);
                }
            }
        }

        public Bit256(params byte[] bits)
        {
            _a = _b = _c = _d = 0;
            for (int i = 0; i < bits.Length; i++)
            {
                SetBit(bits[i], true);
            }
        }

        public void SetBit(byte i, bool value)
        {
            if (i > 191) { _d = _d.SetBit(i - 192, value); return; }
            if (i > 127) { _c = _c.SetBit(i - 128, value); return; }
            if (i > 63)  { _b = _b.SetBit(i - 64, value); return; }
            _a = _a.SetBit(i, value);
        }

        public bool IsBitSet(int i)
        {
            if (i > 191) { return _d.IsBitSet(i - 192); }
            if (i > 127) { return _c.IsBitSet(i - 128); }
            if (i > 63)  { return _b.IsBitSet(i - 64); }
            return _a.IsBitSet(i);
        }
       
        public bool IsBitSet(byte i)
        {
            if (i > 191) { return _d.IsBitSet(i - 192); }
            if (i > 127) { return _c.IsBitSet(i - 128); }
            if (i > 63)  { return _b.IsBitSet(i - 64); }
            return _a.IsBitSet(i);
        }

        public bool IsBitSet(char i)
        {
            if (i > 191) { return _d.IsBitSet(i - 192); }
            if (i > 127) { return _c.IsBitSet(i - 128); }
            if (i > 63) { return _b.IsBitSet(i - 64); }
            return _a.IsBitSet(i);
        }

        public string ToString(char separator, bool addLetters)
        {
            if (addLetters) { return $"[D]: {Convert.ToString((long)_d, 2).PadLeft(64, '0')}{separator}[C]: {Convert.ToString((long)_c, 2).PadLeft(64, '0')}{separator}[B]: {Convert.ToString((long)_b, 2).PadLeft(64, '0')}{separator}[A]: {Convert.ToString((long)_a, 2).PadLeft(64, '0')}"; }
            return $"{Convert.ToString((long)_d, 2).PadLeft(64, '0')}{separator}{Convert.ToString((long)_c, 2).PadLeft(64, '0')}{separator}{Convert.ToString((long)_b, 2).PadLeft(64, '0')}{separator}{Convert.ToString((long)_a, 2).PadLeft(64, '0')}";
        }

        public override string ToString() => ToString(' ', false);

        public HashSet<T> ToHasSet<T>()
        {
            HashSet<T> set = new HashSet<T>();
            Type t = typeof(T);
            for (int i = 0; i < 256; i++)
            {
                if (!IsBitSet(i)) { continue; }
                set.Add((T)Convert.ChangeType(i, t));
            }
            return set;
        }
    }
}