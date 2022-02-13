using Joonaxii.MathJX;
using System;
using System.Runtime.InteropServices;

namespace Joonaxii.Image
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct HSVColor : IColor
    {
        [FieldOffset(0)] private int _hsv;

        [FieldOffset(0)] public short h;
        [FieldOffset(2)] public byte s;
        [FieldOffset(3)] public byte v;

        public HSVColor(short h, byte s, byte v)
        {
            _hsv = 0;

            this.h = h;
            this.s = s;
            this.v = v;
        }

        public static implicit operator HSVColor(FastColor c) => ColorConverter.RGBToHSV(c);
        public static implicit operator HSLColor(HSVColor c) => ColorConverter.HSVToHSL(c);
        public static implicit operator FastColor(HSVColor c) => ColorConverter.HSVToRGB(c);
        public static implicit operator HSVColor(HSLColor c) => ColorConverter.HSLToHSV(c);

        public static HSVColor Lerp(HSVColor a, HSVColor b, float t) => (HSVColor)a.Lerp(b, t);
        public IColor Lerp(IColor to, float t)
        {
            to.GetValues(out float h1, out float s1, out float v1, out float a1);
            short h = (short)Math.Round((float)Maths.LerpAngle(this.h, h1, t));
            byte s = (byte)(this.s + (t * (s1 - this.s)));
            byte v = (byte)(this.v + (t * (v1 - this.v)));
            return new HSVColor(h, s, v);
        }

        public float InverseLerp(IColor to, IColor t)
        {
            to.GetValues(out float r1, out float g1, out float b1, out float a1);
            t.GetValues(out float r2, out float g2, out float b2, out float a2);

            Vector3 tmp = new Vector3(
                Maths.InverseLerpAngle(h, r1, r2), 
                Maths.InverseLerp(s, g1, g2), 
                Maths.InverseLerp(v, b1, b2));
            return tmp.Magnitude;
        }

        public static HSVColor operator -(HSVColor c0, HSVColor c1)
        {
            int h = (c0.h - c1.h);
            int s = (c0.s - c1.s);
            int v = (c0.v - c1.v);

            return new HSVColor(
                (short)(h < 0 ? 0 : h > 360 ? 360 : h),
                (byte)(s < 0 ? 0 : s > 100 ? 100 : s),
                (byte)(v < 0 ? 0 : v > 100 ? 100 : v));
        }

        public static HSVColor operator +(HSVColor c0, HSVColor c1)
        {
            float h = (c0.h + c1.h);
            float s = (c0.s + c1.s);
            float v = (c0.v + c1.v);

            return new HSVColor(
                (short)(h < 0 ? 0 : h > 360 ? 360 : h),
                (byte)(s < 0 ? 0 : s > 100 ? 100 : s),
                (byte)(v < 0 ? 0 : v > 100 ? 100 : v));
        }

        public static HSVColor operator *(float v, HSVColor c) => c * v;
        public static HSVColor operator *(HSVColor c, float m)
        {
            float h = (c.h * m);
            float s = (c.s * m);
            float v = (c.v * m);

            return new HSVColor(
                (short)(h < 0 ? 0 : h > 360 ? 360 : h),
                (byte)(s < 0 ? 0 : s > 100 ? 100 : s),
                (byte)(v < 0 ? 0 : v > 100 ? 100 : v));
        }

        public override bool Equals(object obj) => obj is HSVColor color && Equals(color);

        public bool Equals(HSVColor other) => _hsv == other._hsv;

        public override int GetHashCode() => _hsv;

        public static bool operator ==(HSVColor left, HSVColor right) => left.Equals(right);
        public static bool operator !=(HSVColor left, HSVColor right) => !(left == right);

        public override string ToString() => $"HSV: ({h}, {s}, {v})";

        public void GetValues(out float v0, out float v1, out float v2, out float v3)
        {
            v0 = h;
            v1 = s;
            v2 = v;
            v3 = 0;
        }
    }
}