using Joonaxii.MathJX;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Joonaxii.IO.BitStream
{
    public class BitReader : BinaryReader
    {
        public bool IsEoF { get => BaseStream.Position >= _streamLen; }

        private Encoding _encoding;
        private Decoder _decoder;

        private const int MAX_CHAR_BYTES = 128;
        private byte[] _charBytes;
        private char[] _charBuffer;
        private int _maxCharsSize;

        private bool _2BytesPerChar;
        private bool _isMemoryStream;

        private ulong _bitBuffer;
        private int _bitBPos;

        private long _streamPos;

        private const int FILE_BUFFER_SIZE = 4096;
        private byte[] _fileBuffer;
        private int _fileBufIndex = -1;

        private long _streamLen;

        public BitReader(Stream input) : this(input, Encoding.UTF8, false) { }
        public BitReader(Stream input, bool leaveOpen) : this(input, Encoding.UTF8, leaveOpen) { }
        public BitReader(Stream input, Encoding encoding) : this(input, encoding, false) { }
        public BitReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, Encoding.UTF8, leaveOpen)
        {
            _bitBuffer = 0;
            _bitBPos = -1;
            _streamPos = input.Position;
            _encoding = encoding;
            _decoder = encoding?.GetDecoder();

            _maxCharsSize = encoding != null ? encoding.GetMaxCharCount(MAX_CHAR_BYTES) : MAX_CHAR_BYTES / 2;

            _2BytesPerChar = encoding is UnicodeEncoding;
            _isMemoryStream = input is MemoryStream;

            if (!_isMemoryStream)
            {
                _fileBuffer = new byte[FILE_BUFFER_SIZE];
            }

            _streamLen = input.Length;
        }

        public override bool ReadBoolean()
        {
            ValidateBuffer(false);
            bool val = _bitBuffer.IsBitSet(_bitBPos++);
            SeekBaseStream(_streamPos);
            return val;
        }

        public override byte ReadByte() => ReadByte(8);
        public override sbyte ReadSByte() => ReadSByte(8);

        public override char ReadChar() => (char)ReadUInt16(16);

        public override short ReadInt16() => ReadInt16(16);
        public override ushort ReadUInt16() => ReadUInt16(16);

        public override int ReadInt32() => ReadInt32(32);
        public override uint ReadUInt32() => ReadUInt32(32);

        public override long ReadInt64() => ReadInt64(64);
        public override ulong ReadUInt64() => ReadUInt64(64);

        public override string ReadString()
        {
            int len = Read7BitInt();
            StringBuilder sb = null;

            if (_encoding == null)
            {
                sb = new StringBuilder();
                for (int i = 0; i < len; i++)
                {
                    sb.Append(ReadChar());
                }
                return sb.ToString();
            }

            if (len <= 0) { return string.Empty; }

            if (_charBytes == null)
            {
                _charBytes = new byte[MAX_CHAR_BYTES];
            }

            if (_charBuffer == null)
            {
                _charBuffer = new char[_maxCharsSize];
            }

            int currPos = 0;
            int n;
            int rLen;
            int cRead;

            while (currPos < len)
            {
                rLen = ((len - currPos) > MAX_CHAR_BYTES) ? MAX_CHAR_BYTES : (len - currPos);
                n = Read(_charBytes, 0, rLen);

                if (n == 0) { break; }

                cRead = _decoder.GetChars(_charBytes, 0, n, _charBuffer, 0);
                if (currPos == 0 & n == len) { return new string(_charBuffer, 0, cRead); }

                if (sb == null) { sb = new StringBuilder(len); }
                sb.Append(_charBuffer, 0, cRead);
                currPos += n;
            }
            return sb == null ? string.Empty : sb.ToString();
        }

        public override int Read() => BaseStream.Position >= _streamLen ? -1 : ReadByte();

        public override int Read(char[] buffer, int index, int count)
        {
            int c = Math.Min(count, buffer.Length);
            Buffer.BlockCopy(ReadChars(count), 0, buffer, index, c * 2);
            return c;
        }
        public override char[] ReadChars(int count)
        {
            char[] chars = new char[count];
            for (int i = 0; i < count; i++)
            {
                chars[i] = ReadChar();
            }
            return chars;
        }

        public override byte[] ReadBytes(int count)
        {
            byte[] bytes = new byte[count];
            for (int i = 0; i < count; i++)
            {
                bytes[i] = ReadByte();
            }
            return bytes;
        }
        public override int Read(byte[] buffer, int index, int count)
        {
            int c = Math.Min(count, buffer.Length);
            Buffer.BlockCopy(ReadBytes(count), 0, buffer, index, c);
            return c;
        }

        public override unsafe float ReadSingle()
        {
            uint tmpBuf = (uint)(ReadByte() | ReadByte() << 8 | ReadByte() << 16 | ReadByte() << 24);
            return *((float*)&tmpBuf);
        }

        public override unsafe double ReadDouble()
        {
            uint lo = (uint)(ReadByte() | ReadByte() << 8 | ReadByte() << 16 | ReadByte() << 24);
            uint hi = (uint)(ReadByte() | ReadByte() << 8 | ReadByte() << 16 | ReadByte() << 24);

            ulong tmp = ((ulong)hi) << 32 | lo;
            return *((double*)&tmp);
        }

        public override decimal ReadDecimal()
        {
            int[] buff = new int[4];
            Buffer.BlockCopy(ReadBytes(16), 0, buff, 0, 16);
            return new Decimal(buff);
        }

        public byte ReadByte(int bits) => (byte)GetValue(bits);
        public sbyte ReadSByte(int bits) => (sbyte)GetValue(bits);

        public short ReadInt16(int bits) => (short)GetValue(bits);
        public ushort ReadUInt16(int bits) => (ushort)GetValue(bits);

        public int ReadInt32(int bits) => (int)GetValue(bits);
        public uint ReadUInt32(int bits) => (uint)GetValue(bits);

        public long ReadInt64(int bits) => (long)GetValue(bits);
        public ulong ReadUInt64(int bits) => GetValue(bits);

        private ulong GetValue(int bits)
        {
            bits = bits < 0 ? 0 : bits > 64 ? 64 : bits;
            if (_bitBPos <= 0)
            {
                if (_bitBPos < 0)
                {
                    ReadBuffer();
                }
                _bitBPos = bits;
                SeekBaseStream(_streamPos);
                return _bitBuffer & ((1UL << bits) - 1);
            }
            ValidateBuffer(false);

            int remBits = 64 - _bitBPos;
            if (remBits >= bits)
            {
                bool lastPart = remBits <= bits;
                ulong selectMask = lastPart ? ulong.MaxValue : ((1UL << (_bitBPos + bits)) - 1L);
                ulong delectMask = ~((1UL << _bitBPos) - 1L);
                ulong masked = (_bitBuffer & (selectMask & delectMask));
                ulong val = masked >> _bitBPos;

                _bitBPos += bits;
                SeekBaseStream(_streamPos);
                return val;
            }

            ulong maskLO = (ulong.MaxValue & ~((1UL << (_bitBPos)) - 1L));
            ulong lo = ((_bitBuffer & maskLO) >> (_bitBPos));

            BaseStream.Seek(_streamPos + 8, SeekOrigin.Begin);
            ReadBuffer();

            int hiBits = bits - remBits;
            ulong mask = ((1UL << hiBits) - 1UL);
            ulong hi = _bitBuffer & mask;

            _bitBPos = hiBits;
            SeekBaseStream(_streamPos);

            return lo | (hi << remBits);
        }

        public byte[] ReadToEnd()
        {
            ValidateBuffer(true);
            int count = (int)(_streamLen - BaseStream.Position);
            return ReadBytes(count);
        }

        public int Read7BitInt()
        {
            int count = 0;
            int shift = 0;

            byte b = ReadByte();
            count |= (b & 0x7F) << shift;
            shift += 7;

            while ((b & 0x80) != 0)
            {
                if (shift >= 5 * 7) { break; }

                b = ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            }
            return count;
        }
        public uint Read7BitUInt() => (uint)Read7BitInt();
        public void DiscardBitBuffer() => ValidateBuffer(true);

        public void ByteAlign()
        {
            if (_bitBPos < 0 | _bitBPos % 8 == 0) { return; }

            _bitBPos = GetNextBitPos();
            SeekBaseStream(BaseStream.Position);
        }

        private void ValidateBuffer(bool force)
        {
            if (_bitBPos > 63)
            {
                ReadBuffer();
                return;
            }

            long pos = BaseStream.Position;
            int posOffset = GetStreamOffset();
            if (pos != _streamPos + posOffset)
            {
                ReadBuffer();
                return;
            }

            if (!force) { return; }
            ByteAlign();
            ReadBuffer();
        }

        private void SeekBaseStream(long prevPos)
        {
            long vPos = _streamPos + GetStreamOffset();
            if (vPos != prevPos)
            {
                BaseStream.Seek(vPos >= _streamLen ? _streamLen : vPos, SeekOrigin.Begin);
            }
        }

        private int GetStreamOffset() => (_bitBPos >> 3);
        private int GetNextBitPos() => ((_bitBPos >> 3) + 1) << 3;

        private byte[] _byteBuffer = new byte[8];
        private void ReadBuffer()
        {
            _bitBPos = 0;
            _streamPos = BaseStream.Position;
            if (_isMemoryStream)
            {
                int count = BaseStream.Read(_byteBuffer, 0, 8);
                BaseStream.Seek(_streamPos, SeekOrigin.Begin);
                _bitBuffer = 0;
                for (int i = 0; i < count; i++)
                {
                    _bitBuffer += (ulong)_byteBuffer[i] << (i * 8);
                }
            }
            else
            {
                FillFileBuffer();
                BaseStream.Seek(_streamPos, SeekOrigin.Begin);

                _bitBuffer = 0;
                for (int i = _fileBufIndex; i < _fileBufIndex + 8; i++)
                {
                    _bitBuffer += (ulong)_fileBuffer[i] << (i * 8);
                }
                _fileBufIndex += 8;
            }
        }

        private void FillFileBuffer()
        {
            if(_fileBufIndex > -1 && _fileBufIndex >= FILE_BUFFER_SIZE) { return; }
            BaseStream.Read(_fileBuffer, 0, FILE_BUFFER_SIZE);
            _fileBufIndex = 0;
        }
    }
}