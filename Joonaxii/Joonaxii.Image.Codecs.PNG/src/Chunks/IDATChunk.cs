using System.Collections.Generic;
using System.Text;

namespace Joonaxii.Image.Codecs.PNG
{
    public class IDATChunk : PNGChunk
    {
        public IDATChunk() : base(0, PNGChunkType.IDAT, new byte[0], 0) { }
        public IDATChunk(int len, byte[] data, uint crc) : base(len, PNGChunkType.IDAT, data, crc)
        {
        }

        public void SetData(byte[] data, int len)
        {
            length = len;
            this.data = data;
            crc = GetCrc();
        }

        public override string GetSpedcificInfoString() => new string('-', 16);
    }
}