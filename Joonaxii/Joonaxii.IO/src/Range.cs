namespace Joonaxii.MathX
{
    public struct RangeInt
    {
        public int Length { get; private set; }
        public int start;
        public int end;

        public RangeInt(int start)
        {
            this.start = start;
            end = start + 1;

            Length = end - start;
        }

        public RangeInt(int start, int end, bool exlusiveEnd = false)
        {
            this.start = start;
            this.end = exlusiveEnd ? end : end + 1;

            Length = end - start;
        }

        public static implicit operator RangeInt(int i) => new RangeInt(i);
        public override string ToString() => $"Start: {start}, End: {end}";
    }

    public struct RangeUShort
    {
        public int Length { get; private set; }
        public ushort start;
        public ushort end;

        public RangeUShort(ushort start)
        {
            this.start = start;
            end = (ushort)(start + 1);

            Length = end - start;
        }

        public RangeUShort(ushort start, ushort end, bool exlusiveEnd = false)
        {
            this.start = start;
            this.end = (ushort)(exlusiveEnd ? end : end + 1);

            Length = end - start;
        }

        public static implicit operator RangeUShort(ushort i) => new RangeUShort(i);
    }

    public struct RangeChar
    {
        public int Length { get; private set; }
        public char start;
        public char end;

        public RangeChar(ushort start)
        {
            this.start = (char)start;
            end = (char)(start + 1);

            Length = end - start;
        }

        public RangeChar(char start)
        {
            this.start = start;
            end = (char)(start + 1);

            Length = end - start;
        }

        public RangeChar(ushort start, ushort end, bool exlusiveEnd = false)
        {
            this.start = (char)start;
            this.end = (char)(exlusiveEnd ? end : end + 1);

            Length = end - start;
        }

        public RangeChar(char start, char end, bool exlusiveEnd = false)
        {
            this.start = start;
            this.end = (char)(exlusiveEnd ? end : end + 1);

            Length = end - start;
        }

        public static implicit operator RangeChar(char i) => new RangeChar(i);
    }
}