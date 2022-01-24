using System.IO;
using System.IO.Compression;
using System.Text;

namespace Joonaxii.Data.Image.Conversion.PNG
{
    public class ICCPChunk : PNGChunk
    {
        public string profileName;
        public PNGCompressionMethod compressionMethod;

        public byte[] profileBytes;

        public ICCPChunk(int len, byte[] data, uint crc) : base(len, PNGChunkType.iCCP, data, crc)
        {
            using(var ms = GetStream())
            using(BinaryReader br = new BinaryReader(ms))
            {
                profileName = "";

                byte b = br.ReadByte();
                while(b != 0)
                {
                    profileName += (char)b;
                    b = br.ReadByte();
                }

                compressionMethod = (PNGCompressionMethod)br.ReadByte();

                ms.Seek(2, SeekOrigin.Current);
                long pos = ms.Position;
                using(DeflateStream defStrm = new DeflateStream(ms, CompressionMode.Decompress))
                using(MemoryStream msDat = new MemoryStream())
                {
                    ms.Seek(pos, SeekOrigin.Begin);
                    defStrm.CopyTo(msDat);
                    profileBytes = msDat.ToArray();
                }
            }
        }

        public override string ToMinString() => $"{base.ToMinString()} [{profileName}, {compressionMethod}, {profileBytes.Length} bytes]";

        public override string GetSpedcificInfoString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"    -Profile Name: {profileName}");
            sb.AppendLine($"    -Compression: {compressionMethod}");
            sb.AppendLine($"    -Profile: {profileBytes.Length} bytes");
        
            sb.AppendLine(new string('-', 16));
            return sb.ToString();
        }
    }
}