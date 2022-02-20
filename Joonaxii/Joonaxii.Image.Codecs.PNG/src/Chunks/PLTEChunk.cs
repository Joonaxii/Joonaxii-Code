using Joonaxii.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Joonaxii.Image.Codecs.PNG
{
    public class PLTEChunk : PNGChunk
    {
        public FastColor[] pixels;

        public PLTEChunk(IList<ColorContainer> palette) : base(0, PNGChunkType.PLTE, null, 0)
        {
            int palLen = palette.Count * 3;
           // using (var stream = new MemoryStream(new byte[palLen], true))
            //using (BinaryWriter bw = new BinaryWriter(stream))
            {
                data = new byte[palLen];
                pixels = new FastColor[palette.Count];
                int pos = 0;
                for (int i = 0; i < palette.Count; i++)
                {
                    var c = palette[i].color;
                    IOExtensions.WriteToByteArray(data, pos, c.r, 1, false);
                    IOExtensions.WriteToByteArray(data, pos + 1, c.g, 1, false);
                    IOExtensions.WriteToByteArray(data, pos + 2, c.b, 1, false);
      
                    pos += 3;
                    pixels[i] = c;
                }
                length = palLen;
                crc = GetCrc();
            }
        }

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