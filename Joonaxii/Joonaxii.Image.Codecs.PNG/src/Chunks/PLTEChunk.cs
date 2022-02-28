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

        public PLTEChunk(BinaryReader br, int len, uint crc) : base(len, PNGChunkType.PLTE, crc, 0)
        {
            int palLen = (length / 3);
            pixels = new FastColor[palLen];

            unsafe
            {
                fixed (FastColor* ptr = pixels)
                {
                    for (int i = 0; i < palLen; i++)
                    {
                        ptr[i] = new FastColor(br.ReadByte(), br.ReadByte(), br.ReadByte());
                    }
                }
            }
        }

        public static void Write(BinaryWriter bw, IList<ColorContainer> palette)
        {
            var length = palette.Count * 3;
            unsafe
            {
                byte[] plt = new byte[length + 4];

                fixed(byte* ptr = plt)
                {
                    IOExtensions.WriteToByteArray(plt, 0, (int)PNGChunkType.PLTE, 4, true);
                    int pos = 4;

                    for (int i = 0; i < palette.Count; i++)
                    {
                        var c = palette[i].color;
                        IOExtensions.WriteToByteArray(plt, pos, (int)c, 3, false);
                        pos += 3;
                    }
                    Write(bw, ptr, length);
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

        public void ApplyTransparency(Stream stream, tRNSChunk chunk)
        {
            long pos = stream.Position;
            stream.Seek(chunk.dataStart, SeekOrigin.Begin);

            int min = Math.Min(chunk.length, pixels.Length);
            for (int i = 0; i < min; i++)
            {
                pixels[i].SetAlpha((byte)stream.ReadByte());
            }
            stream.Seek(pos, SeekOrigin.Begin);
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