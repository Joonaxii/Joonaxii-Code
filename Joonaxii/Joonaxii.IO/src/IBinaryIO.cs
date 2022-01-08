using System.IO;

namespace Joonaxii.IO
{
    public interface IBinaryIO
    {
        void Read(BinaryReader br);
        void Write(BinaryWriter bw);
    }

    public interface IBinaryIO<T>
    {
        T Read(BinaryReader br);
        T Write(BinaryWriter bw);
    }
}
