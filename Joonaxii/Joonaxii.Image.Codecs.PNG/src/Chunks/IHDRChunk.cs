using Joonaxii.IO;
using System.IO;
using System.Text;

namespace Joonaxii.Image.Codecs.PNG
{
    public class IHDRChunk : PNGChunk
    {
        public int width;
        public int height;

        public byte bitDepth;
        public PNGColorType colorType;

        public PNGCompressionMethod compressionMethod;
        public PNGFilterMethod filterMethod;

        public InterlaceMethod interlaceMethod;

        public IHDRChunk(int width, int height, byte bitDepth, PNGColorType colorType) : base(0, PNGChunkType.IHDR, null, 0)
        {
            this.width = width;
            this.height = height;

            this.bitDepth = bitDepth;
            this.colorType = colorType;
            compressionMethod = 0;
            filterMethod = 0;
            interlaceMethod = 0;

            using(MemoryStream ms = new MemoryStream())
            using(BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.WriteBigEndian(width);
                bw.WriteBigEndian(height);
                bw.Write(bitDepth);
                bw.Write((byte)colorType);

                bw.Write((byte)compressionMethod);
                bw.Write((byte)filterMethod);
                bw.Write((byte)interlaceMethod);

                ms.Flush();
                bw.Flush();

                data = ms.ToArray();
            }

            length = data.Length;
            crc = GetCrc();
        }

        public IHDRChunk(int len, byte[] data, uint crc) : base(len, PNGChunkType.IHDR, data, crc)
        {
            using(var stream = GetStream())
            using(BinaryReader br = new BinaryReader(stream))
            {
                width = br.ReadInt32BigEndian();
                height = br.ReadInt32BigEndian();

                bitDepth = br.ReadByte();
                colorType = (PNGColorType)br.ReadByte();

                compressionMethod = (PNGCompressionMethod)br.ReadByte();
                filterMethod = (PNGFilterMethod)br.ReadByte();

                interlaceMethod = (InterlaceMethod)br.ReadByte();
            }
        }

        public byte GetBytesPerPixel()
        {
            switch (colorType)
            {
                default:                        return 0;

                case PNGColorType.GRAYSCALE: 
                case PNGColorType.PALETTE_IDX:  return 1;

                case PNGColorType.RGB:          return 3;

                case PNGColorType.GRAY_ALPHA:   return 2;
                case PNGColorType.RGB_ALPHA:    return 4;
            }
        }

        public int GetBytesPerScanline()
        {
            switch (bitDepth)
            {
                default: return 0;
                case 1:  return (width + 7) >> 3;
                case 2:  return (width + 3) >> 2;
                case 4:  return (width + 1) >> 1;

                case 8:
                case 16: return width * GetBytesPerPixel() * ((bitDepth + 7) >> 3);
            }
        }

        public override string ToMinString() => $"{base.ToMinString()} [{width} W, {height} H, {bitDepth} bit, {colorType}, {compressionMethod}, {filterMethod}, {interlaceMethod}]";

        public override string GetSpedcificInfoString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"    -Width: {width}px");
            sb.AppendLine($"    -Height: {height}px");
            sb.AppendLine($"    -Bit Depth: {bitDepth}");
            sb.AppendLine($"    -Color Type: {colorType}");
            sb.AppendLine($"    -Compression: {compressionMethod}");
            sb.AppendLine($"    -Filter: {filterMethod}");
            sb.AppendLine($"    -Interlace Method: {interlaceMethod}");

            sb.AppendLine(new string('-', 16));
            return sb.ToString();
        }
    }
}