using System;
using System.IO;

namespace Joonaxii.IO
{
    public class BitWriter : BinaryWriter
    {
        public byte BitPosition { get; private set; } = 0;
        private bool[] _curBits = new bool[8]; 

        public BitWriter() : base() { }
        public BitWriter(Stream stream) : base(stream) { }

        public override void Flush()
        {
            FlushBits();
            base.Flush();
        }

        public override void Write(byte[] buffer, int index, int count)
        {
            for (int i = index; i < index + count; i++)
            {
                Write(buffer[i]);
            }
        }

        public override void Write(byte value)
        {
            for (byte i = 0; i < 8; i++)
            {
                Write(value.IsBitSet(i));
            }
        }

        public override void Write(bool value)
        {
            _curBits[BitPosition] = value;
            BitPosition++;

            if(BitPosition >= 8)
            {
                FlushBits();
            }
        }

        public override void Write(char[] chars, int index, int count)
        {
            for (int i = index; i < index + count; i++)
            {
                Write(chars[i]);
            }
        }

        public override void Write(float value) => Write(BitConverter.GetBytes(value));
        public override void Write(ulong value) => Write(BitConverter.GetBytes(value));
        public override void Write(long value) => Write(BitConverter.GetBytes(value));
        public override void Write(uint value) => Write(BitConverter.GetBytes(value));
        public override void Write(int value) => Write(BitConverter.GetBytes(value));
        public override void Write(ushort value) => Write(BitConverter.GetBytes(value));
        public override void Write(short value) => Write(BitConverter.GetBytes(value));
        public override void Write(double value) => Write(BitConverter.GetBytes(value));
        public override void Write(char[] value) => Write(value, 0, value.Length);
        public override void Write(char value) => Write(BitConverter.GetBytes(value));
        public override void Write(byte[] buffer) => Write(buffer, 0, buffer.Length);

        public override void Write(string value)
        {
            Write(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        public override void Write(decimal value)
        {
            int[] bits = decimal.GetBits(value);
            for (int i = 0; i < bits.Length; i++)
            {
                Write(bits[i]);
            } 
        }

        public byte ToByte()
        {
            byte byt = 0;
            for (int i = 0; i < 8; i++)
            {
                byt += (byte)((_curBits[i] ? 1 : 0) << i);
            }
            return byt;
        }

        private void FlushBits()
        {
            if(BitPosition == 0) { return; }
            base.Write(ToByte());
            BitPosition = 0;
            _curBits = new bool[8];
        }
    }
}
