namespace Joonaxii.MathX
{
    public struct Range
    {
        public int start;
        public int end;

        public Range(int start)
        {
            this.start = start;
            end = start + 1;
        }

        public Range(int start, int end, bool exlusiveEnd = false)
        {
            this.start = start;
            this.end = exlusiveEnd ? end : end + 1;
        }

        public static implicit operator Range(int i) => new Range(i);
    }
}