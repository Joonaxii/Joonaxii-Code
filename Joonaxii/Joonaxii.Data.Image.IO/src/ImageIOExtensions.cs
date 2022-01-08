using Joonaxii.IO;
using System;
using System.IO;

namespace Joonaxii.Data.Image.IO
{
    public static class ImageIOExtensions
    {
        public const float BIT_5_TO_FLOAT = 1.0f / 31.0f;
        public const float BIT_6_TO_FLOAT = 1.0f / 63.0f;

        public const float BYTE_TO_FLOAT = 1.0f / byte.MaxValue;
        public const float BYTE_TO_USHORT = BYTE_TO_FLOAT * ushort.MaxValue;

        public const float USHORT_TO_FLOAT = 1.0f / ushort.MaxValue;
        public const float USHORT_TO_BYTE = USHORT_TO_FLOAT * 255;

        public static ColorMode GetColorMode(byte bpp, int r = 0xFFFFFF, int g = 0xFFFFFF, int b = 0xFFFFFF, int a = 0x0)
        {
            switch (bpp)
            {
                default: return ColorMode.RGBA32;

                case 4: return ColorMode.Indexed4;
                case 8: return ColorMode.Indexed8;

                case 16:
                    if(g == 0x7E0) { return ColorMode.RGB565; }
                    return a != 0 ? ColorMode.ARGB555 : ColorMode.RGB555;
                case 24: return ColorMode.RGB24;
                case 32: return ColorMode.RGBA32;
            }
        }

        public static byte GetBPP(this ColorMode cmd)
        {
            switch (cmd)
            {
                default: return 32;

                case ColorMode.Indexed4: return 4;
                case ColorMode.Indexed8: return 8;

                case ColorMode.OneBit: return 1;

                case ColorMode.RGB555:
                case ColorMode.ARGB555:
                case ColorMode.RGB565: return 16;

                case ColorMode.RGB24: return 24;
                case ColorMode.RGBA32: return 32;
            }
        }

        public static byte To5Bit(byte b) => (byte)(31.0f * (b * BYTE_TO_FLOAT));
        public static byte To6Bit(byte b) => (byte)(63.0f * (b * BYTE_TO_FLOAT));

        public static byte From5Bit(byte b) => (byte)Math.Round(255.0f * (b * BIT_5_TO_FLOAT));
        public static byte From6Bit(byte b) => (byte)Math.Round(255.0f * (b * BIT_6_TO_FLOAT));

        public static bool RequiresBits(this ColorMode cmd)
        {
            switch (cmd)
            {
                default: return false;
                case ColorMode.Indexed4:
                case ColorMode.Indexed8:
                case ColorMode.RGB555:
                case ColorMode.RGB565:
                    return true;
            }
        }

        public static void WriteColors(this BinaryWriter bw, FastColor[] colors) => WriteColors(bw, colors, colors.Length, ColorMode.RGBA32);
        public static void WriteColors(this BinaryWriter bw, FastColor[] colors, int count) => WriteColors(bw, colors, count, ColorMode.RGBA32);
        public static void WriteColors(this BinaryWriter bw, FastColor[] colors, ColorMode pFmt) => WriteColors(bw, colors, colors.Length, pFmt);
        public static void WriteColors(this BinaryWriter bw, FastColor[] colors, int count, ColorMode pFmt)
        {
            if(pFmt.RequiresBits() && bw is BitWriter bwI)
            {
                WriteColors(bwI, colors, count, pFmt);
                return;
            }
            count = colors.Length < count ? colors.Length : count;
            for (int i = 0; i < count; i++)
            {
                WriteColorInternal(bw, colors[i], pFmt);
            }
        }

        public static FastColor[] ReadColors(this BinaryReader br, int count, bool reverse = false) => ReadColors(br, count, ColorMode.RGBA32, reverse);
        public static FastColor[] ReadColors(this BinaryReader br, int count, ColorMode pFmt, bool reverse = false)
        {
            FastColor[] colors = new FastColor[count];
            ReadColors(br, colors, count, pFmt);
            return colors;
        }

        public static int ReadColors(this BinaryReader br, FastColor[] colors, bool reverse = false) => ReadColors(br, colors, colors.Length, ColorMode.RGBA32, reverse);
        public static int ReadColors(this BinaryReader br, FastColor[] colors, ColorMode pFmt, bool reverse = false) => ReadColors(br, colors, colors.Length, pFmt, reverse);
        public static int ReadColors(this BinaryReader br, FastColor[] colors, int count, bool reverse = false) => ReadColors(br, colors, count, ColorMode.RGBA32, reverse);
        public static int ReadColors(this BinaryReader br, FastColor[] colors, int count, ColorMode pFmt, bool reverse = false)
        {
            if (pFmt.RequiresBits() && br is BitReader brI) { return ReadColors(brI, colors, count, pFmt, reverse); }
            count = colors.Length < count ? colors.Length : count;
            for (int i = 0; i < count; i++)
            {
                colors[i] = ReadColorInternal(br, pFmt, reverse);
            }
            return count;
        }

        public static void WriteColors(this BitWriter bw, FastColor[] colors) => WriteColors(bw, colors, colors.Length, ColorMode.RGBA32);
        public static void WriteColors(this BitWriter bw, FastColor[] colors, int count) => WriteColors(bw, colors, count, ColorMode.RGBA32);
        public static void WriteColors(this BitWriter bw, FastColor[] colors, ColorMode pFmt) => WriteColors(bw, colors, colors.Length, pFmt);
        public static void WriteColors(this BitWriter bw, FastColor[] colors, int count, ColorMode pFmt)
        {
            count = colors.Length < count ? colors.Length : count;
            for (int i = 0; i < count; i++)
            {
                WriteColor(bw, colors[i], pFmt);
            }
        }

        public static FastColor[] ReadColors(this BitReader br, int count, bool reverse = false) => ReadColors(br, count, ColorMode.RGBA32, reverse);
        public static FastColor[] ReadColors(this BitReader br, int count, ColorMode pFmt, bool reverse = false)
        {
            FastColor[] colors = new FastColor[count];
            ReadColors(br, colors, count, pFmt, reverse);
            return colors;
        }

        public static int ReadColors(this BitReader br, FastColor[] colors, int count, bool reverse = false) => ReadColors(br, colors, count, ColorMode.RGBA32, reverse);
        public static int ReadColors(this BitReader br, FastColor[] colors, ColorMode pFmt, bool reverse = false) => ReadColors(br, colors, colors.Length, pFmt, reverse);
        public static int ReadColors(this BitReader br, FastColor[] colors, bool reverse = false) => ReadColors(br, colors, colors.Length, ColorMode.RGBA32, reverse);
        public static int ReadColors(this BitReader br, FastColor[] colors, int count, ColorMode pFmt, bool reverse = false)
        {
            count = colors.Length < count ? colors.Length : count;
            for (int i = 0; i < count; i++)
            {
                colors[i] = ReadColor(br, pFmt, reverse);
            }
            return count;
        }

        public static FastColor ReadColor(this BinaryReader br, int reverseMode)
        {
            byte r, g, b, a;
            switch (reverseMode)
            {
                default: return new FastColor(br.ReadInt32());
                case 1:
                    a = br.ReadByte();
                    b = br.ReadByte();
                    g = br.ReadByte();
                    r = br.ReadByte();
                    return new FastColor(r, g, b, a);
                case 2:
                    b = br.ReadByte();
                    g = br.ReadByte();
                    r = br.ReadByte();
                    a = br.ReadByte();
                    return new FastColor(r, g, b, a);
            }
        }
        public static void WriteColor(this BinaryWriter bw, FastColor color) => bw.Write(color);

        public static FastColor ReadColor(this BinaryReader br, ColorMode pFmt, bool reverse)
        {
            if (pFmt.RequiresBits()) { return br is BitReader brI ? ReadColor(brI, pFmt, reverse) : FastColor.clear; }
            return ReadColorInternal(br, pFmt, reverse);
        }
        public static void WriteColor(this BinaryWriter bw, FastColor color, ColorMode pFmt)
        {
            if (pFmt.RequiresBits() && bw is BitWriter bwI) 
            {
                WriteColor(bwI, color, pFmt);
                return;
            }
            WriteColorInternal(bw, color, pFmt);
        }

        public static FastColor ReadColor(this BitReader br, ColorMode pFmt, bool reverse)
        {
            byte r, g, b, a;
            switch (pFmt)
            {
                default: return new FastColor(br.ReadInt32());
                case ColorMode.RGBA32: return new FastColor(br.ReadInt32());

                case ColorMode.Indexed4:
                    return new FastColor(br.ReadByte(4));
                case ColorMode.Indexed8:
                    return new FastColor(br.ReadByte(8));
                case ColorMode.RGB565:
                    if (reverse)
                    {
                        b = From5Bit(br.ReadByte(5));
                        g = From6Bit(br.ReadByte(6));
                        r = From5Bit(br.ReadByte(5));
                        return new FastColor(r, g, b, 255);
                    }
                    return new FastColor(From5Bit(br.ReadByte(5)), 
                                         From6Bit(br.ReadByte(6)), 
                                         From5Bit(br.ReadByte(5)), 255);
                case ColorMode.RGB555:
                case ColorMode.ARGB555:
                    bool bA = pFmt == ColorMode.RGB555;
                    if (reverse)
                    {
                        b = From5Bit(br.ReadByte(5));
                        g = From5Bit(br.ReadByte(5));
                        r = From5Bit(br.ReadByte(5));
                        bA |= br.ReadBoolean();
                    } 
                    else
                    {
                        bA |= br.ReadBoolean();
                        r = From5Bit(br.ReadByte(5));
                        g = From5Bit(br.ReadByte(5));
                        b = From5Bit(br.ReadByte(5));
                    }
                    return new FastColor(r, g, b, (byte)(bA ? 255 : 0));
                case ColorMode.RGB24:
                    if (reverse)
                    {
                        b = br.ReadByte();
                        g = br.ReadByte();
                        r = br.ReadByte();
                        return new FastColor(r, g, b, 255);
                    }
                    return new FastColor(br.ReadByte(), br.ReadByte(), br.ReadByte(), 255);
                //case 64:
                //    return new FastColor(
                //    (byte)(br.ReadUInt16() * USHORT_TO_BYTE),
                //    (byte)(br.ReadUInt16() * USHORT_TO_BYTE),
                //    (byte)(br.ReadUInt16() * USHORT_TO_BYTE),
                //    (byte)(br.ReadUInt16() * USHORT_TO_BYTE));
            }
        }
        public static void WriteColor(this BitWriter bw, FastColor color, ColorMode pFmt)
        {
            switch (pFmt)
            {
                default: bw.Write(color, 32); break;
                case ColorMode.Indexed4:
                    bw.Write(color.r, 4);
                    break;
                case ColorMode.Indexed8:
                    bw.Write(color.r);
                    break;
                case ColorMode.RGB565:
                    bw.Write(To5Bit(color.r), 5);
                    bw.Write(To6Bit(color.g), 6);
                    bw.Write(To5Bit(color.b), 5);
                    break;
                case ColorMode.RGB555:
                case ColorMode.ARGB555:
                    bw.Write(pFmt != ColorMode.ARGB555 | color.a > 127);
                    bw.Write(To5Bit(color.r), 5);
                    bw.Write(To5Bit(color.g), 5);
                    bw.Write(To5Bit(color.b), 5);
                    break;
                case ColorMode.RGB24:
                    bw.Write(color.r);
                    bw.Write(color.g);
                    bw.Write(color.b);
                    break;
                case ColorMode.RGBA32: bw.Write(color, 32); break;
                //case 64:
                //    bw.Write((ushort)(color.r * BYTE_TO_USHORT));
                //    bw.Write((ushort)(color.g * BYTE_TO_USHORT));
                //    bw.Write((ushort)(color.b * BYTE_TO_USHORT));
                //    bw.Write((ushort)(color.a * BYTE_TO_USHORT));
                //    break;
            }
        }

        private static FastColor ReadColorInternal(BinaryReader br, ColorMode pFmt, bool reverse)
        {
            ushort val;
            switch (pFmt)
            {
                default: return ReadColor(br, reverse ? 1 : 0);
                case ColorMode.Indexed8:
                    return new FastColor(br.ReadByte());
                case ColorMode.RGB555:
                case ColorMode.ARGB555:
                    bool hasA = pFmt == ColorMode.RGB555;

                    val = br.ReadUInt16();
                    if (reverse)
                    {
                        hasA = hasA || ((byte)(val & 0x8000) >> 15) > 0;
                        return new FastColor(
                            From5Bit((byte)((val & 0x7C00) >> 10)),
                            From5Bit((byte)((val & 0x3E0) >> 5)),
                            From5Bit((byte)((val & 0x1F))),
                            (byte)(hasA ? 255 : 0));
                    }

                    hasA = hasA || ((byte)(val & 0x1)) > 0;
                    return new FastColor(
                         From5Bit((byte)((val & 0x3E) >> 1)),
                         From5Bit((byte)((val & 0x7C0) >> 6)),
                         From5Bit((byte)((val & 0xF800) >> 11)), 
                         (byte)(hasA ? 255 : 0));

                case ColorMode.RGB565:
                    val = br.ReadUInt16();
                    if (reverse)
                    {
                        return new FastColor(
                            From5Bit((byte)((val & 0xF800) >> 11)),
                            From6Bit((byte)((val & 0x7E0) >> 6)),
                            From5Bit((byte)((val & 0x1F))), 255);
                    }
                    return new FastColor(
                         From5Bit((byte)((val & 0x1F))),
                         From6Bit((byte)((val & 0x7E0) >> 6)),
                         From5Bit((byte)((val & 0xF800) >> 11)), 255);

                case ColorMode.RGB24:
                    if (reverse)
                    {
                        byte b = br.ReadByte();
                        byte g = br.ReadByte();
                        byte r = br.ReadByte();
                        return new FastColor(r, g, b, 255);
                    }
                    return new FastColor(br.ReadByte(), br.ReadByte(), br.ReadByte(), 255);
                case ColorMode.RGBA32: return ReadColor(br, reverse ? 1 : 0);
                //case 64:
                //    return new FastColor(
                //    (byte)(br.ReadUInt16() * USHORT_TO_BYTE),
                //    (byte)(br.ReadUInt16() * USHORT_TO_BYTE),
                //    (byte)(br.ReadUInt16() * USHORT_TO_BYTE),
                //    (byte)(br.ReadUInt16() * USHORT_TO_BYTE));
            }
        }
        private static void WriteColorInternal(BinaryWriter bw, FastColor color, ColorMode pFmt)
        {
            switch (pFmt)
            {
                default: WriteColor(bw, color); break;
                case ColorMode.Indexed8:
                    bw.Write(color.r);
                    break;
                case ColorMode.RGB555:
                case ColorMode.RGB565:
                    bw.Write(color.r);
                    bw.Write(color.g);
                    break;
                case ColorMode.RGB24:
                    bw.Write(color.r);
                    bw.Write(color.g);
                    bw.Write(color.b);
                    break;
                case ColorMode.RGBA32: WriteColor(bw, color); break;
                //case 64:
                //    bw.Write((ushort)(color.r * BYTE_TO_USHORT));
                //    bw.Write((ushort)(color.g * BYTE_TO_USHORT));
                //    bw.Write((ushort)(color.b * BYTE_TO_USHORT));
                //    bw.Write((ushort)(color.a * BYTE_TO_USHORT));
                //    break;
            }
        }
    }
}
