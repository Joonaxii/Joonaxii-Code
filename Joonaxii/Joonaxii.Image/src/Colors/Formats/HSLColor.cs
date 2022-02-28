using Joonaxii.MathJX;
using System;
using System.Runtime.InteropServices;

namespace Joonaxii.Image
{
    [StructLayout(LayoutKind.Explicit, Size=4)]
    public struct HSLColor : IColor, IEquatable<HSLColor>
    {
        [FieldOffset(0)] private int _hsl;

        [FieldOffset(0)] public short h;
        [FieldOffset(2)] public byte s;
        [FieldOffset(3)] public byte l;

        public HSLColor(short h, byte s, byte l)
        {
            _hsl = 0;

            this.h = h;
            this.s = s;
            this.l = l;
        }       
        
        public static implicit operator HSLColor(FastColor c) => ColorConverter.RGBToHSL(c);
        public static implicit operator HSLColor(HSVColor c) => ColorConverter.HSVToHSL(c);
        public static implicit operator FastColor(HSLColor c) => ColorConverter.HSLToRGB(c);
        public static implicit operator HSVColor(HSLColor c) => ColorConverter.HSLToHSV(c);

        public static HSLColor Lerp(HSLColor a, HSLColor b, float t) => (HSLColor)a.Lerp(b, t);
        public IColor Lerp(IColor to, float t)
        {
            to.GetValues(out float h1, out float s1, out float l1, out float a1);
            int h = (int)Math.Round((float)Maths.LerpAngle(this.h, h1, t));
            int s = (int)(this.s + (t * (s1 - this.s)));
            int l = (int)(this.l + (t * (l1 - this.l)));
            return new HSLColor((short)h, (byte)s, (byte)l);
        }

        public static HSLColor operator -(HSLColor c0, HSLColor c1)
        {
            int h = (c0.h - c1.h);
            int s = (c0.s - c1.s);
            int l = (c0.l - c1.l);

            return new HSLColor(
                (short)(h < 0 ? 0 : h > 360 ? 360 : h),
                (byte) (s < 0 ? 0 : s > 100 ? 100 : s),
                (byte) (l < 0 ? 0 : l > 100 ? 100 : l));
        }

        public static HSLColor operator +(HSLColor c0, HSLColor c1)
        {
            float h = (c0.h + c1.h);
            float s = (c0.s + c1.s);
            float l = (c0.l + c1.l);

            return new HSLColor(
                (short)(h < 0 ? 0 : h > 360 ? 360 : h),
                (byte)(s < 0 ? 0  : s > 100 ? 100 : s),
                (byte)(l < 0 ? 0  : l > 100 ? 100 : l));
        }

        public static HSLColor operator *(float v, HSLColor c) => c * v;
        public static HSLColor operator *(HSLColor c, float v)
        {
            float h = (c.h * v);
            float s = (c.s * v);
            float l = (c.l * v);

            return new HSLColor(
                (short)(h < 0 ? 0 : h > 360 ? 360 : h),
                (byte)(s < 0 ? 0 : s > 100 ? 100 : s),
                (byte)(l < 0 ? 0 : l > 100 ? 100 : l));
        }

        public override bool Equals(object obj) => obj is HSLColor color && Equals(color);

        public bool Equals(HSLColor other) => _hsl == other._hsl;

        public override int GetHashCode() => _hsl;

        public static bool operator ==(HSLColor left, HSLColor right) => left.Equals(right);
        public static bool operator !=(HSLColor left, HSLColor right) => !(left == right);

        public override string ToString() => $"HSL: ({h}, {s}, {l})";

        public void GetValues(out float v0, out float v1, out float v2, out float v3)
        {
            v0 = h;
            v1 = s;
            v2 = l;
            v3 = 0;
        }
    }
}