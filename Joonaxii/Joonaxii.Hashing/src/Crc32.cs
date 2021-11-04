using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.Hashing.src
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public class Crc32
    {
        private const uint GENERATOR = 0xEDB88320;
        private uint _value;

        private static readonly uint[] CHECKSUM_TABLE;
        static Crc32()
        {
            CHECKSUM_TABLE = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint itm = i;
                for (int b = 0; b < 8; b++)
                {
                    itm = ((itm & 1) != 0) ? (GENERATOR ^ (itm >> 1)) : (itm >> 1);
                }
                CHECKSUM_TABLE[i] = itm;
            }
        }

        public Crc32() { _value = 0; }
    }
}
