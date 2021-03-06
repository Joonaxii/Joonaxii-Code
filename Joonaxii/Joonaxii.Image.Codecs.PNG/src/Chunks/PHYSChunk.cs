using Joonaxii.IO;
using System.IO;
using System.Text;

namespace Joonaxii.Image.Codecs.PNG
{
    public class PHYSChunk : PNGChunk
    {
        public int pixelsPerUnitX;
        public int pixelsPerUnitY;
        public UnitSpecifier unitSpecifier;

        public PHYSChunk(BinaryReader br, int len, uint crc) : base(len, PNGChunkType.pHYs, crc, 0)
        {
            pixelsPerUnitX = br.ReadInt32BigEndian();
            pixelsPerUnitY = br.ReadInt32BigEndian();
            unitSpecifier = (UnitSpecifier)br.ReadByte();
        }

        public override string ToMinString() => $"{base.ToMinString()} [{pixelsPerUnitX} uX, {pixelsPerUnitY} uY, {unitSpecifier}]";
        public override string GetSpedcificInfoString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"    -Pixels Per Unit X: {pixelsPerUnitX}");
            sb.AppendLine($"    -Pixels Per Unit Y: {pixelsPerUnitY}");
            sb.AppendLine($"    -Unit Specifier: {unitSpecifier}");
     
            sb.AppendLine(new string('-', 16));
            return sb.ToString();
        }
    }
}