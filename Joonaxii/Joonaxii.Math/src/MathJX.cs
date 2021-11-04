namespace Joonaxii.MathJX
{
    public static class MathJX
    {
        public static bool IsBitSet(this ulong val, int bit) => (val & (1ul << bit)) != 0;
        public static ulong SetBit(this ulong input, int bitIndex, bool value)
        {
            if (value) { return input |= (1ul << bitIndex); }
            return input &= ~(1ul << bitIndex);
        }

        public static bool IsBitSet(this ulong val, char bit) => (val & (1ul << bit)) != 0;
        public static ulong SetBit(this ulong input, char bitIndex, bool value)
        {
            if (value) { return input |= (1ul << bitIndex); }
            return input &= ~(1ul << bitIndex);
        }

        public static bool IsBitSet(this ulong val, byte bit) => (val & (1ul << bit)) != 0;
        public static ulong SetBit(this ulong input, byte bitIndex, bool value)
        {
            if (value) { return input |= (1ul << bitIndex); }
            return input &= ~(1ul << bitIndex);
        }

        public static bool IsBitSet(this ushort val, byte bit) => (val & (1 << bit)) != 0;
        public static ushort SetBit(this ushort input, byte bitIndex, bool value)
        {
            if (value) { return input |= (ushort)(1 << bitIndex); }
            return input &= (ushort)~(1 << bitIndex);
        }

        public static bool IsBitSet(this int val, int bit) => (val & (1 << bit)) != 0;
        public static int SetBit(this int input, int bitIndex, bool value)
        {
            if (value) { return input |= (1 << bitIndex); }
            return input &= ~(1 << bitIndex);
        }

        public static bool IsBitSet(this byte val, int bit) => (val & (1 << bit)) != 0;
        public static byte SetBit(this byte input, int bitIndex, bool value)
        {
            if (value) { return input |= (byte)(1 << bitIndex); }
            return input &= (byte)~(1 << bitIndex);
        }
    }
}
