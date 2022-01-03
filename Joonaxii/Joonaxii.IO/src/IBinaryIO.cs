using System.IO;

namespace Joonaxii.IO
{
    public interface IBinaryIO
    {
        void Read(BinaryReader br);
        void Write(BinaryWriter bw);
    }
}
