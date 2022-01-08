using Joonaxii.IO;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Joonaxii.Data.Compression.RLE
{
    [StructLayout(LayoutKind.Explicit, Size = 3)]
    public struct RLEValue : IEquatable<RLEValue>, IBinaryIO<RLEValue>
    {
        public uint ToUInt32 { get => (uint)ToInt32; }
        public int ToInt32 { get => GetHashCode(); }

        [FieldOffset(0)] public ushort lo;
        [FieldOffset(2)] public byte hi;

        public RLEValue(ushort lo, byte hi)
        {
            this.lo = lo;
            this.hi = hi;
        }

        public RLEValue(uint value)
        {
            lo = (ushort)value;
            hi = (byte)(value >> 16);
        }

        public RLEValue(int value) : this((uint)value) { }

        public override bool Equals(object obj) => obj is RLEValue value && Equals(value);
        public bool Equals(RLEValue other) => lo == other.lo & hi == other.hi;

        public override int GetHashCode() => (lo + (hi << 16));

        public RLEValue Read(BinaryReader br)
        {
            lo = br.ReadUInt16();
            hi = br.ReadByte();
            return this;
        }

        public RLEValue Write(BinaryWriter bw)
        {
            bw.Write(lo);
            bw.Write(hi);
            return this;
        }

        public static bool operator ==(RLEValue left, RLEValue right) => left.Equals(right);
        public static bool operator !=(RLEValue left, RLEValue right) => !(left == right);
    }
}