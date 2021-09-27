using System;
using System.IO;

namespace Joonaxii.IO
{
    public class BitWriter : BinaryWriter
    {
        public byte BitPosition { get; private set; } = 0;
        private byte _bufferBits = 0; 

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

        public void Write(int value, int bits)
        {
            for (int i = 0; i < bits; i++)
            {
                Write(value.IsBitSet(i));
            }
        }

        public override void Write(bool value)
        {
            _bufferBits = _bufferBits.SetBit(BitPosition++, value);
            if (BitPosition >= 8)
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
        public override void Write(double value) => Write(BitConverter.GetBytes(value));

        public override void Write(ulong value) => Write(BitConverter.GetBytes(value));
        public override void Write(long value) => Write(BitConverter.GetBytes(value));

        public override void Write(uint value) => Write((int)value, 32);
        public override void Write(int value) => Write(value, 32);
        public override void Write(ushort value) => Write(value, 16);
        public override void Write(short value) => Write(value, 16);
        public override void Write(char[] value) => Write(value, 0, value.Length);
        public override void Write(char value) => Write(value, 16);
        public override void Write(byte[] buffer) => Write(buffer, 0, buffer.Length);
        public override void Write(byte value) => Write(value, 8);

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

        private void FlushBits()
        {
            if(BitPosition == 0) { return; }
            base.Write(_bufferBits);
            BitPosition = 0;
            _bufferBits = 0;
        }
    }
}
