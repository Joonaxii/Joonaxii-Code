namespace Joonaxii.Data
{
    public struct MagicByte
    {
        public static MagicByte any { get; } = new MagicByte() { _canBeAny = true };

        private byte _value;
        private bool _canBeAny;

        public MagicByte(byte b)
        {
            _value = b;
            _canBeAny = false;
        }

        public static implicit operator MagicByte(byte b) => new MagicByte(b);
        public static implicit operator MagicByte(char c) => c > 0xFF ? any : new MagicByte((byte)c);
        public static implicit operator MagicByte(int i) => i > 0xFF ? any : new MagicByte((byte)i);

        public bool IsValid(byte b) => _canBeAny | _value == b;
    }
}