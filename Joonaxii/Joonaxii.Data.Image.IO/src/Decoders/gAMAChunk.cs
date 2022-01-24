using System.Text;

namespace Joonaxii.Data.Image.Conversion.PNG
{
    public class gAMAChunk : PNGChunk
    {
        public float gamma;

        public gAMAChunk(int len, byte[] data, uint crc) : base(len, PNGChunkType.gAMA, data, crc)
        {
            gamma = ((uint)(data[3] + (data[2] << 8) + (data[1] << 16) + (data[0] << 24))) / 100000.0f;
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