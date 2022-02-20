using System.Runtime.InteropServices;

namespace Joonaxii.Image.Misc.VTF
{
    [StructLayout(LayoutKind.Explicit, Size=8)]
    public struct VTFResource
    {
        public VTFTag Tag { get => (VTFTag)(_tag & 0xFF_FF_FF); }
        public byte Flags { get => (byte)((_tag >> 24) & 0xFF); }

        public uint Offset { get => _offset; }

        [FieldOffset(0)] private uint _tag;
        [FieldOffset(8)] private uint _offset;

        public VTFResource(uint tagFlag, uint offset)
        {
            _tag = tagFlag;
            _offset = offset;
        }

        public override string ToString() => $"{Tag}, {Flags:0x00}, {Offset} bytes";
    }
}