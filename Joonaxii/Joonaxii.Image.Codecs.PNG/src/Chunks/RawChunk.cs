using System;
using System.Text;

namespace Joonaxii.Image.Codecs.PNG
{
    public class RawChunk : PNGChunk
    {
        public RawChunk(int len, PNGChunkType type, byte[] data, uint crc) : base(len, type, data, crc) { }

        public override string GetSpedcificInfoString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sb.AppendLine($"    -0x{Convert.ToString(data[i], 16).PadLeft(2, '0')}");
            }
            sb.AppendLine(new string('-', 16));
            return sb.ToString();
        }
    }
}