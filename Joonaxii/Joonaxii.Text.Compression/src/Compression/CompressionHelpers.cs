
namespace Joonaxii.Text.Compression
{
    public static class CompressionHelpers
    {
        public static byte ToSize(int count)
        {
            if(count > ushort.MaxValue) { return 4; }
            if(count > byte.MaxValue) { return 2; }
            return 1;
        }

        public static bool IsLZW(byte[] data, long offset = 0)
        {
            if(data.Length - offset < 3) { return false; }
            for (long i = 0; i < 3; i++)
            {
                if(data[i + offset] != LZW.HEADER_STR[i]) { return false; }
            }
            return true;
        }

        public static bool IsTTC(byte[] data, long offset = 0)
        {
            if (data.Length - offset < 3) { return false; }
            for (long i = 0; i < 3; i++)
            {
                if (data[i + offset] != TTC.HEADER_STR[(int)i]) { return false; }
            }
            return true;
        }
    }
}
