using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Joonaxii.Image
{
    [StructLayout(LayoutKind.Explicit, Size = 4), Serializable]
    public struct YCrCb : IColor
    {
        public float Y  { get => ( y / (float)byte.MaxValue); }
        public float CB { get => (cb / (float)byte.MaxValue - 0.5f) * 2.0f; }
        public float CR { get => (cr / (float)byte.MaxValue - 0.5f) * 2.0f; }

        [FieldOffset(0)] private int _rCbCr;

        [FieldOffset(0)] public byte y;
        [FieldOffset(1)] public byte cb;
        [FieldOffset(2)] public byte cr;

        public YCrCb(byte y, byte cb, byte cr) : this()
        {
            this.y = y;
            this.cb = cb;
            this.cr = cr;
        }

        public void GetValues(out float v0, out float v1, out float v2, out float v3)
        {
            v0 = Y;
            v1 = CB;
            v2 = CR;
            v3 = 0;
        }

        public override bool Equals(object obj) => obj is YCrCb color && Equals(color);
        public bool Equals(YCrCb other) => _rCbCr == other._rCbCr;
        public override int GetHashCode() => _rCbCr;

        public override string ToString() => $"YCbCr: ({y} ({Y}), {cb} ({CB}), {cr} ({CR})";

        public IColor Lerp(IColor to, float t)
        {
            throw new NotImplementedException();
        }
    }
}
