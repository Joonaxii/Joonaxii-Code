using System;
using System.Text;

namespace Joonaxii.IO
{
    public static class IOExtensions
    {
        public static int GetCharSize(this string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if(str[i] > byte.MaxValue) { return 2; }
            }
            return 1;
        }

        public static int NextPowerOf(int value, int power)
        {
            while(value % power != 0)
            {
                value++;
            }
            return value;
        }


        public static long NextPowerOf(long value, int power)
        {
            while(value % power != 0)
            {
                value++;
            }
            return value;
        }

        public static int BitsNeeded(int value) => (int)(Math.Log(value) / Math.Log(2)) + 1;

        public static int ReadInt(this byte[] data, int start, bool backwards = false)
        {
            int val = 0;

            if (backwards)
            {
                for (int i = 3; i >= 0; i--)
                {
                    val += (ushort)((data[i + start] << ((3 - i) * 8)));
                }
                return val;
            }

            for (int i = 0; i < 4; i++)
            {
                val += (data[i + start] << (i * 8));
            }
            return val;
        }

        public static ushort ReadUshort(this byte[] data, int start, bool backwards = false)
        {
            ushort val = 0;

            if (backwards)
            {
                for (int i = 1; i >= 0; i--)
                {
                    val += (ushort)((data[i + start] << ((1 - i) * 8)));
                }
                return val;
            }

            for (int i = 0; i < 2; i++)
            {
                val += (ushort)((data[i + start] << (i * 8)));
            }
            return val;
        }

        public static string GetAsHex(this string str, string separator = "")
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                char chr = str[i];
                sb.Append($"{Convert.ToString(chr, 16)}{separator}");
            }
            return sb.ToString();
        }

        public static bool IsBitSet(this ulong val, int bit) => (val & (1ul << bit)) != 0;
        public static ulong SetBit(this ulong input, int bitIndex, bool value)
        {
            if (value) { return input |= (1ul << bitIndex); }
            return input &= ~(1ul << bitIndex);
        }

        public static bool IsBitSet(this ulong val, char bit) => (val & (1ul << bit)) != 0;
        public static ulong SetBit(this ulong input, char bitIndex, bool value)
        {
            if (value) { return input |= (1ul << bitIndex); }
            return input &= ~(1ul << bitIndex);
        }

        public static bool IsBitSet(this ulong val, byte bit) => (val & (1ul << bit)) != 0;
        public static ulong SetBit(this ulong input, byte bitIndex, bool value)
        {
            if (value) { return input |= (1ul << bitIndex); }
            return input &= ~(1ul << bitIndex);
        }

        public static bool IsBitSet(this ushort val, byte bit) => (val & (1 << bit)) != 0;
        public static ushort SetBit(this ushort input, byte bitIndex, bool value)
        {
            if (value) { return input |= (ushort)(1 << bitIndex); }
            return input &= (ushort)~(1 << bitIndex);
        }

        public static bool IsBitSet(this int val, int bit) => (val & (1 << bit)) != 0;
        public static int SetBit(this int input, int bitIndex, bool value)
        {
            if (value) { return input |= (1 << bitIndex); }
            return input &= ~(1 << bitIndex);
        }

        public static bool IsBitSet(this byte val, int bit) => (val & (1 << bit)) != 0;
        public static byte SetBit(this byte input, int bitIndex, bool value)
        {
            if (value) { return input |= (byte)(1 << bitIndex); }
            return input &= (byte)~(1 << bitIndex);
        }
    }
}
