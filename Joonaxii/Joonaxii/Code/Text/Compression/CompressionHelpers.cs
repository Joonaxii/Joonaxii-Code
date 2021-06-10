using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.Text.Compression
{
    public static class CompressionHelpers
    {
        public static bool IsLZW(byte[] data)
        {
            if(data.Length < 3) { return false; }
            for (int i = 0; i < 3; i++)
            {
                if(data[i] != LZW.HEADER_STR[i]) { return false; }
            }
            return true;
        }

        public static bool IsTTC(byte[] data)
        {
            if (data.Length < 3) { return false; }
            for (int i = 0; i < 3; i++)
            {
                if (data[i] != TTC.HEADER_STR[i]) { return false; }
            }
            return true;
        }
    }
}
