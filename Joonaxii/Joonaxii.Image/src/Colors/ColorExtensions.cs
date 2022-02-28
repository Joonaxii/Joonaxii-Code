using Joonaxii.MathJX;
using System.Collections.Generic;

namespace Joonaxii.Image
{
    public static class ColorExtensions
    {
        public const float NORMALIZE_BYTE = 1.0f / 255.0f;
        public const float NORMALIZE_5BIT = 1.0f / 31.0f;
        public const float NORMALIZE_6BIT = 1.0f / 63.0f;

        public static ushort ToRGB565(this FastColor cl, bool reverse)
        {
            return (ushort)(reverse ?
                  (To5Bit(cl.b) | (To6Bit(cl.g) << 5) | (To5Bit(cl.g) << 11))
                : (To5Bit(cl.r) | (To6Bit(cl.g) << 5) | (To5Bit(cl.b) << 11)));
        }

        public static ushort ToRGB555(this FastColor cl, bool reverse)
        {
            return (ushort)(reverse ?
                  (To5Bit(cl.b) | (To5Bit(cl.g) << 5) | (To5Bit(cl.g) << 10))
                : (To5Bit(cl.r) | (To5Bit(cl.g) << 5) | (To5Bit(cl.b) << 10)));
        }

        public static ushort ToARGB555(this FastColor cl, bool reverse)
        {
            int alpha = (cl.a > 0 ? 1 : 0);
            return (ushort)(reverse ?
                  (To5Bit(cl.b) | (To5Bit(cl.g) << 5) | (To5Bit(cl.g) << 10) | (alpha << 15))
                : (alpha + (To5Bit(cl.r) << 1) | (To5Bit(cl.g) << 6) | (To5Bit(cl.b) << 11)));
        }

        public static ushort ToRGBA555(this FastColor cl, bool reverse)
        {
            int alpha = (cl.a > 0 ? 1 : 0);
            return (ushort)(reverse ?
                  (alpha + (To5Bit(cl.b) << 1) | (To5Bit(cl.g) << 6) | (To5Bit(cl.g) << 11))
                : (To5Bit(cl.r) | (To5Bit(cl.g) << 5) | (To5Bit(cl.b) << 10) | (alpha << 15)));
        }

        public static FastColor FromRGB565(int v, bool reverse)
        {
            return reverse ?
                new FastColor(From5Bit((v >> 11) & 0x1F), From6Bit((v >> 5) & 0x3F), From5Bit(v & 0x1f)) 
               :new FastColor(From5Bit(v & 0x1f), From6Bit((v >> 5) & 0x3F), From5Bit((v >> 11) & 0x1F));
        }

        public static FastColor FromRGB555(int v, bool reverse)
        {
            return reverse ?
                new FastColor(From5Bit((v >> 10) & 0x1F), From5Bit((v >> 5) & 0x3F), From5Bit(v & 0x1f))
               : new FastColor(From5Bit(v & 0x1f), From5Bit((v >> 5) & 0x1f), From5Bit((v >> 10) & 0x1F));
        }

        public static FastColor FromARGB555(int v, bool reverse)
        {
            return reverse ?
                new FastColor(From5Bit((v >> 10) & 0x1F), From5Bit((v >> 5) & 0x1f), From5Bit(v & 0x1f), (byte)(((v >> 15) & 0x1) != 0 ? 255 : 0))
               :new FastColor(From5Bit((v >> 1) & 0x1f), From5Bit((v >> 6) & 0x1f), From5Bit((v >> 11) & 0x1F), (byte)((v & 0x1) != 0 ? 255 : 0));
        }

        public static FastColor FromRGBA555(this ushort v, bool reverse)
        {
            return reverse ?
                 new FastColor(From5Bit((v >> 11) & 0x1F), From5Bit((v >> 6) & 0x3F), From5Bit((v >> 1) & 0x1f), (byte)((v & 0x1) != 0 ? 255 : 0))
               : new FastColor(From5Bit(v & 0x1f), From5Bit((v >> 5) & 0x3F), From5Bit((v >> 10) & 0x1F), (byte)(((v >> 15) & 0x1) != 0 ? 255 : 0));
        }

        public static byte To5Bit(this int b) => (byte)((b * NORMALIZE_BYTE) * 31);
        public static byte To6Bit(this int b) => (byte)((b * NORMALIZE_BYTE) * 63);

        public static byte From5Bit(this int b) => (byte)((b * NORMALIZE_5BIT) * 255);
        public static byte From6Bit(this int b) => (byte)((b * NORMALIZE_6BIT) * 255);

        public static byte ToGrayscale(this FastColor color)
        {
            float v = (color.r * 0.3f + color.g * 0.59f + color.b * 0.11f);
            return (byte)Maths.Clamp(v, 0, 255);
        }

        public static unsafe FastColor GetIndexed(byte* ptr, int bpp, IList<FastColor> palette)
        {
            int val;
            int p = 0;
            val = 0;
            while (bpp-- > 0)
            {
                val += (*ptr++ << p);
                p += 8;
            }
            return palette[val];
        }

        public static unsafe FastColor GetRGB24(byte* ptr, int bpp, IList<FastColor> palette) 
            => new FastColor(*ptr++, *ptr++, *ptr);

         public static unsafe FastColor GetRGBA32(byte* ptr, int bpp, IList<FastColor> palette) 
            => new FastColor(*ptr++, * ptr++, * ptr++, *ptr);

        public static unsafe FastColor GetGrayscale(byte* ptr, int bpp, IList<FastColor> palette)
            => new FastColor(*ptr);

        public static unsafe FastColor GetGrayscaleAlpha(byte* ptr, int bpp, IList<FastColor> palette)
           => new FastColor(*ptr++, *ptr);

        public static unsafe FastColor GetRGB565(byte* ptr, int bpp, IList<FastColor> palette)
            => FromRGB565(GetValueFromPtr(ptr, bpp), false);

        public static unsafe FastColor GetRGB555(byte* ptr, int bpp, IList<FastColor> palette) 
            => FromRGB555(GetValueFromPtr(ptr, bpp), false);
        
        public static unsafe FastColor GetARGB555(byte* ptr, int bpp, IList<FastColor> palette) 
            => FromARGB555(GetValueFromPtr(ptr, bpp), false);

        public static unsafe void GetIndexedPtr(byte* ptr, byte* ptrData, int bpp, IList<FastColor> palette)
        {
            int val;
            int p = 0;
            val = 0;
            while (bpp-- > 0)
            {
                val += (*ptr++ << p);
                p += 8;
            }

            var c = palette[val];
            *ptrData++ = c.r;
            *ptrData++ = c.a;
            *ptrData++ = c.b;
            *ptrData++ = c.a;
        }

        public static unsafe void GetRGB24Ptr(byte* ptr, byte* ptrData, int bpp, IList<FastColor> palette)
        {
            *ptrData++ = *ptr++;
            *ptrData++ = *ptr++;
            *ptrData++ = *ptr;
            *ptrData = 255;
        }

        public static unsafe void GetRGBA32Ptr(byte* ptr, byte* ptrData, int bpp, IList<FastColor> palette)
        {
            *ptrData++ = *ptr++;
            *ptrData++ = *ptr++;
            *ptrData++ = *ptr++;
            *ptrData = *ptr;
        }

        public static unsafe void GetGrayscalePtr(byte* ptr, byte* ptrData, int bpp, IList<FastColor> palette)
        {
            *ptrData++ = *ptr;
            *ptrData++ = *ptr;
            *ptrData++ = *ptr;
            *ptrData = 255;
        }

        public static unsafe void GetGrayscaleAlphaPtr(byte* ptr, byte* ptrData, int bpp, IList<FastColor> palette)
        {
            *ptrData++ = *ptr;
            *ptrData++ = *ptr;
            *ptrData++ = *ptr++;
            *ptrData = *ptr;
        }

        public static unsafe void GetRGB565Ptr(byte* ptr, byte* ptrData, int bpp, IList<FastColor> palette)
        {
            int v = *ptr++ | (*ptr << 8);
            *ptrData++ = From5Bit(v & 0x1f);
            *ptrData++ = From6Bit((v >> 5) & 0x3F);
            *ptrData++ = From5Bit((v >> 11) & 0x1F);

            *ptrData = 255;
        }
        public static unsafe void GetRGB555Ptr(byte* ptr, byte* ptrData, int bpp, IList<FastColor> palette)
        {
            int v = *ptr++ | (*ptr << 8);

            *ptrData++ = From5Bit((v >> 1) & 0x1f);
            *ptrData++ = From5Bit((v >> 6) & 0x1f);
            *ptrData++ = From5Bit((v >> 11) & 0x1F);
            *ptrData = 255;
        }
        public static unsafe void GetARGB555Ptr(byte* ptr, byte* ptrData, int bpp, IList<FastColor> palette)
        {
            int v = *ptr++ | (*ptr << 8);

            *ptrData++ = From5Bit((v >> 1) & 0x1f);
            *ptrData++ = From5Bit((v >> 6) & 0x1f);
            *ptrData++ = From5Bit((v >> 11) & 0x1F);
            *ptrData = (byte)((v & 0x1) != 0 ? 255 : 0);
        }

        private static unsafe int GetValueFromPtr(byte* ptr, int bpp)
        {
            int val;
            int p = 0;
            val = 0;
            while (bpp-- > 0)
            {
                val += (*ptr++ << p);
                p += 8;
            }
            return val;
        }

        public static unsafe FastColor GetColor(byte* ptr, int bpp, ColorMode format, IList<FastColor> palette)
        {
            switch (format)
            {
                case ColorMode.Indexed:        return GetIndexed(ptr, bpp, palette);
                case ColorMode.RGB24:          return GetRGB24(ptr, bpp, palette);
                case ColorMode.RGBA32:         return GetRGBA32(ptr, bpp, palette);

                case ColorMode.RGB565:         return GetRGB565(ptr, bpp, palette);
                case ColorMode.RGB555:         return GetRGB555(ptr, bpp, palette);
                case ColorMode.ARGB555:        return GetARGB555(ptr, bpp, palette);

                case ColorMode.Grayscale:      return GetGrayscale(ptr, bpp, palette);
                case ColorMode.GrayscaleAlpha: return GetGrayscaleAlpha(ptr, bpp, palette);
            }
            return FastColor.clear;
        }
    }
}