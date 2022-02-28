using System.IO;
using System.IO.Compression;
using System.Text;

namespace Joonaxii.Image.Codecs.PNG
{
    public class ICCPChunk : PNGChunk
    {
        public string profileName;
        public PNGCompressionMethod compressionMethod;

        public byte[] profileBytes;

        public ICCPChunk(BinaryReader br, int len, uint crc) : base(len, PNGChunkType.iCCP, crc, 0)
        {
            profileName = "";

            byte b = br.ReadByte();
            while (b != 0)
            {
                profileName += (char)b;
                b = br.ReadByte();
            }
            compressionMethod = (PNGCompressionMethod)br.ReadByte();

            byte[] data = new byte[len];
            br.Read(data, 0, len);

            using(MemoryStream ms = new MemoryStream(data))
            using (DeflateStream defStrm = new DeflateStream(ms, CompressionMode.Decompress))
            using (MemoryStream msDat = new MemoryStream())
            {
                ms.Seek(2, SeekOrigin.Begin);
                defStrm.CopyTo(msDat);
                profileBytes = msDat.ToArray();
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