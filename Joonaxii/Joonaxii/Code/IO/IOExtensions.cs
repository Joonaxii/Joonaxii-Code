using Joonaxii.MathX;
using System;
using System.Collections.Generic;

namespace Joonaxii.IO
{
    public static class IOExtensions
    {
        public static readonly string[] SIZE_SUFFIXES = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static int GetCharSize(this string str)
        {
            char highest = '\0';
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                highest = highest < c ? c : highest;
            }
            return highest > byte.MaxValue ? 2 : 1;
        }

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

        public static string GetFileSizeString(long bytes, int decimalPlaces = 1)
        {
            decimalPlaces = decimalPlaces < 0 ? 0 : decimalPlaces;
            bool isNegative = bytes < 0;

            bytes = isNegative ? -bytes : bytes;

            int mag = (int)Math.Log(bytes, 1024);
            decimal size = (decimal)bytes / (1L << (mag * 10));

            if (Math.Round(size, decimalPlaces) >= 1000)
            {
                mag++;
                size /= 1024;
            }

            return string.Format($"{(isNegative ? "-" : "")}{"{0:n"}{decimalPlaces}{"}"} {"{1}"}", size, SIZE_SUFFIXES[mag]);
        }
    }
}
