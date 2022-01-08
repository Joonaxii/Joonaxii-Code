using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.Debugging
{
    public static class DebugExtensions
    {
        private static readonly string[] SIZE_SUFFIXES = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string ToBytes(this byte value, byte bytesToExtract = 1) => ToBytes(value, bytesToExtract);
        public static string ToBytes(this short value, byte bytesToExtract = 1) => ToBytes(value, bytesToExtract);
        public static string ToBytes(this int value, byte bytesToExtract = 1) => ToBytes(value, bytesToExtract);
        public static string ToBytes(this ulong value, byte bytesToExtract = 1) => ToBytes(value, bytesToExtract);
        public static string ToBytes(this long value, byte bytesToExtract = 1)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bytesToExtract; i++)
            {
                byte bb = (byte)(value >> (i * 8));
                sb.Append($"|Byte #{i} {bb} => {bb.ToHexString(true, 1)} => {bb.ToBinaryString(true, 8)}|=");
            }
            return sb.ToString();
        }

        public static string ToHexString(this byte value, bool withHeader, byte paddingMode = 1, int paddingZeroes = 2) => ToHexString((long)value, withHeader, paddingMode, paddingZeroes);
        public static string ToHexString(this int value, bool withHeader, byte paddingMode = 1, int paddingZeroes = 8) => ToHexString((long)value, withHeader, paddingMode, paddingZeroes);
        public static string ToHexString(this ulong value, bool withHeader, byte paddingMode = 1, int paddingZeroes = 16) => ToHexString((long)value, withHeader, paddingMode, paddingZeroes);
        public static string ToHexString(this long value, bool withHeader, byte paddingMode = 1, int paddingZeroes = 16)
        {
            string str = Convert.ToString(value, 16);
            int pad;

            string hdr = withHeader ? "0x" : "";
            switch (paddingMode)
            {
                default: return $"{hdr}{str}";
                case 1:
                    pad = str.Length % 2 == 0 ? 0 : 1;
                    return $"{hdr}{str.PadLeft(str.Length + pad, '0')}";
                case 2:
                    return $"{hdr}{str.PadLeft(paddingZeroes, '0')}";
            }
        }

        public static string ToBinaryString(this byte value, bool withHeader, byte paddingZeroes = 0) => ToBinaryString((long)value, withHeader, paddingZeroes);
        public static string ToBinaryString(this int value, bool withHeader, byte paddingZeroes = 0) => ToBinaryString((long)value, withHeader, paddingZeroes);
        public static string ToBinaryString(this short value, bool withHeader, byte paddingZeroes = 0) => ToBinaryString((long)value, withHeader, paddingZeroes);
        public static string ToBinaryString(this ulong value, bool withHeader, byte paddingZeroes = 0) => ToBinaryString((long)value, withHeader, paddingZeroes);
        public static string ToBinaryString(this long value, bool withHeader, byte paddingZeroes = 0)
        {
            string str = Convert.ToString(value, 2);
            string hdr = withHeader ? "0b" : "";
            return $"{hdr}{str.PadLeft(paddingZeroes, '0')}";
        }

        public static string ToFileSizeString(this long bytes, int decimalPlaces = 1)
        {
            if (bytes == 0) { return $"{bytes} bytes"; }
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
