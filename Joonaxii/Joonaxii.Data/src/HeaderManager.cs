using System;
using System.IO;

namespace Joonaxii.Data.Image.Conversion
{
    public static class HeaderManager
    {
        private static MagicHeader[] _magicHeaders = new MagicHeader[]
{
        new MagicHeader(HeaderType.BMP,
            new MagicByte[] { 0x42, 0x4D, } ),

        new MagicHeader(HeaderType.PNG,
            new MagicByte[] { 0x89, 0x50, 0x4E, 0x47, 
                              0xFFF, 0xFFF, 0xFFF, 0xFFF }),

        new MagicHeader(HeaderType.RAW_TEXTURE,
            new MagicByte[] { 'R', 'A', 'W' }),

        new MagicHeader(HeaderType.WEBP,
            new MagicByte[] {
                0x52, 0x49, 0x46, 0x46,
                0xF00, 0xF00, 0xF00, 0xF00,
                0x57, 0x45, 0x42, 0x50}),

        new MagicHeader(HeaderType.GIF87,
            new MagicByte[] {
                0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }),
        new MagicHeader(HeaderType.GIF89,
            new MagicByte[] {
                0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }),

        new MagicHeader(HeaderType.JPEG,
            new MagicByte[] { 0xFF, 0xD8, 0xFF, 0xD8 }),
        new MagicHeader(HeaderType.JPEG,
            new MagicByte[] { 0xFF, 0xD8, 0xFF, 0xEE }),
        new MagicHeader(HeaderType.JPEG,
            new MagicByte[] {
                0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A,
                0x46, 0x49, 0x46, 0x00, 0x01}),
           new MagicHeader(HeaderType.JPEG,
            new MagicByte[] {
                0xFF, 0xD8, 0xFF, 0xE1, 0xF00, 0xF00, 0x45,
                0x78, 0x69, 0x66, 0x00, 0x00}),
};

        static HeaderManager()
        {
            Array.Sort(_magicHeaders);
        }

        public static HeaderType GetFileType(byte[] data)
        {
            for (int i = 0; i < _magicHeaders.Length; i++)
            {
                var header = _magicHeaders[i];
                if (header.HasHeader(data))
                {
                    return header.GetHeaderType;
                }
            }
            return HeaderType.UNKNOWN;
        }

        public static HeaderType GetFileType(BinaryReader br, bool resetPositionOnSuccess)
        {
            long pos = br.BaseStream.Position;
            for (int i = 0; i < _magicHeaders.Length; i++)
            {
                var header = _magicHeaders[i];
                if (header.HasHeader(br, pos, resetPositionOnSuccess))
                {
                    return header.GetHeaderType;
                }
                br.BaseStream.Seek(pos, SeekOrigin.Begin);
            }
            return HeaderType.UNKNOWN;
        }

        public static bool TryGetHeader(HeaderType type, out MagicHeader header, int len = -1)
        {
            for (int i = 0; i < _magicHeaders.Length; i++)
            {
                var hdr = _magicHeaders[i];
                if (hdr.GetHeaderType == type & (len <= 0 | hdr.Length == len))
                {
                    header = hdr;
                    return true;
                }
            }
            header = MagicHeader.none;
            return false;
        }
    }
}
