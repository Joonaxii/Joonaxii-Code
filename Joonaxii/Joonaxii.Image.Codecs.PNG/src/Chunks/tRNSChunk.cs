using Joonaxii.Data.Coding;
using Joonaxii.IO;
using System.Collections.Generic;
using System.IO;

namespace Joonaxii.Image.Codecs.PNG
{
    public class tRNSChunk : PNGChunk
    {
        public byte[] alphaData;

        public tRNSChunk(int len, uint crc, long pos) : base(len, PNGChunkType.tRNS, crc, pos)  { }

        public static void Write(BinaryWriter bw, IList<FastColor> palette)
        {
            var length = palette.Count;
            unsafe
            {
                byte[] alpha = new byte[length + 4];
                fixed(byte* alph = alpha)
                {
                    IOExtensions.WriteToByteArray(alpha, 0, (int)PNGChunkType.tRNS, 4, true);
                    for (int i = 0, j = 4; i < length; i++, j++)
                    {
                        alpha[j] = palette[i].a;
                    }
                    Write(bw, alph, length);
                }
            }
        }

        public override string ToMinString() => base.ToMinString();
        public override string GetSpedcificInfoString() => string.Empty;
    }
}