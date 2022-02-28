using Joonaxii.Data.Coding;
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

        public IHDRChunk(BinaryReader br, int len, uint crc) : base(len, PNGChunkType.IHDR, crc, 0)
        {
            width = br.ReadInt32BigEndian();
            height = br.ReadInt32BigEndian();

            bitDepth = br.ReadByte();
            colorType = (PNGColorType)br.ReadByte();

            compressionMethod = (PNGCompressionMethod)br.ReadByte();
            filterMethod = (PNGFilterMethod)br.ReadByte();
            interlaceMethod = (InterlaceMethod)br.ReadByte();
        }

        public static void Write(BinaryWriter bw, int width, int height, byte bitDepth, PNGColorType colorType)
        {
            unsafe
            {
                byte* data = stackalloc byte[17];
                IOExtensions.WriteToByteArray(data, 0, (int)PNGChunkType.IHDR, 4, true);

                IOExtensions.WriteToByteArray(data, 4, width, 4, true);
                IOExtensions.WriteToByteArray(data, 8, height, 4, true);

                IOExtensions.WriteToByteArray(data, 12, bitDepth, 1, false);
                IOExtensions.WriteToByteArray(data, 13, (long)colorType, 1, false);

                IOExtensions.WriteToByteArray(data, 14, 0L, 1, false);
                IOExtensions.WriteToByteArray(data, 15, 0L, 1, false);
                IOExtensions.WriteToByteArray(data, 16, 0L, 1, false);
                Write(bw, data, 13);
            }
        }

        public byte GetBytesPerPixel()
        {
            switch (colorType)
            {
                default: return 0;

                case PNGColorType.GRAYSCALE:
                case PNGColorType.PALETTE_IDX: return 1;

                case PNGColorType.RGB: return 3;

                case PNGColorType.GRAY_ALPHA: return 2;
                case PNGColorType.RGB_ALPHA: return 4;
            }
        }

        public int GetBytesPerScanline()
        {
            switch (bitDepth)
            {
                default: return width * GetBytesPerPixel() * ((bitDepth + 7) >> 3);
                case 1: return (width + 7) >> 3;
                case 2: return (width + 3) >> 2;
                case 4: return (width + 1) >> 1;

                    //case 8:
                    //case 16: return width * GetBytesPerPixel() * ((bitDepth + 7) >> 3);
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