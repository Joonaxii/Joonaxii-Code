using Joonaxii.IO;
using System.IO;
using System.Text;

namespace Joonaxii.Image.Codecs.PNG
{
    public class SPLTChunk : PNGChunk
    {
        public string paletteName;
        public byte sampleDepth;

        public FastColor[] pixels;

        public SPLTChunk(BinaryReader br, int len, uint crc) : base(len, PNGChunkType.sPLT, crc, 0)
        {
            pixels = new FastColor[0];

            paletteName = "";
            byte v = br.ReadByte();
            len--;
            while (v != 0)
            {
                paletteName += (char)v;
                v = br.ReadByte();
                len--;
            }
            sampleDepth = br.ReadByte();
            len--;

            int pixelCount = (len / (sampleDepth == 8 ? 6 : 10));

            pixels = new FastColor[pixelCount];
            switch (sampleDepth)
            {
                case 8:
                    for (int i = 0; i < pixelCount; i++)
                    {
                        pixels[i] = new FastColor(br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte());
                        br.ReadUInt16BigEndian();
                    }
                    break;
                case 16:
                    const float SHORT_TO_BYTE = (1.0f / ushort.MaxValue) * 255.0f;

                    for (int i = 0; i < pixelCount; i++)
                    {
                        byte r = (byte)(br.ReadUInt16BigEndian() * SHORT_TO_BYTE);
                        byte g = (byte)(br.ReadUInt16BigEndian() * SHORT_TO_BYTE);
                        byte b = (byte)(br.ReadUInt16BigEndian() * SHORT_TO_BYTE);
                        byte a = (byte)(br.ReadUInt16BigEndian() * SHORT_TO_BYTE);

                        pixels[i] = new FastColor(r, g, b, a);
                        br.ReadUInt16BigEndian();
                    }
                    break;
            }
        }

        public override string ToMinString() => $"{base.ToMinString()} [{paletteName}, {sampleDepth}, {pixels.Length} colors]";

        public override string GetSpedcificInfoString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"    -Palette Name: {paletteName}");
            sb.AppendLine($"    -Sample Depth: {sampleDepth}");
            sb.AppendLine($"    -Colors: {pixels.Length}");

            sb.AppendLine(new string('-', 16));
            return sb.ToString();
        }
    }
}