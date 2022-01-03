using System;
using System.IO;

namespace Joonaxii.Data
{
    public struct MagicHeader : IComparable<MagicHeader>
    {
        public static MagicHeader unknown { get; } = new MagicHeader(HeaderType.UNKNOWN, new MagicByte[0]);
        public HeaderType GetHeaderType { get => _headerType; }

        private HeaderType _headerType;

        private MagicByte[] _magicBytes;
        public MagicHeader(HeaderType type, MagicByte[] bytes)
        {
            _headerType = type;
            _magicBytes = bytes;
        }

        public bool HasHeader(BinaryReader br, long originalPos, bool moveBackOnSuccess = false)
        {
            for (int i = 0; i < _magicBytes.Length; i++)
            {
                byte b = br.ReadByte();
                if (!_magicBytes[i].IsValid(b))
                {
                    br.BaseStream.Seek(originalPos, SeekOrigin.Begin);
                    return false; 
                }
            }

            if (moveBackOnSuccess)
            {
                br.BaseStream.Seek(originalPos, SeekOrigin.Begin);
            }
            return true;
        }

        public bool HasHeader(byte[] bytes) => HasHeader(bytes, 0);
        public bool HasHeader(byte[] bytes, int start)
        {
            int len = bytes.Length - start;
            if (len < 1 | len < _magicBytes.Length) { return false; }
            for (int i = start; i < start + _magicBytes.Length; i++)
            {
                if (!_magicBytes[i].IsValid(bytes[i])) { return false; }
            }
            return true;
        }

        public int CompareTo(MagicHeader other) => other._magicBytes.Length.CompareTo(_magicBytes.Length);
    }
}
