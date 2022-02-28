using Joonaxii.MathJX;
using System;
using System.Runtime.InteropServices;

namespace Joonaxii.Image
{
    [StructLayout(LayoutKind.Explicit, Size = 4), Serializable]
    public struct FastColor : IColor, IEquatable<FastColor>
    {
        public static FastColor clear { get; } = new FastColor(0, 0, 0, 0);
        public static FastColor black { get; } = new FastColor(0, 0, 0);
        public static FastColor white { get; } = new FastColor(255, 255, 255);

        public byte this[int i]
        {
            get
            {
                switch (i)
                {
                    default: throw new IndexOutOfRangeException($"Index outside of the range 0 - 3");
                    case 0: return r;
                    case 1: return g;
                    case 2: return b;
                    case 3: return a;
                }
            }

            set
            {
                switch (i)
                {
                    default: throw new IndexOutOfRangeException($"Index outside of the range 0 - 3");
                    case 0: r = value; break;
                    case 1: g = value; break;
                    case 2: b = value; break;
                    case 3: a = value; break;
                }
            }
        }

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

        public FastColor(byte v, byte a)
        {
            _rgba = 0;
            this.r = v;
            this.g = v;
            this.b = v;
            this.a = a;
        }

        public FastColor(int rgba) : this()
        {
            _rgba = rgba;
        }

        public static explicit operator int(FastColor c) => c._rgba;
        public static explicit operator uint(FastColor c) => (uint)c._rgba;

        public override bool Equals(object obj) => obj is FastColor color && Equals(color);
        public bool Equals(FastColor other) => _rgba == other._rgba;
        public override int GetHashCode() => _rgba;

        public float GetScalar() => r + b + g + a;

        public static bool operator <(FastColor cA, FastColor cB) => (cA.r < cB.r) & (cA.g < cB.g) & (cA.b < cB.b) & (cA.a < cB.a);
        public static bool operator >(FastColor cA, FastColor cB) => (cA.r > cB.r) & (cA.g > cB.g) & (cA.b > cB.b) & (cA.a > cB.a);

        public static bool operator <=(FastColor cA, FastColor cB) => (cA.r <= cB.r) & (cA.g <= cB.g) & (cA.b <= cB.b) & (cA.a <= cB.a);
        public static bool operator >=(FastColor cA, FastColor cB) => (cA.r >= cB.r) & (cA.g >= cB.g) & (cA.b >= cB.b) & (cA.a >= cB.a);

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
            int r = (c0.r - c1.r);
            int g = (c0.g - c1.g);
            int b = (c0.b - c1.b);
            int a = (c0.a - c1.a);

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

        public static FastColor Lerp(FastColor a, FastColor b, float t) => (FastColor)a.Lerp(b, t);
        public IColor Lerp(IColor to, float t)
        {
            to.GetValues(out float r1, out float g1, out float b1, out float a1);
            int r = (int)Math.Round(this.r + (t * (r1 - this.r)));
            int g = (int)Math.Round(this.g + (t * (g1 - this.g)));
            int b = (int)Math.Round(this.b + (t * (b1 - this.b)));
            int a = (int)Math.Round(this.a + (t * (a1 - this.a)));
            return new FastColor((byte)r, (byte)g, (byte)b, (byte)a);
        }

        public float InverseLerp(IColor to, IColor t)
        {
            to.GetValues(out float r1, out float g1, out float b1, out float a1);
            t.GetValues(out float r2, out float g2, out float b2, out float a2);

            Vector4 c0 = new Vector4(r, g, b, a);
            Vector4 c1 = new Vector4(r1, g1, b1, a1);
            Vector4 v = new Vector4(r2, g2, b2, a2);

            return Vector4.InverseLerp(c0, c1, v);
        }

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

        public void SetAlpha(byte alpha)
        {
            a = alpha;
        }

        public void Set(byte gray)
        {
            r = gray;
            g = gray;
            b = gray;
        }

        public void Set(byte gray, byte alpha)
        {
            r = gray;
            g = gray;
            b = gray;
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

        public void GetValues(out float v0, out float v1, out float v2, out float v3)
        {
            v0 = r;
            v1 = g;
            v2 = b;
            v3 = a;
        }
    }
}
