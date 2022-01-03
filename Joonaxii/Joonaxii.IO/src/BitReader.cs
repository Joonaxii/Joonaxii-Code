using Joonaxii.MathJX;
using System;
using System.IO;
using System.Text;

namespace Joonaxii.IO
{
    public class BitReader : BinaryReader
    {
        public bool IsEoF { get => BaseStream.Position >= BaseStream.Length; }

        private byte _buffer;
        private byte _bufferPos;

        private Encoding _encoding;
        private Decoder _decoder;

        private const int MAX_CHAR_BYTES = 128;
        private byte[] _charBytes;
        private char[] _singleChar;
        private char[] _charBuffer;
        private int _maxCharsSize;

        private bool _2BytesPerChar;
        private bool _isMemoryStream;

        public BitReader(Stream input) : this(input, Encoding.UTF8)
        {
        }

        public BitReader(Stream input, Encoding encoding) : base(input)
        {
            _bufferPos = 8;
            _encoding = encoding;
            _decoder = encoding?.GetDecoder();

            _maxCharsSize = encoding != null ? encoding.GetMaxCharCount(MAX_CHAR_BYTES) : MAX_CHAR_BYTES / 2;

            _2BytesPerChar = encoding is UnicodeEncoding;
            _isMemoryStream = input.GetType() == typeof(MemoryStream);
        }

        public override bool ReadBoolean()
        {
            ReadBuffer();
            return _buffer.IsBitSet(_bufferPos++);
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

            if(len <= 0) { return string.Empty; }

            if(_charBytes == null)
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

            while(currPos < len)
            {
                rLen = ((len - currPos) > MAX_CHAR_BYTES) ? MAX_CHAR_BYTES : (len - currPos);
                n = Read(_charBytes, 0, rLen);

                if(n == 0) { break; }

                cRead = _decoder.GetChars(_charBytes, 0, n, _charBuffer, 0);
                if(currPos == 0 & n == len) { return new string(_charBuffer, 0, cRead); }

                if(sb == null) { sb = new StringBuilder(len); }
                sb.Append(_charBuffer, 0, cRead);
                currPos += n;
            }
            return sb == null ? string.Empty : sb.ToString();
        }

        public override int Read() => BaseStream.Position >= BaseStream.Length ? -1 : ReadByte();

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

        public byte ReadByte(byte bits)
        {
            byte v = 0;
            for (byte i = 0; i < bits; i++)
            {
                v = v.SetBit(i, ReadBoolean());
            }
            return v;
        }
        public sbyte ReadSByte(byte bits)
        {
            sbyte v = 0;
            for (byte i = 0; i < bits; i++)
            {
                v = v.SetBit(i, ReadBoolean());
            }
            return v;
        }

        public short ReadInt16(byte bits)
        {
            short v = 0;
            for (byte i = 0; i < bits; i++)
            {
                v = v.SetBit(i, ReadBoolean());
            }
            return v;
        }
        public ushort ReadUInt16(byte bits)
        {
            ushort v = 0;
            for (byte i = 0; i < bits; i++)
            {
                v = v.SetBit(i, ReadBoolean());
            }
            return v;
        }

        public int ReadInt32(byte bits)
        {
            int v = 0;
            for (byte i = 0; i < bits; i++)
            {
                v = v.SetBit(i, ReadBoolean());
            }
            return v;
        }
        public uint ReadUInt32(byte bits)
        {
            uint v = 0;
            for (byte i = 0; i < bits; i++)
            {
                v = v.SetBit(i, ReadBoolean());
            }
            return v;
        }

        public long ReadInt64(byte bits)
        {
            long v = 0;
            for (int i = 0; i < bits; i++)
            {
                v = v.SetBit(i, ReadBoolean());
            }
            return v;
        }
        public ulong ReadUInt64(byte bits)
        {
            ulong v = 0;
            for (byte i = 0; i < bits; i++)
            {
                v = v.SetBit(i, ReadBoolean());
            }
            return v;
        }

        public byte[] ReadToEnd()
        {
            if(_bufferPos < 8 & _bufferPos > 0) { DiscardBitBuffer(false); }
            int count = /*_bufferPos >= 8 ? (int)(BaseStream.Length - BaseStream.Position) - 1 : */(int)(BaseStream.Length - BaseStream.Position);
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

        public void DiscardBitBuffer(bool ignoreFirst = false) => ReadBuffer(ignoreFirst || _bufferPos > 0);

        private void ReadBuffer(bool force = false)
        {
            if (_bufferPos >= 8 | force)
            {
                _buffer = IsEoF ? (byte)0 : base.ReadByte();
                _bufferPos = 0;
            }
        }
    }
}