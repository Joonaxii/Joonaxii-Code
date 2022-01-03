using Joonaxii.MathJX;
using System;
using System.IO;
using System.Text;

namespace Joonaxii.IO
{
    public class BitWriter : BinaryWriter
    {
        private byte _buffer;
        private byte _bufferPos;

        private Encoding _encoding;
        private Encoder _encoder;

        private const int LARGE_CHAR_BUF_SIZE = 256;
        private byte[] _charBuffer;
        private int _maxChars;

        public BitWriter(Stream input) : this(input, Encoding.UTF8)
        {
            _bufferPos = 0;
            _buffer = 0;
        }

        public BitWriter(Stream input, Encoding encoding) : base(input)
        {
            _bufferPos = 0;
            _buffer = 0;

            _encoding = encoding;
            _encoder = encoding?.GetEncoder();
        }

        public override void Write(bool value)
        {
            _buffer = _buffer.SetBit(_bufferPos++, value);
            FlushIfNeeded();
        }

        public override void Write(byte[] buffer) => Write(buffer, 0, buffer.Length);
        public override void Write(byte[] buffer, int index, int count)
        {
            for (int i = index; i < index + count; i++)
            {
                Write(buffer[i]);
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

        public override void Write(byte value) => Write(value, 8);
        public override void Write(sbyte ch) => Write(ch, 8);

        public override void Write(char ch) => Write(ch, 16);

        public override void Write(ushort ch) => Write(ch, 16);
        public override void Write(short ch) => Write(ch, 16);

        public override void Write(uint ch) => Write(ch, 32);
        public override void Write(int ch) => Write(ch, 32);

        public override void Write(ulong ch) => Write(ch, 64);
        public override void Write(long ch) => Write(ch, 64);

        public void Write(byte value, byte bits) => Write(value, 0, bits);
        public void Write(byte value, byte start, byte bits)
        {
            for (int i = start; i < start + bits; i++)
            {
                Write(value.IsBitSet(i));
            }
        }

        public void Write(sbyte value, byte bits) => Write(value, 0, bits);
        public void Write(sbyte value, byte start, byte bits)
        {
            for (byte i = start; i < start + bits; i++)
            {
                Write(value.IsBitSet(i));
            }
        }

        public void Write(short value, byte bits) => Write(value, 0, bits);
        public void Write(short value, byte start, byte bits)
        {
            for (byte i = start; i < start + bits; i++)
            {
                Write(value.IsBitSet(i));
            }
        }

        public void Write(char value, byte bits) => Write(value, 0, bits);
        public void Write(char value, byte start, byte bits)
        {
            for (byte i = start; i < start + bits; i++)
            {
                Write(value.IsBitSet(i));
            }
        }

        public void Write(ushort value, byte bits) => Write(value, 0, bits);
        public void Write(ushort value, byte start, byte bits)
        {
            for (byte i = start; i < start + bits; i++)
            {
                Write(value.IsBitSet(i));
            }
        }

        public void Write(int value, byte bits) => Write(value, 0, bits);
        public void Write(int value, byte start, byte bits)
        {
            for (int i = start; i < start + bits; i++)
            {
                Write(value.IsBitSet(i));
            }
        }

        public void Write(uint value, byte bits) => Write(value, 0, bits);
        public void Write(uint value, byte start, byte bits)
        {
            for (byte i = start; i < start + bits; i++)
            {
                Write(value.IsBitSet(i));
            }
        }

        public void Write(long value, byte bits) => Write(value, 0, bits);
        public void Write(long value, byte start, byte bits)
        {
            for (int i = start; i < start + bits; i++)
            {
                Write(value.IsBitSet(i));
            }
        }

        public void Write(ulong value, byte bits) => Write(value, 0, bits);
        public void Write(ulong value, byte start, byte bits)
        {
            for (byte i = start; i < start + bits; i++)
            {
                Write(value.IsBitSet(i));
            }
        }

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
            if(value == null) { throw new ArgumentNullException(nameof(value)); }
            int len;

            if(_encoding == null)
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

            if(_charBuffer == null)
            {
                _charBuffer = new byte[LARGE_CHAR_BUF_SIZE];
                _maxChars = _charBuffer.Length / _encoding.GetMaxByteCount(1);
            }

            if(len <= _charBuffer.Length)
            {
                _encoding.GetBytes(value, 0, value.Length, _charBuffer, 0);
                Write(_charBuffer, 0, len);
                return;
            }

            int charSt = 0;
            int numLeft = value.Length;

            while(numLeft > 0)
            {
                int charC = (numLeft > _maxChars) ? _maxChars : numLeft;
                int byteL;

                checked
                {
                    if(charSt < 0 | charC < 0 | charSt + charC > value.Length) { throw new ArgumentOutOfRangeException("charC"); }

                    fixed(char* pChars = value)
                    fixed(byte* pBytes = _charBuffer)
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
            FlushIfNeeded(true);
            _charBuffer = null;

            base.Dispose(disposing);
        }

        public override void Flush()
        {
            FlushIfNeeded(true);
            base.Flush();
        }

        public override void Close()
        {
            FlushIfNeeded(true);
            base.Close();
        }

        public void FlushBitBuffer() => FlushIfNeeded(true);

        private void FlushIfNeeded(bool force = false)
        {
            if (_bufferPos >= 8 | (force & _bufferPos > 0))
            {
                base.Write(_buffer);
                _bufferPos = 0;
                _buffer = 0;
            }
        }
    }
}
