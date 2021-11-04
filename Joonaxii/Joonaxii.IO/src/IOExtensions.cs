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

        public static int ToInt(byte[] data, bool bigEndian = false) => ToInt(data, 0, bigEndian);
        public static int ToInt(byte[] data, int start, bool bigEndian = false)
        {
            int val = 0;
            if (bigEndian)
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

        public static string ToHexString(this string str, string separator = "")
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                char chr = str[i];
                sb.Append($"{Convert.ToString(chr, 16)}{separator}");
            }
            return sb.ToString();
        }

        public static uint ToUInt(byte[] data, bool bigEndian = false) => ToUInt(data, 0, bigEndian);
        public static uint ToUInt(byte[] data, int start, bool bigEndian = false)
        {
            uint val = 0;
            if (bigEndian)
            {
                for (int i = 3; i >= 0; i--)
                {
                    val += (uint)((data[i + start] << ((3 - i) * 8)));
                }
                return val;
            }
            for (int i = 0; i < 4; i++)
            {
                val += (uint)(data[i + start] << (i * 8));
            }
            return val;
        }

        public static short ToShort(byte[] data, bool bigEndian = false) => ToShort(data, 0, bigEndian);
        public static short ToShort(byte[] data, int start, bool bigEndian = false)
        {
            short val = 0;

            if (bigEndian)
            {
                for (int i = 1; i >= 0; i--)
                {
                    val += (short)((data[i + start] << ((1 - i) * 8)));
                }
                return val;
            }

            for (int i = 0; i < 2; i++)
            {
                val += (short)((data[i + start] << (i * 8)));
            }
            return val;
        }

        public static ushort ToUShort(byte[] data, bool bigEndian = false) => ToUShort(data, 0, bigEndian);
        public static ushort ToUShort(byte[] data, int start, bool bigEndian = false)
        {
            ushort val = 0;

            if (bigEndian)
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


    }
}
