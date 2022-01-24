using System;
using System.IO;
using System.Text;

namespace Joonaxii.Data.Image.Conversion.PNG
{
    public class PLTEChunk : PNGChunk
    {
        public FastColor[] pixels;

        public PLTEChunk(int len, byte[] data, uint crc) : base(len, PNGChunkType.PLTE, data, crc)
        {
            pixels = new FastColor[0];

            using (var stream = GetStream())
            using (BinaryReader br = new BinaryReader(stream))
            {
                int palLen = (length / 3);
                pixels = new FastColor[palLen];

                for (int i = 0; i < palLen; i++)
                {
                    pixels[i] = new FastColor(br.ReadByte(), br.ReadByte(), br.ReadByte());
                }
            }
        }

        public void ApplyGamma(byte[] gammaTable)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                var px = pixels[i];
                px.r = gammaTable[px.r];
                px.g = gammaTable[px.g];
                px.b = gammaTable[px.b];
                pixels[i] = px;
            }
        }

        public void ApplyTransparency(byte[] data)
        {
            int min = Math.Min(data.Length, pixels.Length);
            for (int i = 0; i < min; i++)
            {
                pixels[i].Set(data[i]);
            }
        }

        public override string ToMinString() => $"{base.ToMinString()} [{pixels.Length} colors]";

        public override string GetSpedcificInfoString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"    -Colors: {pixels.Length}");

            sb.AppendLine(new string('-', 16));
            return sb.ToString();
        }
    }
}