namespace Joonaxii.IO
{
    public interface IBitIO
    {
        void Read(BitReader br);
        void Write(BitWriter bw);
    }

    public interface IBitIO<T>
    {
        T Read(BitReader br);
        T Write(BitWriter bw);
    }
}
