using System;

namespace Joonaxii.IO
{
    public static class IOExtensions
    {
        public static readonly string[] SIZE_SUFFIXES = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

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
