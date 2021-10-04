namespace Joonaxii.Text.Compression
{
    public class TokenRange
    {
        public byte size;
        public byte bits;

        public TokenRange(byte size)
        {
            this.size = size;
            this.bits = 0;
        }

        public void Setup(byte bitsNeeded) => bits = bits < bitsNeeded ? bitsNeeded : bits;
    }
}