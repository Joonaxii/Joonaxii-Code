using System;
using System.IO;

namespace Joonaxii.Data
{
    public struct MagicHeader : IComparable<MagicHeader>
    {
        public static MagicHeader none { get; } = new MagicHeader(HeaderType.NONE, new MagicByte[0]);

        public int Length { get => _magicBytes.Length; }
        public HeaderType GetHeaderType { get => _headerType; }
        private HeaderType _headerType;

        private Action<BinaryWriter, MagicByte[]> _defaultWrite;

        private MagicByte[] _magicBytes;
        public MagicHeader(HeaderType type, MagicByte[] bytes) : this(type, bytes, null) { }

        public MagicHeader(HeaderType type, MagicByte[] bytes, Action<BinaryWriter, MagicByte[]> defaultHeaderWrite)
        {
            _headerType = type;
            _magicBytes = bytes;
            _defaultWrite = defaultHeaderWrite;
        }

        public long IndexOf(BinaryReader br, long start)
        {
            var stream = br.BaseStream;
            long prevPos = stream.Position;

            stream.Seek(start, SeekOrigin.Begin);
            int bytesDone = 0;
            long startA = start;
            long pos = start;

            while (stream.Position < stream.Length)
            {
                byte b = br.ReadByte();
                pos++;
                if (_magicBytes[bytesDone++].IsValid(b))
                {
                    if (bytesDone >= Length) { return startA; }
                    continue;
                }
                bytesDone = 0;
                startA = pos;
            }
            stream.Seek(prevPos, SeekOrigin.Begin);
            return -1;
        }

        public bool HasHeader(Stream stream, long originalPos, bool moveBackOnSuccess = false)
        {
            for (int i = 0; i < _magicBytes.Length; i++)
            {
                int b = stream.ReadByte();
                if (b < 0 || !_magicBytes[i].IsValid((byte)b))
                {
                    stream.Seek(originalPos, SeekOrigin.Begin);
                    return false;
                }
            }

            if (moveBackOnSuccess)
            {
                stream.Seek(originalPos, SeekOrigin.Begin);
            }
            return true;
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

        public void WriteHeader(BinaryWriter bw)
        {
            if (_defaultWrite != null)
            {
                _defaultWrite.Invoke(bw, _magicBytes);
                return;
            }
            WriteHeader(bw, null);
        }
        public void WriteHeader(BinaryWriter bw, byte[] unkownBytes, int len = -1)
        {
            int uI = 0;
            len = len < 1 ? _magicBytes.Length : len > _magicBytes.Length ? _magicBytes.Length : len;
            for (int i = 0; i < len; i++)
            {
                var byt = _magicBytes[i];
                if (byt.GetByte(out byte val))
                {
                    bw.Write(val);
                    continue;
                }

                if (unkownBytes == null || unkownBytes.Length < uI)
                {
                    bw.Write((byte)0);
                    continue;
                }
                bw.Write(unkownBytes[uI]);
            }
        }

        public int CompareTo(MagicHeader other) => other._magicBytes.Length.CompareTo(_magicBytes.Length);
    }
}
