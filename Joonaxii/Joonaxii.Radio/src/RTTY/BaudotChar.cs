using System.Runtime.InteropServices;

namespace Joonaxii.Radio.RTTY
{
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    public struct BaudotChar
    {
        public bool IsSymbol { get => (_value & 0x80) != 0; }
        public bool IsControl { get => Baudot.IsControl(_value); }
        public byte GetValue { get => (byte)(_value & 0x1F); }

        public char GetChar { get => Baudot.ToChar(_value); }

        private byte _value;

        public BaudotChar(byte value) => _value = value;
        public BaudotChar(char c) => _value = Baudot.ToBaudot(c);
    }
}
