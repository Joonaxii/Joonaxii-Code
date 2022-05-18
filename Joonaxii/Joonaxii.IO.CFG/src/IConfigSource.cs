namespace Joonaxii.IO.CFG
{
    public interface IConfigSource
    {
        void Read(ConfigStream cfg);
        void Write(ConfigStream cfg);
    }
}
