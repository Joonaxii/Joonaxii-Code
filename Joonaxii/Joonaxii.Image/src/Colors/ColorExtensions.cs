using Joonaxii.MathJX;

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
               : new FastColor(From5Bit(v & 0x1f), From5Bit((v >> 5) & 0x3F), From5Bit((v >> 10) & 0x1F));
        }

        public static FastColor FromARGB555(int v, bool reverse)
        {
            return reverse ?
                new FastColor(From5Bit((v >> 10) & 0x1F), From5Bit((v >> 5) & 0x3F), From5Bit(v & 0x1f), (byte)(((v >> 15) & 0x1) != 0 ? 255 : 0))
               :new FastColor(From5Bit((v >> 1) & 0x1f), From5Bit((v >> 6) & 0x3F), From5Bit((v >> 11) & 0x1F), (byte)((v & 0x1) != 0 ? 255 : 0));
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
    }
}