using System.Collections.Generic;
using System.Text;

namespace Joonaxii.Image.Codecs.PNG
{
    public class tRNSChunk : PNGChunk
    {
        public tRNSChunk(IList<ColorContainer> palette) : base(palette.Count, PNGChunkType.tRNS, new byte [palette.Count], 0)
        {
            for (int i = 0; i < palette.Count; i++)
            {
                data[i] = palette[i].color.a;
            }
            crc = GetCrc();
        }

        public tRNSChunk(int len, byte[] data, uint crc) : base(len, PNGChunkType.tRNS, data, crc) { }

        public override string ToMinString() => base.ToMinString();
        public override string GetSpedcificInfoString() => string.Empty;
    }
}