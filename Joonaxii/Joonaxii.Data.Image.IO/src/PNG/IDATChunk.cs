using System.Collections.Generic;
using System.Text;

namespace Joonaxii.Data.Image.Conversion.PNG
{
    public class IDATChunk : PNGChunk
    {
        public IDATChunk(int len, byte[] data, uint crc) : base(len, PNGChunkType.IDAT, data, crc)
        {
        }

        public override string GetSpedcificInfoString() => new string('-', 16);
    }
}