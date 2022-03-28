using Joonaxii.Image.Texturing;
using Joonaxii.IO;
using Joonaxii.IO.BitStream;
using Joonaxii.MathJX;
using System;
using System.Collections.Generic;
using System.IO;

namespace Joonaxii.Image.Codecs
{
    public static class ImageCodecExtensions
    {
        public const float BIT_5_TO_FLOAT = 1.0f / 31.0f;
        public const float BIT_6_TO_FLOAT = 1.0f / 63.0f;

        public const float BYTE_TO_FLOAT = 1.0f / byte.MaxValue;
        public const float BYTE_TO_USHORT = BYTE_TO_FLOAT * ushort.MaxValue;

        public const float USHORT_TO_FLOAT = 1.0f / ushort.MaxValue;
        public const float USHORT_TO_BYTE = USHORT_TO_FLOAT * 255;

        public static TextureFormat GetColorMode(byte bpp, int r = 0xFFFFFF, int g = 0xFFFFFF, int b = 0xFFFFFF, int a = 0x0)
        {
            switch (bpp)
            {
                default: return TextureFormat.RGBA32;

                case 4: return TextureFormat.Indexed4;
                case 8: return TextureFormat.Indexed8;

                case 16:
                    if(g == 0x7E0) { return TextureFormat.RGB565; }
                    return a != 0 ? TextureFormat.ARGB555 : TextureFormat.RGB555;
                case 24: return TextureFormat.RGB24;
                case 32: return TextureFormat.RGBA32;
            }
        }

        public static byte GetBPP(this TextureFormat cmd)
        {
            switch (cmd)
            {
                default: return 32;

                case TextureFormat.Indexed4: return 4;

                case TextureFormat.Indexed: 
                case TextureFormat.Indexed8: return 8;

                case TextureFormat.OneBit:   return 1;

                case TextureFormat.RGB555:
                case TextureFormat.ARGB555:
                case TextureFormat.RGB565:   return 16;

                case TextureFormat.RGB24:    return 24;
                case TextureFormat.RGBA32:   return 32;
            }
        }

        public static byte To5Bit(byte b) => (byte)(31.0f * (b * BYTE_TO_FLOAT));
        public static byte To6Bit(byte b) => (byte)(63.0f * (b * BYTE_TO_FLOAT));

        public static byte From5Bit(byte b) => (byte)Math.Round(255.0f * (b * BIT_5_TO_FLOAT));
        public static byte From6Bit(byte b) => (byte)Math.Round(255.0f * (b * BIT_6_TO_FLOAT));

        public static bool RequiresBits(this TextureFormat cmd)
        {
            switch (cmd)
            {
                default: return false;
                case TextureFormat.Indexed4:
                case TextureFormat.Indexed8:
                    return true;
            }
        }

        public static byte[] ToBytes(this FastColor[] colors, PixelByteOrder byteOrder, bool invertY, int width, int height, TextureFormat mode)
        {
            byte bPP = mode.GetBPP();
            if(bPP < 8)
            {
                return null;
            }
            bPP >>= 3;
            byte[] data = new byte[bPP * colors.Length];

            int ii = 0;
            int pI;
            FastColor c;
            switch (mode)
            {
                default:
                    for (int i = 0; i < colors.Length; i++)
                    {
                        pI = i;

                        if (invertY)
                        {
                            int x = pI % width;
                            int y = pI / width;
                            pI = ((height - 1 - y) * width) + x;
                        }

                        c = colors[pI];

                        switch (byteOrder)
                        {
                            case PixelByteOrder.RGBA:
                                data[ii++] = c.r;
                                data[ii++] = c.g;
                                data[ii++] = c.b;
                                data[ii++] = c.a;
                                break;

                            case PixelByteOrder.ARGB:
                                data[ii++] = c.a;
                                data[ii++] = c.r;
                                data[ii++] = c.g;
                                data[ii++] = c.b;
                                break;

                            case PixelByteOrder.ABGR:
                                data[ii++] = c.a;
                                data[ii++] = c.b;
                                data[ii++] = c.g;
                                data[ii++] = c.r;
                                break;
                        }

                    }
                    break;
                case TextureFormat.Indexed8:
                    for (int i = 0; i < colors.Length; i++)
                    {
                        pI = i;
                        if (invertY)
                        {
                            int x = pI % width;
                            int y = pI / width;
                            pI = ((height - 1 - y) * width) + x;
                        }

                        c = colors[pI];
                        data[ii++] = c.r;
                    }
                    break;
                case TextureFormat.RGB24:
                    for (int i = 0; i < colors.Length; i++)
                    {
                        pI = i;
                        if (invertY)
                        {
                            int x = pI % width;
                            int y = pI / width;
                            pI = ((height - 1 - y) * width) + x;
                        }

                        c = colors[pI];
                        switch (byteOrder)
                        {
                            case PixelByteOrder.RGBA:
                            case PixelByteOrder.ARGB:
                                data[ii++] = c.r;
                                data[ii++] = c.g;
                                data[ii++] = c.b;
                                break;

                            case PixelByteOrder.ABGR:
                                data[ii++] = c.b;
                                data[ii++] = c.g;
                                data[ii++] = c.r;
                                break;
                        }
                    }
                    break;
                case TextureFormat.RGB565:
                    for (int i = 0; i < colors.Length; i++)
                    {
                        pI = i;
                        if (invertY)
                        {
                            int x = pI % width;
                            int y = pI / width;
                            pI = ((height - 1 - y) * width) + x;
                        }

                        c = colors[pI];
                        byte r5 = To5Bit(c.r);
                        byte g6 = To6Bit(c.g);
                        byte b5 = To5Bit(c.b);

                        byte lo = (byte)((r5 & 0b11111) + ((g6 & 0b111) << 5));
                        byte hi = (byte)(((g6 & 0b111000) >> 3) + ((b5 & 0b11111) << 3));

                        data[ii++] = lo;
                        data[ii++] = hi;
                    }
                    break;
                case TextureFormat.ARGB555:
                case TextureFormat.RGB555:
                    for (int i = 0; i < colors.Length; i++)
                    {
                        pI = i;
                              if (invertY)
                        {
                            int x = pI % width;
                            int y = pI / width;
                            pI = ((height - 1 - y) * width) + x;
                        }

                        c = colors[pI];

                        byte r5 = To5Bit(c.r);
                        byte g5 = To5Bit(c.g);
                        byte b5 = To5Bit(c.b);
                        byte a1 = (byte)(c.a > 127 ? 1 : 0);

                        byte lo = (byte)((r5 & 0b11111) + ((g5 & 0b111) << 5));
                        byte hi = (byte)(((g5 & 0b11000) >> 3) + ((b5 & 0b11111) << 2) + (a1 << 7));

                        data[ii++] = lo;
                        data[ii++] = hi;
                    }
                    break;
            }
            return data;
        }

        public static FastColor[] ToFastColor(byte[] bytes, TextureFormat mode)
        {
            byte bPP = mode.GetBPP();
            if (bPP < 8)
            {
                return null;
            }
            bPP >>= 3;
            FastColor[] pix = new FastColor[bytes.Length / bPP];

            int ii = 0;
            switch (mode)
            {
                default:
                    for (int i = 0; i < bytes.Length; i+=bPP)
                    {
                        pix[ii++] = new FastColor(bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3]);
                    }
                    break;
                case TextureFormat.Indexed8:
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        pix[ii++] = new FastColor(bytes[i]);
                    }
                    break;
                case TextureFormat.RGB24:
                    for (int i = 0; i < bytes.Length; i += bPP)
                    {
                        pix[ii++] = new FastColor(bytes[i], bytes[i + 1], bytes[i + 2], 255);
                    }
                    break;
                case TextureFormat.RGB565:
                    for (int i = 0; i < bytes.Length; i += bPP)
                    {
                        byte lo = bytes[i];
                        byte hi = bytes[i + 1];

                        pix[ii++] = new FastColor(
                            From5Bit((byte)(lo & 0b11111)),
                            From6Bit((byte)(((lo & 0b11100000) >> 5) + ((hi & 0b111) << 3))), 
                            From5Bit((byte)((hi & 0b11111000) >> 3)), 255);
                    }
                    break;
                case TextureFormat.ARGB555:
                case TextureFormat.RGB555:
                    for (int i = 0; i < bytes.Length; i += bPP)
                    {
                        byte lo = bytes[i];
                        byte hi = bytes[i + 1];

                        pix[ii++] = new FastColor(
                            From5Bit((byte)(lo & 0b11111)),
                            From5Bit((byte)(((lo & 0b1110000) >> 5) + ((hi & 0b11) << 3))),
                            From5Bit((byte)((hi & 0b1111100) >> 2)), 
                            (byte)(((hi & 0b10000000) >> 7) != 0 ? 255 : 0));
                    }
                    break;
            }
            return pix;
        }

        public static void WriteColors(this BinaryWriter bw, IList<FastColor> colors) => WriteColors(bw, colors, colors.Count, TextureFormat.RGBA32, false);
        public static void WriteColors(this BinaryWriter bw, IList<FastColor> colors, int count) => WriteColors(bw, colors, count, TextureFormat.RGBA32, false);
        public static void WriteColors(this BinaryWriter bw, IList<FastColor> colors, TextureFormat pFmt) => WriteColors(bw, colors, colors.Count, pFmt, false);
        public static void WriteColors(this BinaryWriter bw, IList<FastColor> colors, int count, TextureFormat pFmt, bool reverse)
        {
            if(pFmt.RequiresBits() && bw is BitWriter bwI)
            {
                WriteColors(bwI, colors, count, pFmt, reverse);
                return;
            }
            count = colors.Count < count ? colors.Count : count;
            for (int i = 0; i < count; i++)
            {
                WriteColorInternal(bw, colors[i], pFmt, reverse);
            }
        }

        public static void WriteColors(this BinaryWriter bw, IList<FastColor> colors, int width, int height, TextureFormat pFmt, bool reverse, int bytePadding = 0)
        {
            if(bytePadding < 1) { WriteColors(bw, colors, width * height, pFmt, reverse); return; }
            if (pFmt.RequiresBits() && bw is BitWriter bwI)
            {
                WriteColors(bwI, colors, width, height, pFmt, reverse, bytePadding);
                return;
            }

            byte[] temp = new byte[bytePadding];
            for (int y = 0; y < height; y++)
            {
                int yY = y * width;
                for (int x = 0; x < width; x++)
                {
                    int i = yY + x;
                    WriteColorInternal(bw, colors[i], pFmt, reverse);
                }
                bw.Write(temp);
            }
        }

        public static FastColor[] ReadColors(this BinaryReader br, int count, bool reverse = false) => ReadColors(br, count, TextureFormat.RGBA32, reverse);
        public static FastColor[] ReadColors(this BinaryReader br, int count, TextureFormat pFmt, bool reverse = false)
        {
            FastColor[] colors = new FastColor[count];
            ReadColors(br, colors, count, pFmt);
            return colors;
        }

        public static int ReadColors(this BinaryReader br, IList<FastColor> colors, bool reverse = false) => ReadColors(br, colors, colors.Count, TextureFormat.RGBA32, reverse);
        public static int ReadColors(this BinaryReader br, IList<FastColor> colors, TextureFormat pFmt, bool reverse = false) => ReadColors(br, colors, colors.Count, pFmt, reverse);
        public static int ReadColors(this BinaryReader br, IList<FastColor> colors, int count, bool reverse = false) => ReadColors(br, colors, count, TextureFormat.RGBA32, reverse);
        public static int ReadColors(this BinaryReader br, IList<FastColor> colors, int count, TextureFormat pFmt, bool reverse = false)
        {
            if (pFmt.RequiresBits() && br is BitReader brI) { return ReadColors(brI, colors, count, pFmt, reverse); }
            count = colors.Count < count ? colors.Count : count;
            for (int i = 0; i < count; i++)
            {
                colors[i] = ReadColorInternal(br, pFmt, reverse);
            }
            return count;
        }

        public static void WriteColors(this BitWriter bw, IList<FastColor> colors) => WriteColors(bw, colors, colors.Count, TextureFormat.RGBA32, false);
        public static void WriteColors(this BitWriter bw, IList<FastColor> colors, int count) => WriteColors(bw, colors, count, TextureFormat.RGBA32, false);
        public static void WriteColors(this BitWriter bw, IList<FastColor> colors, TextureFormat pFmt) => WriteColors(bw, colors, colors.Count, pFmt, false);
        public static void WriteColors(this BitWriter bw, IList<FastColor> colors, int count, TextureFormat pFmt, bool reverse)
        {
            count = colors.Count < count ? colors.Count : count;
            for (int i = 0; i < count; i++)
            {
                WriteColor(bw, colors[i], pFmt, reverse);
            }
        }

        public static void WriteColors(this BitWriter bw, FastColor[] colors, int width, int height, TextureFormat pFmt, bool reverse, int bytePadding)
        {
            if (bytePadding < 1) { WriteColors(bw, colors, width * height, pFmt, reverse); return; }

            byte[] temp = new byte[bytePadding];
            for (int y = 0; y < height; y++)
            {
                int yY = y * width;
                for (int x = 0; x < width; x++)
                {
                    int i = yY + x;
                    WriteColor(bw, colors[i], pFmt, reverse);
                }
                bw.Write(temp);
            }
        }

        public static FastColor[] ReadColors(this BitReader br, int count, bool reverse = false) => ReadColors(br, count, TextureFormat.RGBA32, reverse);
        public static FastColor[] ReadColors(this BitReader br, int count, TextureFormat pFmt, bool reverse = false)
        {
            FastColor[] colors = new FastColor[count];
            ReadColors(br, colors, count, pFmt, reverse);
            return colors;
        }

        public static int ReadColors(this BitReader br, IList<FastColor> colors, int count, bool reverse = false) => ReadColors(br, colors, count, TextureFormat.RGBA32, reverse);
        public static int ReadColors(this BitReader br, IList<FastColor> colors, TextureFormat pFmt, bool reverse = false) => ReadColors(br, colors, colors.Count, pFmt, reverse);
        public static int ReadColors(this BitReader br, IList<FastColor> colors, bool reverse = false) => ReadColors(br, colors, colors.Count, TextureFormat.RGBA32, reverse);
        public static int ReadColors(this BitReader br, IList<FastColor> colors, int count, TextureFormat pFmt, bool reverse = false)
        {
            count = colors.Count < count ? colors.Count : count;
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
        public static void WriteColor(this BinaryWriter bw, FastColor color) => bw.Write((uint)color);

        public static FastColor ReadColor(this BinaryReader br, TextureFormat pFmt, bool reverse)
        {
            if (pFmt.RequiresBits()) { return br is BitReader brI ? ReadColor(brI, pFmt, reverse) : FastColor.clear; }
            return ReadColorInternal(br, pFmt, reverse);
        }
        public static void WriteColor(this BinaryWriter bw, FastColor color, TextureFormat pFmt)
        {
            if (pFmt.RequiresBits() && bw is BitWriter bwI) 
            {
                WriteColor(bwI, color, pFmt);
                return;
            }
            WriteColorInternal(bw, color, pFmt);
        }

        public static unsafe void ReadColors(this BinaryReader br, Texture texture, PixelByteOrder orderSrc)
        {
            byte[] scanBuffer = new byte[texture.ScanSize];
            byte* scan = (byte*)texture.LockBits();

            fixed(byte* scanBuf = scanBuffer)
            {
                if (orderSrc == PixelByteOrder.RGBA | texture.Format == TextureFormat.ARGB555 | (texture.Format == TextureFormat.Indexed))
                {
                    for (int i = 0; i < texture.Height; i++)
                    {
                        br.Read(scanBuffer, 0, texture.ScanSize);
                        BufferUtils.Memcpy(scanBuf, scan, texture.ScanSize);
                        scan += texture.ScanSize;
                    }
                    texture.UnlockBits();
                    return;
                }

                int h = texture.Height;
                byte* scanPtr;
                switch (orderSrc)
                {
                    case PixelByteOrder.ARGB:
                        scanPtr = scanBuf;
                        while (h-- > 0)
                        {
                            br.Read(scanBuffer, 0, texture.ScanSize);
                            for (int i = 0; i < texture.ScanSize; i = +texture.BytesPerPixel)
                            {
                                for (int j = 0; j < texture.BytesPerPixel; j++)
                                {
                                    switch (j)
                                    {
                                        default: scan[3] = *scanPtr++; break;
                                        case 1: scan[0] = *scanPtr++; break;
                                        case 2: scan[1] = *scanPtr++; break;
                                        case 3: scan[2] = *scanPtr++; break;
                                    }
                                }
                                scan += texture.BytesPerPixel;
                            }
                        }
                        break;

                    case PixelByteOrder.ABGR:
                        scanPtr = scanBuf;
                        while (h-- > 0)
                        {
                            br.Read(scanBuffer, 0, texture.ScanSize);
                            for (int i = 0; i < texture.ScanSize; i = +texture.BytesPerPixel)
                            {
                                for (int j = 0; j < texture.BytesPerPixel; j++)
                                {
                                    switch (j)
                                    {
                                        default: scan[3] = *scanPtr++; break;
                                        case 1: scan[2] = *scanPtr++; break;
                                        case 2: scan[1] = *scanPtr++; break;
                                        case 3: scan[0] = *scanPtr++; break;
                                    }
                                }
                                scan += texture.BytesPerPixel;
                            }
                        }
                        break;
                }
                texture.UnlockBits();
            }
        }

        public static FastColor ReadColor(this BitReader br, TextureFormat pFmt, bool reverse)
        {
            byte r, g, b, a;
            switch (pFmt)
            {
                default: return new FastColor(br.ReadInt32());
                case TextureFormat.RGBA32: return new FastColor(br.ReadInt32());

                case TextureFormat.Indexed4:
                    return new FastColor(br.ReadByte(4));
                case TextureFormat.Indexed8:
                    return new FastColor(br.ReadByte(8));
                case TextureFormat.RGB565:
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
                case TextureFormat.RGB555:
                case TextureFormat.ARGB555:
                    bool bA = pFmt == TextureFormat.RGB555;
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
                case TextureFormat.RGB24:
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
        public static void WriteColor(this BitWriter bw, FastColor color, TextureFormat pFmt, bool reverse = false)
        {
            switch (pFmt)
            {
                default: bw.Write((uint)color, 32); break;
                case TextureFormat.Indexed4:
                    bw.Write(color.r, 4);
                    break;
                case TextureFormat.Indexed8:
                    bw.Write(color.r);
                    break;
                case TextureFormat.RGB565:
                    if (reverse)
                    {
                        bw.Write(To5Bit(color.b), 5);
                        bw.Write(To6Bit(color.g), 6);
                        bw.Write(To5Bit(color.r), 5);
                        break;
                    }

                    bw.Write(To5Bit(color.r), 5);
                    bw.Write(To6Bit(color.g), 6);
                    bw.Write(To5Bit(color.b), 5);
                    break;
                case TextureFormat.RGB555:
                case TextureFormat.ARGB555:
                    if (reverse)
                    {
                        bw.Write(To5Bit(color.b), 5);
                        bw.Write(To6Bit(color.g), 5);
                        bw.Write(To5Bit(color.r), 5);
                        bw.Write(pFmt != TextureFormat.ARGB555 | color.a > 127);
                        break;
                    }
                    bw.Write(pFmt != TextureFormat.ARGB555 | color.a > 127);
                    bw.Write(To5Bit(color.r), 5);
                    bw.Write(To5Bit(color.g), 5);
                    bw.Write(To5Bit(color.b), 5);
                    break;
                case TextureFormat.RGB24:
                    if (reverse)
                    {
                        bw.Write(color.b);
                        bw.Write(color.g);
                        bw.Write(color.r);
                        break;
                    }
                    bw.Write(color.r);
                    bw.Write(color.g);
                    bw.Write(color.b);
                    break;
                case TextureFormat.RGBA32:
                    if (reverse)
                    {
                        bw.Write(color.b);
                        bw.Write(color.g);
                        bw.Write(color.r);
                        bw.Write(color.a);
                        break;
                    }
                    bw.Write((uint)color, 32); 
                    break;
                    //case 64:
                    //    bw.Write((ushort)(color.r * BYTE_TO_USHORT));
                    //    bw.Write((ushort)(color.g * BYTE_TO_USHORT));
                    //    bw.Write((ushort)(color.b * BYTE_TO_USHORT));
                    //    bw.Write((ushort)(color.a * BYTE_TO_USHORT));
                    //    break;
            }
        }

        private static FastColor ReadColorInternal(BinaryReader br, TextureFormat pFmt, bool reverse)
        {
            ushort val;
            switch (pFmt)
            {
                default: return ReadColor(br, reverse ? 1 : 0);
                case TextureFormat.Indexed8:
                    return new FastColor(br.ReadByte());
                case TextureFormat.RGB555:
                case TextureFormat.ARGB555:
                    bool hasA = pFmt == TextureFormat.RGB555;

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

                case TextureFormat.RGB565:
                    val = br.ReadUInt16();
                    if (reverse)
                    {
                        return new FastColor(
                            From5Bit((byte)((val >> 11) & 0x1F)),
                            From6Bit((byte)(((val >> 5) & 0x3F))),
                            From5Bit((byte)((val & 0x1F))), 255);
                    }
                    return new FastColor(
                         From5Bit((byte)((val & 0x1F))),
                         From6Bit((byte)((val & 0x7E0) >> 6)),
                         From5Bit((byte)((val & 0xF800) >> 11)), 255);

                case TextureFormat.RGB24:
                    if (reverse)
                    {
                        byte b = br.ReadByte();
                        byte g = br.ReadByte();
                        byte r = br.ReadByte();
                        return new FastColor(r, g, b, 255);
                    }
                    return new FastColor(br.ReadByte(), br.ReadByte(), br.ReadByte(), 255);
                case TextureFormat.RGBA32: return ReadColor(br, reverse ? 1 : 0);
                //case 64:
                //    return new FastColor(
                //    (byte)(br.ReadUInt16() * USHORT_TO_BYTE),
                //    (byte)(br.ReadUInt16() * USHORT_TO_BYTE),
                //    (byte)(br.ReadUInt16() * USHORT_TO_BYTE),
                //    (byte)(br.ReadUInt16() * USHORT_TO_BYTE));
            }
        }
        private static void WriteColorInternal(BinaryWriter bw, FastColor color, TextureFormat pFmt, bool reverse = false)
        {
            switch (pFmt)
            {
                default: WriteColor(bw, color); break;
                case TextureFormat.Indexed8:
                    bw.Write(color.r);
                    break;
                case TextureFormat.RGB555:
                case TextureFormat.RGB565:
                    if (reverse)
                    {
                        bw.Write(color.g);
                        bw.Write(color.r);
                        break;
                    }
                    bw.Write(color.r);
                    bw.Write(color.g);
                    break;
                case TextureFormat.RGB24:
                    if (reverse)
                    {
                        bw.Write(color.b);
                        bw.Write(color.g);
                        bw.Write(color.r);
                        break;
                    }
                    bw.Write(color.r);
                    bw.Write(color.g);
                    bw.Write(color.b);
                    break;
                case TextureFormat.RGBA32:
                    if (reverse)
                    {
                        bw.Write(color.a);
                        bw.Write(color.b);
                        bw.Write(color.g);
                        bw.Write(color.r);
                        break;
                    }
                    WriteColor(bw, color);
                    break;
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
