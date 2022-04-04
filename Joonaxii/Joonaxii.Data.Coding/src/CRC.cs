using System.IO;

namespace Joonaxii.Data.Coding
{
    public class CRC
    {
        public const uint CRC_START_VALUE = 0xFFFFFFFF;

        private readonly static uint[] _crcTable;
        static CRC()
        {
            uint c;
            _crcTable = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                c = i;
                for (int k = 0; k < 8; k++)
                {
                    if ((c & 1) == 1)
                    {
                        c = 0xEDB88320 ^ (c >> 1);
                        continue;
                    }
                    c >>= 1;
                }
                _crcTable[i] = c;
            }
        }

        public static uint Calculate(long hdr, int hdrLen, byte[] bytes, int start, int len)
        {
            uint c;
            c = 0xFFFFFFFF;

            for (int i = 0; i < hdrLen; i++)
            {
                c = _crcTable[(c ^ ((hdr >> (i << 3)) & 0xff)) & 0xFF] ^ (c >> 8);
            }
       
            for (int i = start; i < start + len; i++)
            {
                c = _crcTable[(c ^ bytes[i]) & 0xFF] ^ (c >> 8);
            }
            return c ^ 0xFFFFFFFF;
        }

        public static unsafe uint Calculate(long hdr, int hdrLen, byte* bytes, int start, int len)
        {
            uint c;
            c = 0xFFFFFFFF;

            for (int i = 0; i < hdrLen; i++)
            {
                c = _crcTable[(c ^ ((hdr >> (i << 3)) & 0xff)) & 0xFF] ^ (c >> 8);
            }

            for (int i = start; i < start + len; i++)
            {
                c = _crcTable[(c ^ bytes[i]) & 0xFF] ^ (c >> 8);
            }
            return c ^ 0xFFFFFFFF;
        }

        public static uint Calculate(byte[] bytes, int start, int len)
        {
            uint c;
            c = 0xFFFFFFFF;
            for (int i = start; i < start + len; i++)
            {
                c = _crcTable[(c ^ bytes[i]) & 0xFF] ^ (c >> 8);
            }
            return c ^ 0xFFFFFFFF;
        }

        public static uint CalculateUnsafe(byte[] bytes, int start, int len)
        {
            unsafe
            {
                fixed (byte* buf = bytes)
                {
                    return Calculate(buf, start, len);
                }
            }
        }

        public static unsafe uint ProgAdd(uint crc, byte* ptr, int length)
        {
            for (int i = 0; i < length; i++)
            {
                crc = _crcTable[(crc ^ *ptr++) & 0xFF] ^ (crc >> 8);
            }
            return crc;
        }

        public static uint ProgAdd(uint crc, Stream stream, int length)
        {
            for (int i = 0; i < length; i++)
            {
                crc = _crcTable[(crc ^ stream.ReadByte()) & 0xFF] ^ (crc >> 8);
            }
            return crc;
        }

        public static uint ProgAdd(uint crc, byte value) => _crcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        public static uint ProgAdd(uint crc, sbyte value) => _crcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);

        public static uint ProgAdd(uint crc, short value) => _crcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        public static uint ProgAdd(uint crc, ushort value) => _crcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        public static uint ProgAdd(uint crc, char value) => _crcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);

        public static uint ProgAdd(uint crc, int value) => _crcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        public static uint ProgAdd(uint crc, uint value) => _crcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);

        public static uint ProgAdd(uint crc, long value) => _crcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        public static uint ProgAdd(uint crc, ulong value) => _crcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);

        public static uint ProgEnd(uint crc) => crc ^ CRC_START_VALUE;

        public static uint Calculate(Stream stream, int length)
        {
            uint c;
            c = CRC_START_VALUE;
            for (int i = 0; i < length; i++)
            {
                c = _crcTable[(c ^ stream.ReadByte()) & 0xFF] ^ (c >> 8);
            }
            return c ^ CRC_START_VALUE;
        }

        public static unsafe uint Calculate(byte* bytes, int start, int len)
        {
            uint c;
            c = CRC_START_VALUE;
            bytes += start;
            while (len-- > 0)
            {
                c = _crcTable[(c ^ *bytes) & 0xFF] ^ (c >> 8);
                bytes++;
            }
            return c ^ CRC_START_VALUE;
        }
    }
}
