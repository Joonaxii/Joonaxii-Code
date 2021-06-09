using System;
using System.IO;
using System.Text;

namespace Joonaxii.IO
{
    public class BitReader : BinaryReader
    {
        public byte BitPosition { get; private set; } = 0;
        private bool[] _curBits = new bool[8];

        public BitReader(Stream stream) : base(stream) { }

        public override bool ReadBoolean()
        {
            FlushIfNeeded();

            bool b = _curBits[BitPosition];
            BitPosition++;
            return b;
        }

        public override byte ReadByte()
        {
            FlushIfNeeded();

            byte byt = 0;
            for (int i = 0; i < 8; i++)
            {
                byt += (byte)((ReadBoolean() ? 1 : 0) << i);
            }
            return byt;
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
            for (int i = index; i < index + count; i++)
            {
                if(i >= buffer.Length) { return buffer.Length; }
                buffer[i] = ReadByte();
            }       
            return count;
        }

        public override int Read(char[] chars, int index, int count)
        {
            for (int i = index; i < index + count; i++)
            {
                if (i >= chars.Length) { return chars.Length; }
                chars[i] = ReadChar();
            }
            return count;
        }

        public override char ReadChar() => BitConverter.ToChar(ReadBytes(2), 0);
        public override char[] ReadChars(int count)
        {
            var chars = new char[count];
            Read(chars, 0, count);
            return chars;
        }

        public override decimal ReadDecimal()
        {
            int[] ints = new int[4];
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = ReadInt32();
            }
            return new decimal(ints);
        }
        public override double ReadDouble() => BitConverter.ToDouble(ReadBytes(8), 0);
        public override short ReadInt16() => BitConverter.ToInt16(ReadBytes(2), 0);
        public override int ReadInt32() => BitConverter.ToInt32(ReadBytes(4), 0);
        public override long ReadInt64() => BitConverter.ToInt64(ReadBytes(8), 0);
        public override sbyte ReadSByte() => (sbyte)ReadByte();
        public override float ReadSingle() => BitConverter.ToSingle(ReadBytes(4), 0);
        public override string ReadString()
        {
            int l = ReadInt32();
            StringBuilder sb = new StringBuilder(l);
            for (int i = 0; i < l; i++)
            {
                sb.Append(ReadChar());
            }
            return sb.ToString();
        }
        public override ushort ReadUInt16() => BitConverter.ToUInt16(ReadBytes(2), 0);
        public override uint ReadUInt32() => BitConverter.ToUInt32(ReadBytes(4), 0);
        public override ulong ReadUInt64() => BitConverter.ToUInt64(ReadBytes(8), 0);

        private void FlushIfNeeded()
        {
            if (BitPosition >= 8)
            {
                _curBits = new bool[8];
                BitPosition = 0;
            }
        }
    }
}