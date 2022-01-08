using System;
using System.Runtime.InteropServices;

namespace Joonaxii.Data.Image
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
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

        public static bool operator ==(FastColor left, FastColor right) => left.Equals(right);
        public static bool operator !=(FastColor left, FastColor right) => !(left == right);

        public void Set(byte gray, bool setAlpha = false)
        {
            r = gray;
            g = gray;
            b = gray;
            a = setAlpha ? gray : (byte)255;
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
