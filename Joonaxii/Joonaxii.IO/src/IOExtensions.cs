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

    }
}
