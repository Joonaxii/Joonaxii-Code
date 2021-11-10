using Joonaxii.Math;
using System;
using System.IO;
using System.Text;

namespace Joonaxii.IO
{
    public class BitReader : BinaryReader
    {
        public byte BitPosition { get; private set; } = 0;
        private byte _bufferBits = 0;

        public BitReader(Stream stream) : base(stream) { BitPosition = 8; }

        public override bool ReadBoolean()
        {
            FlushIfNeeded();
            return _bufferBits.IsBitSet(BitPosition++);
        }

        public override byte ReadByte()
        {
            FlushIfNeeded();

            byte byt = 0;
            for (int i = 0; i < 8; i++)
            {
                byt = byt.SetBit(i, ReadBoolean());
            }
            return byt;
        }

        public byte ReadByte(int bitsPerByte, int bitJump = 1, bool debug = false)
        {
            FlushIfNeeded();

            byte byt = 0;
            byte bytI = 0;
            while (bytI < 8)
            {
                for (int i = 0; i < bitsPerByte; i++)
                {
                    byt = byt.SetBit(bytI++, ReadBoolean());
                    if(bytI >= 8) { break; }
                }

                for (int i = 0; i < bitJump; i++)
                {
                    BitPosition = 8;
                    FlushIfNeeded();
                    BitPosition = 8;
                }
            }

            if (debug)
            {
                System.Diagnostics.Debug.Print($"{_bufferBits} [{Convert.ToString(_bufferBits, 2).PadLeft(8, '0')}]");
            }
            return byt;
        }

        public int ReadValue(int bits)
        {
            int val = 0;
            for (int i = 0; i < bits; i++)
            {
                val = val.SetBit(i, ReadBoolean());
            }
            return val;
        }

        public ulong ReadValue(byte leastBits, int bytes, int bitJump = 1, bool debug = false)
        {
            FlushIfNeeded();

            ulong val = 0;

            for (int i = 0; i < bytes; i++)
            {
                val += ((ulong)ReadByte(leastBits, bitJump) << i * 8);
            }
            return val;
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
                if (i >= buffer.Length) { return buffer.Length; }
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
        public override short ReadInt16() => (short)ReadValue(16);
        public override int ReadInt32() => ReadValue(32);
        public int ReadInt32(byte bits, int bitJump = 1) => (int)ReadValue(bits, 4, bitJump);
        public override long ReadInt64() => BitConverter.ToInt64(ReadBytes(8), 0);
        public ulong ReadUInt64(byte bits, int bitJump = 1) => ReadValue(bits, 8, bitJump);
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
        public override ushort ReadUInt16() => (ushort)ReadValue(16);
        public ushort ReadUInt16(byte bits, int bitJump = 1) => (ushort)ReadValue(bits, 2, bitJump);
        public override uint ReadUInt32() => (uint)ReadValue(32);
        public override ulong ReadUInt64() => BitConverter.ToUInt64(ReadBytes(8), 0);

        private void FlushIfNeeded()
        {
            if (BitPosition >= 8)
            {
                _bufferBits = base.ReadByte();
                BitPosition = 0;
            }
        }
    }
}