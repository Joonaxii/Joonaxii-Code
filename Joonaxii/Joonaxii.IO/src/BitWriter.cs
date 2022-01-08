using Joonaxii.MathJX;
using System;
using System.IO;
using System.Text;

namespace Joonaxii.IO
{
    public class BitWriter : BinaryWriter
    {
        public override Stream BaseStream
        {
            get
            {
                ForceBitFlush();
                OutStream.Flush();
                return OutStream;
            }
        
        }

        private ulong _buffer;
        private int _bufferPos;

        private Encoding _encoding;
        private Encoder _encoder;

        private const int LARGE_CHAR_BUF_SIZE = 256;
        private byte[] _charBuffer;
        private int _maxChars;

        private long _prevPos;
        private long _streamPos;

        public BitWriter(Stream input) : this(input, Encoding.UTF8, false) { }
        public BitWriter(Stream input, bool leaveOpen) : this(input, Encoding.UTF8, leaveOpen) { }
        public BitWriter(Stream input, Encoding encoding) : this(input, encoding, false) { }

        public BitWriter(Stream input, Encoding encoding, bool leaveOpen) : base(input, Encoding.UTF8, leaveOpen)
        {
            _prevPos = 0;
            _bufferPos = 0;
            _buffer = 0;
            _streamPos = 0;

            _encoding = encoding;
            _encoder = encoding?.GetEncoder();
        }

        public override void Write(bool value)
        {
            ValidateStreamPosition();
            if (value)
            {
                _buffer |= (1UL << _bufferPos);
            }
            _bufferPos++;
            FlushIfNeeded();
        }

        public override void Write(byte[] buffer) => Write(buffer, 0, buffer.Length);
        public override void Write(byte[] buffer, int index, int count)
        {
            for (int i = index; i < index + count; i++)
            {
                Write(buffer[i], 8);
            }
        }

        public override void Write(char[] buffer) => Write(buffer, 0, buffer.Length);
        public override void Write(char[] buffer, int index, int count)
        {
            for (int i = index; i < index + count; i++)
            {
                Write(buffer[i]);
            }
        }

        public override void Write(byte value) => WriteInternal(value, 8);
        public override void Write(sbyte ch) => WriteInternal(ch, 8);

        public override void Write(char ch) => WriteInternal(ch, 16);

        public override void Write(ushort ch) => WriteInternal(ch, 16);
        public override void Write(short ch) => WriteInternal(ch, 16);

        public override void Write(uint ch) => WriteInternal(ch, 32);
        public override void Write(int ch) => WriteInternal(ch, 32);

        public override void Write(ulong ch) => WriteInternal(ch, 64);
        public override void Write(long ch) => WriteInternal(ch, 64);

        private void WriteInternal(long value, int bits) => WriteInternal((ulong)value, bits);
        private void WriteInternal(ulong value, int bits)
        {
            if(bits < 1) { return; }
            ValidateStreamPosition();
            bits = bits > 64 ? 64 : bits;

            if (_bufferPos == 0)
            {
                _buffer = bits > 63 ? value : value & ((1UL << bits) - 1UL);
                _bufferPos = bits;
                FlushIfNeeded();
                return;
            }

            ulong bitVal;
            int bitsRem = (64 - _bufferPos);
            if (bitsRem >= bits)
            {
                bitVal = value & ((1UL << bits) - 1UL);
                _buffer |= bitVal << _bufferPos;
                _bufferPos += bits;
                FlushIfNeeded();
                return;
            }

            int hi = bits - bitsRem;
            int lo = bits - hi;
            ulong maskHI = ((1UL << hi) - 1UL);

            bitVal = value & ((1UL << lo) - 1UL);

            _buffer += bitVal << _bufferPos;
            ForceLongFlush();

            bitVal = value & (maskHI << lo);
            _buffer = (bitVal >> bitsRem) & maskHI;
            _bufferPos = hi;
        }

        public void Write(sbyte ch, int bits) => WriteInternal(ch, bits);

        public void Write(char ch, int bits) => WriteInternal(ch, bits);

        public void Write(ushort ch, int bits) => WriteInternal(ch, bits);
        public void Write(short ch, int bits) => WriteInternal(ch, bits);

        public void Write(uint ch, int bits) => WriteInternal(ch, bits);
        public void Write(int ch, int bits) => WriteInternal(ch, bits);

        public void Write(ulong ch, int bits) => WriteInternal(ch, bits);
        public void Write(long ch, int bits) => WriteInternal(ch, bits);

        public override long Seek(int offset, SeekOrigin origin)
        {
            FlushIfNeeded(true);
            return base.Seek(offset, origin);
        }

        public override unsafe void Write(float value)
        {
            uint tmpVal = *(uint*)&value;
            Write(tmpVal, 32);
        }

        public override unsafe void Write(double value)
        {
            ulong tmpVal = *(ulong*)&value;
            Write(tmpVal, 64);
        }

        public override void Write(decimal value)
        {
            byte[] bits = new byte[16];
            Buffer.BlockCopy(decimal.GetBits(value), 0, bits, 0, 16);
            Write(bits);
        }

        public override unsafe void Write(string value)
        {
            if (value == null) { throw new ArgumentNullException(nameof(value)); }
            int len;

            if (_encoding == null)
            {
                len = value.Length;
                Write7BitInt(len);
                for (int i = 0; i < len; i++)
                {
                    Write(value[i]);
                }
                return;
            }

            len = _encoding.GetByteCount(value);
            Write7BitInt(len);

            if (_charBuffer == null)
            {
                _charBuffer = new byte[LARGE_CHAR_BUF_SIZE];
                _maxChars = _charBuffer.Length / _encoding.GetMaxByteCount(1);
            }

            if (len <= _charBuffer.Length)
            {
                _encoding.GetBytes(value, 0, value.Length, _charBuffer, 0);
                Write(_charBuffer, 0, len);
                return;
            }

            int charSt = 0;
            int numLeft = value.Length;

            while (numLeft > 0)
            {
                int charC = (numLeft > _maxChars) ? _maxChars : numLeft;
                int byteL;

                checked
                {
                    if (charSt < 0 | charC < 0 | charSt + charC > value.Length) { throw new ArgumentOutOfRangeException("charC"); }

                    fixed (char* pChars = value)
                    fixed (byte* pBytes = _charBuffer)
                    {
                        byteL = _encoder.GetBytes(pChars + charSt, charC, pBytes, _charBuffer.Length, charC == numLeft);
                    }
                }

                Write(_charBuffer, 0, byteL);
                charSt += charC;
                numLeft -= charC;
            }
        }

        public void Write7BitInt(int value) => Write7BitInt((uint)value);
        public void Write7BitInt(uint value)
        {
            while (value >= 0x80)
            {
                Write((value | 0x80), 8);
                value >>= 7;
            }
            Write(value, 8);
        }

        protected override void Dispose(bool disposing)
        {
            FlushIfNeeded(true, true);
            _charBuffer = null;
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            FlushIfNeeded(true, true);
            base.Flush();
        }

        public override void Close()
        {
            FlushIfNeeded(true, true);
            base.Close();
        }

        public void FlushBitBuffer() => FlushIfNeeded(true);

        private void FlushIfNeeded(bool force = false, bool skipValidation = false)
        {
            if (!skipValidation && ValidateStreamPosition()) { return; }
            if (_bufferPos >= 64)
            {
                ForceLongFlush();
                return;
            }

            if ((force & _bufferPos > 0))
            {
                ForceBitFlush();
            }
        }

        public void ByteAlign()
        {
            if (_bufferPos % 8 != 0)
            {
                _bufferPos = ((_bufferPos >> 3) + 1) << 3;
                if (_bufferPos >= 64) { ForceLongFlush(); }
            }
        }
        private void ForceBitFlush()
        {
            ByteAlign();
            switch (_bufferPos)
            {
                case 0: break;
                default: ForceByteFlush(); break;
                case 16: ForceShortFlush(); break;
                case 24: ForceSubIntFlush(3); break;
                case 32: ForceIntFlush(); break;
                case 40: ForceSubIntFlush(5); break;
                case 48: ForceSubIntFlush(6); break;
                case 56: ForceSubIntFlush(7); break;
                case 64: ForceLongFlush(); break;
            }
        }
        private bool ValidateStreamPosition()
        {
            if (_prevPos != OutStream.Position)
            {
                long pos = OutStream.Position;
                OutStream.Seek(_streamPos, SeekOrigin.Begin);
                ForceBitFlush();

                OutStream.Seek(pos, SeekOrigin.Begin);
                _streamPos = pos;
                _prevPos = pos;
                return true;
            }
            return false;
        }

        private void UpdateVirtualPosition()
        {
            long pos = _streamPos + (_bufferPos % 8 != 0 ? ((_bufferPos >> 3) + 1) : _bufferPos >> 8);

            if(_prevPos == pos) { return; }
            _prevPos = pos;
            OutStream.Seek(pos, SeekOrigin.Begin);
        }

        private void ForceByteFlush()
        {
            base.Write((byte)(_buffer & 0xFF));
            _bufferPos = 0;
            _buffer = 0;

            _streamPos++;
            _prevPos = _streamPos;
        }
        private void ForceShortFlush()
        {
            base.Write((ushort)(_buffer & 0xFFFF));
            _bufferPos = 0;
            _buffer = 0;

            _streamPos += 2;
            _prevPos = _streamPos;
        }

        private void ForceIntFlush()
        {
            base.Write((uint)(_buffer & 0xFFFFFFFF));
            _bufferPos = 0;
            _buffer = 0;

            _streamPos += 4;
            _prevPos = _streamPos;
        }

        private void ForceLongFlush()
        {
            base.Write(_buffer);
            _bufferPos = 0;
            _buffer = 0;

            _streamPos += 8;
            _prevPos = _streamPos;
        }

        private byte[] _intSubByt = new byte[7];
        private void ForceSubIntFlush(int bytes)
        {
            _buffer &= ((1UL << bytes * 8) - 1);
            for (int i = 0; i < bytes; i++)
            {
                _intSubByt[i] = (byte)(_buffer >> (i * 8));
            }
            base.Write(_intSubByt, 0, bytes);
            _bufferPos = 0;
            _buffer = 0;

            _streamPos += bytes;
            _prevPos = _streamPos;
        }
    }
}
