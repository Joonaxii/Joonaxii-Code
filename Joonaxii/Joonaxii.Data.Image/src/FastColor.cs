using System;
using System.Runtime.InteropServices;

namespace Joonaxii.Data.Image
{
    [StructLayout(LayoutKind.Explicit, Size = 4), Serializable]
    public struct FastColor : IEquatable<FastColor>
    {
        public static FastColor clear { get; } = new FastColor(0);

        [FieldOffset(0)] private int _rgba;

        [FieldOffset(0)] public byte r;
        [FieldOffset(1)] public byte g;
        [FieldOffset(2)] public byte b;
        [FieldOffset(3)] public byte a;

        public FastColor(byte r, byte g, byte b, byte a)
        {
            _rgba = 0;
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
        public FastColor(byte r, byte g, byte b)
        {
            _rgba = 0;
            this.r = r;
            this.g = g;
            this.b = b;
            a = 255;
        }

        public FastColor(byte v)
        {
            _rgba = 0;
            this.r = v;
            this.g = v;
            this.b = v;
            a = 255;
        }
        public FastColor(int rgba) : this()
        {
            _rgba = rgba;
        }

        public static implicit operator int(FastColor c) => c._rgba;

        public override bool Equals(object obj) => obj is FastColor color && Equals(color);
        public bool Equals(FastColor other) => _rgba == other._rgba;
        public override int GetHashCode() => _rgba;

        public float GetScalar() => r + b + g + a;

        public static FastColor operator *(FastColor c0, FastColor c1)
        {
            int r = (c0.r * c1.r);
            int g = (c0.g * c1.g);
            int b = (c0.b * c1.b);
            int a = (c0.a * c1.a);

            return new FastColor(
                (byte)(r < 0 ? 0 : r > 255 ? 255 : r),
                (byte)(g < 0 ? 0 : g > 255 ? 255 : g),
                (byte)(b < 0 ? 0 : b > 255 ? 255 : b),
                (byte)(a < 0 ? 0 : a > 255 ? 255 : a));
        }

        public static FastColor operator /(FastColor c0, FastColor c1)
        {
            int r = (c0.r / c1.r);
            int g = (c0.g / c1.g);
            int b = (c0.b / c1.b);
            int a = (c0.a / c1.a);

            return new FastColor(
                (byte)(r < 0 ? 0 : r > 255 ? 255 : r),
                (byte)(g < 0 ? 0 : g > 255 ? 255 : g),
                (byte)(b < 0 ? 0 : b > 255 ? 255 : b),
                (byte)(a < 0 ? 0 : a > 255 ? 255 : a));
        }

        public static FastColor operator +(FastColor c0, FastColor c1)
        {
            int r = (c0.r + c1.r);
            int g = (c0.g + c1.g);
            int b = (c0.b + c1.b);
            int a = (c0.a + c1.a);

            return new FastColor(
                (byte)(r < 0 ? 0 : r > 255 ? 255 : r),
                (byte)(g < 0 ? 0 : g > 255 ? 255 : g),
                (byte)(b < 0 ? 0 : b > 255 ? 255 : b),
                (byte)(a < 0 ? 0 : a > 255 ? 255 : a));
        }

        public static FastColor operator -(FastColor c0, FastColor c1)
        {
            int r = (int)(c0.r + c1.r);
            int g = (int)(c0.g + c1.g);
            int b = (int)(c0.b + c1.b);
            int a = (int)(c0.a + c1.a);

            return new FastColor(
                (byte)(r < 0 ? 0 : r > 255 ? 255 : r),
                (byte)(g < 0 ? 0 : g > 255 ? 255 : g),
                (byte)(b < 0 ? 0 : b > 255 ? 255 : b),
                (byte)(a < 0 ? 0 : a > 255 ? 255 : a));
        }

        public static FastColor operator *(float v, FastColor c) => c * v;
        public static FastColor operator *(FastColor c, float v)
        {
            int r = (int)(c.r * v);
            int g = (int)(c.g * v);
            int b = (int)(c.b * v);
            int a = (int)(c.a * v);

            return new FastColor(
                (byte)(r < 0 ? 0 : r > 255 ? 255 : r),
                (byte)(g < 0 ? 0 : g > 255 ? 255 : g),
                (byte)(b < 0 ? 0 : b > 255 ? 255 : b),
                (byte)(a < 0 ? 0 : a > 255 ? 255 : a));
        }

        public static FastColor Scale(FastColor c0, FastColor c1, bool clamp)
        {
            int r = c0.r * c1.r;
            int g = c0.g * c1.g;
            int b = c0.b * c1.b;
            int a = c0.a * c1.a;

            if (clamp)
            {
                r = r < 0 ? 0 : r > 255 ? 255 : r;
                g = g < 0 ? 0 : g > 255 ? 255 : g;
                b = b < 0 ? 0 : b > 255 ? 255 : b;
                a = a < 0 ? 0 : a > 255 ? 255 : a;
            }

            return new FastColor((byte)r, (byte)g, (byte)b, (byte)a);
        }

        public static FastColor Lerp(FastColor c0, FastColor c1, float t) => (c0 + (t * (c1 - c0))); 

        public byte GetAverageRGB()
        {
            int tot = (int)(Math.Round((double)(r + g + b)) * 0.33334d);
            return (byte)(tot < 0 ? 0 : tot > 255 ? 255 : tot);
        }

        public byte GetAverageRGBA()
        {
            int tot = (int)(Math.Round((double)(r + g + b + a)) * 0.25d);
            return (byte)(tot < 0 ? 0 : tot > 255 ? 255 : tot);
        }

        public static FastColor Divide(FastColor c0, FastColor c1, bool clamp)
        {
            int r = c0.r / c1.r;
            int g = c0.g / c1.g;
            int b = c0.b / c1.b;
            int a = c0.a / c1.a;

            if (clamp)
            {
                r = r < 0 ? 0 : r > 255 ? 255 : r;
                g = g < 0 ? 0 : g > 255 ? 255 : g;
                b = b < 0 ? 0 : b > 255 ? 255 : b;
                a = a < 0 ? 0 : a > 255 ? 255 : a;
            }

            return new FastColor((byte)r, (byte)g, (byte)b, (byte)a);
        }

        public static FastColor Add(FastColor c0, FastColor c1, bool clamp) {
            int r = c0.r + c1.r;
            int g = c0.g + c1.g;
            int b = c0.b + c1.b;
            int a = c0.a + c1.a;

            if (clamp)
            {
                r = r < 0 ? 0 : r > 255 ? 255 : r;
                g = g < 0 ? 0 : g > 255 ? 255 : g;
                b = b < 0 ? 0 : b > 255 ? 255 : b;
                a = a < 0 ? 0 : a > 255 ? 255 : a;
            }

            return new FastColor((byte)r, (byte)g, (byte)b, (byte)a);
        }

        public static FastColor Subract(FastColor c0, FastColor c1, bool clamp)
        {
            int r = c0.r - c1.r;
            int g = c0.g - c1.g;
            int b = c0.b - c1.b;
            int a = c0.a - c1.a;

            if (clamp)
            {
                r = r < 0 ? 0 : r > 255 ? 255 : r;
                g = g < 0 ? 0 : g > 255 ? 255 : g;
                b = b < 0 ? 0 : b > 255 ? 255 : b;
                a = a < 0 ? 0 : a > 255 ? 255 : a;
            }

            return new FastColor((byte)r, (byte)g, (byte)b, (byte)a);
        }

        public static bool operator ==(FastColor left, FastColor right) => left.Equals(right);
        public static bool operator !=(FastColor left, FastColor right) => !(left == right);

        public void Set(byte gray, bool setAlpha )
        {
            r = gray;
            g = gray;
            b = gray;
            a = setAlpha ? gray : (byte)255;
        }

        public void Set(byte alpha)
        {
            a = alpha;
        }

        public void Set(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public void Set(byte r, byte g, byte b, bool fullAlpha = false)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            a = fullAlpha ? (byte)255 : a;
        }

        public void Set(int rgba) => _rgba = rgba;

        public override string ToString() => $"RGBA: ({r}, {g}, {b}, {a})";
    }
}
