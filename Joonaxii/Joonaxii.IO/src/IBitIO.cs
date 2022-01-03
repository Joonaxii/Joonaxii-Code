namespace Joonaxii.IO
{
    public interface IBitIO
    {
        void Read(BitReader br);
        void Write(BitWriter bw);
    }
}
