using Joonaxii.IO;
using System;
using System.IO;

namespace Joonaxii.Data.Image.IO
{
    public static class ImageIOExtensions
    {
        public const float BYTE_TO_FLOAT = 1.0f / byte.MaxValue;
        public const float BYTE_TO_USHORT = BYTE_TO_FLOAT * ushort.MaxValue;

        public const float USHORT_TO_FLOAT = 1.0f / ushort.MaxValue;
        public const float USHORT_TO_BYTE = USHORT_TO_FLOAT * 255;

        public static void WriteColors(this BinaryWriter bw, FastColor[] colors) => WriteColors(bw, colors, colors.Length, 32);
        public static void WriteColors(this BinaryWriter bw, FastColor[] colors, int count) => WriteColors(bw, colors, count, 32);
        public static void WriteColors(this BinaryWriter bw, FastColor[] colors, byte bPP) => WriteColors(bw, colors, colors.Length, bPP);
        public static void WriteColors(this BinaryWriter bw, FastColor[] colors, int count, byte bPP)
        {
            if(bPP < 8 && bw is BitWriter bwI)
            {
                WriteColors(bwI, colors, count, bPP);
                return;
            }
            count = colors.Length < count ? colors.Length : count;
            for (int i = 0; i < count; i++)
            {
                WriteColorInternal(bw, colors[i], bPP);
            }
        }

        public static FastColor[] ReadColors(this BinaryReader br, int count) => ReadColors(br, count, 32);
        public static FastColor[] ReadColors(this BinaryReader br, int count, byte bPP)
        {
            FastColor[] colors = new FastColor[count];
            ReadColors(br, colors, count, bPP);
            return colors;
        }

        public static int ReadColors(this BinaryReader br, FastColor[] colors, int count) => ReadColors(br, colors, count, 32);
        public static int ReadColors(this BinaryReader br, FastColor[] colors, int count, byte bPP)
        {
            if (bPP < 8 && br is BitReader brI) { return ReadColors(brI, colors, count, bPP); }
            count = colors.Length < count ? colors.Length : count;
            for (int i = 0; i < count; i++)
            {
                colors[i] = ReadColorInternal(br, bPP);
            }
            return count;
        }

        public static void WriteColors(this BitWriter bw, FastColor[] colors) => WriteColors(bw, colors, colors.Length, 32);
        public static void WriteColors(this BitWriter bw, FastColor[] colors, int count) => WriteColors(bw, colors, count, 32);
        public static void WriteColors(this BitWriter bw, FastColor[] colors, byte bPP) => WriteColors(bw, colors, colors.Length, bPP);
        public static void WriteColors(this BitWriter bw, FastColor[] colors, int count, byte bPP)
        {
            count = colors.Length < count ? colors.Length : count;
            for (int i = 0; i < count; i++)
            {
                WriteColor(bw, colors[i], bPP);
            }
        }

        public static FastColor[] ReadColors(this BitReader br, int count) => ReadColors(br, count, 32);
        public static FastColor[] ReadColors(this BitReader br, int count, byte bPP)
        {
            FastColor[] colors = new FastColor[count];
            ReadColors(br, colors, count, bPP);
            return colors;
        }

        public static int ReadColors(this BitReader br, FastColor[] colors, int count) => ReadColors(br, colors, count, 32);
        public static int ReadColors(this BitReader br, FastColor[] colors, int count, byte bPP)
        {
            count = colors.Length < count ? colors.Length : count;
            for (int i = 0; i < count; i++)
            {
                colors[i] = ReadColor(br, bPP);
            }
            return count;
        }

        public static FastColor ReadColor(this BinaryReader br) => new FastColor(br.ReadInt32());
        public static void WriteColor(this BinaryWriter bw, FastColor color) => bw.Write(color);

        public static FastColor ReadColor(this BinaryReader br, byte bPP)
        {
            if ((bPP < 8)) { return br is BitReader brI ? ReadColor(brI, bPP) : FastColor.clear; }
            return ReadColorInternal(br, bPP);
        }
        public static void WriteColor(this BinaryWriter bw, FastColor color, byte bPP)
        {
            if (bPP < 8 && bw is BitWriter bwI) 
            {
                WriteColor(bwI, color, bPP);
                return;
            }
            WriteColorInternal(bw, color, bPP);
        }

        public static FastColor ReadColor(this BitReader br, byte bPP)
        {
            switch (bPP)
            {
                default: return new FastColor(br.ReadInt32());
                case 32: return new FastColor(br.ReadInt32());

                case 4: 
                case 8:
                    FastColor c = new FastColor();
                    c.Set(br.ReadByte(bPP));
                    return c;
                case 16:
                    return new FastColor(br.ReadByte(), br.ReadByte(), 0, 255);
                case 64:
                    return new FastColor(
                    (byte)(br.ReadUInt16() * USHORT_TO_BYTE),
                    (byte)(br.ReadUInt16() * USHORT_TO_BYTE),
                    (byte)(br.ReadUInt16() * USHORT_TO_BYTE),
                    (byte)(br.ReadUInt16() * USHORT_TO_BYTE));
            }
        }
        public static void WriteColor(this BitWriter bw, FastColor color, byte bPP)
        {
            switch (bPP)
            {
                default: bw.Write(color, 32); break;
                case 4:
                    bw.Write(color.r, 4);
                    break;
                case 8:
                    bw.Write(color.r);
                    break;
                case 16:
                    bw.Write(color.r);
                    bw.Write(color.g);
                    break;
                case 24:
                    bw.Write(color.r);
                    bw.Write(color.g);
                    bw.Write(color.b);
                    break;
                case 32: bw.Write(color, 32); break;
                case 64:
                    bw.Write((ushort)(color.r * BYTE_TO_USHORT));
                    bw.Write((ushort)(color.g * BYTE_TO_USHORT));
                    bw.Write((ushort)(color.b * BYTE_TO_USHORT));
                    bw.Write((ushort)(color.a * BYTE_TO_USHORT));
                    break;
            }
            bw.Write(color);
        }

        private static FastColor ReadColorInternal(BinaryReader br, byte bPP)
        {
            switch (bPP)
            {
                default: return ReadColor(br);
                case 8:
                    FastColor c = new FastColor();
                    c.Set(br.ReadByte());
                    return c;
                case 16:
                    return new FastColor(br.ReadByte(), br.ReadByte(), 0, 255);
                case 24:
                    return new FastColor(br.ReadByte(), br.ReadByte(), br.ReadByte(), 255);
                case 32: return ReadColor(br);
                case 64:
                    return new FastColor(
                    (byte)(br.ReadUInt16() * USHORT_TO_BYTE),
                    (byte)(br.ReadUInt16() * USHORT_TO_BYTE),
                    (byte)(br.ReadUInt16() * USHORT_TO_BYTE),
                    (byte)(br.ReadUInt16() * USHORT_TO_BYTE));
            }
        }
        private static void WriteColorInternal(BinaryWriter bw, FastColor color, byte bPP)
        {
            switch (bPP)
            {
                default: WriteColor(bw, color); break;
                case 8:
                    bw.Write(color.r);
                    break;
                case 16:
                    bw.Write(color.r);
                    bw.Write(color.g);
                    break;
                case 24:
                    bw.Write(color.r);
                    bw.Write(color.g);
                    bw.Write(color.b);
                    break;
                case 32: WriteColor(bw, color); break;
                case 64:
                    bw.Write((ushort)(color.r * BYTE_TO_USHORT));
                    bw.Write((ushort)(color.g * BYTE_TO_USHORT));
                    bw.Write((ushort)(color.b * BYTE_TO_USHORT));
                    bw.Write((ushort)(color.a * BYTE_TO_USHORT));
                    break;
            }
        }
    }
}
