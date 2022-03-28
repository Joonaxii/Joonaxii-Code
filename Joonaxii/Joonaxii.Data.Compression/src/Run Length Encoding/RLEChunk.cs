using Joonaxii.IO;
using Joonaxii.IO.BitStream;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Joonaxii.Data.Compression.RLE
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct RLEChunk : IEquatable<RLEChunk>, IBinaryIO
    {
        public uint ToUInt32 { get => (uint)_union; }
        public int ToInt32 { get => _union; }

        [FieldOffset(0)] private int _union;

        [FieldOffset(0)] public byte count;
        [FieldOffset(1)] public RLEValue value;

        public RLEChunk(byte count, RLEValue value) : this()
        {
            this.count = count;
            this.value = value;
        }

        public override bool Equals(object obj) => obj is RLEChunk chunk && Equals(chunk);
        public bool Equals(RLEChunk other) => _union == other._union;
        public override int GetHashCode() => _union.GetHashCode();

        public RLEChunk Read(BitReader br, byte lenBits, byte valueBits)
        {
            count = br.ReadByte(lenBits);
            value = new RLEValue(br.ReadUInt32(valueBits));
            return this;
        }

        public void Write(BitWriter bw, byte lenBits, byte valueBits)
        {
            bw.Write(count, lenBits);
            bw.Write(value.ToUInt32, valueBits);
        }

        public void Read(BinaryReader br)
        {
            count = br.ReadByte();
            value.Read(br);
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(count);
            value.Write(bw);
        }

        public static bool operator ==(RLEChunk left, RLEChunk right) => left.Equals(right);
        public static bool operator !=(RLEChunk left, RLEChunk right) => !(left == right);
    }
}