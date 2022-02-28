using System.IO;
using System.Text;

namespace Joonaxii.Image.Codecs.PNG
{
    public class gAMAChunk : PNGChunk
    {
        public float gamma;

        public gAMAChunk(BinaryReader br, int len, uint crc) : base(len, PNGChunkType.gAMA, crc, 0)
        {
            unsafe
            {
                byte* temp = stackalloc byte[4];
                for (int i = 0; i < 4; i++, temp++)
                {
                    *temp = br.ReadByte();
                }
                gamma = ((uint)(temp[3] + (temp[2] << 8) + (temp[1] << 16) + (temp[0] << 24))) / 100000.0f;
            }
        }

        public override string ToMinString() => $"{base.ToMinString()} [{gamma}]";

        public override string GetSpedcificInfoString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"    -Gamma: {gamma}");
   
            sb.AppendLine(new string('-', 16));
            return sb.ToString();
        }
    }
}