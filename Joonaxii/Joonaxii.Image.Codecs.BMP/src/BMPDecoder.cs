using Joonaxii.Data;
using Joonaxii.Image.Texturing;
using Joonaxii.IO;
using System;
using System.IO;

namespace Joonaxii.Image.Codecs.BMP
{
    public class BMPDecoder : ImageDecoderBase
    {
        public BMPDecoder(Stream stream) : base(stream) { }
        public BMPDecoder(BinaryReader br, bool dispose) : base(br, dispose) { }

        public override ImageDecodeResult Decode(bool skipHeader)
        {
            if (!skipHeader)
            {
                var hdr = HeaderManager.GetFileType(_br, false);
                if (hdr != HeaderType.BMP) { return ImageDecodeResult.InvalidImageFormat; }
            }

            MemoryStream ms = null;
            BitReader br = _br as BitReader;
            bool isBitReader = br != null;

            if (!isBitReader)
            {
                ms = new MemoryStream();
                _br.BaseStream.CopyToWithPos(ms);
                br = new BitReader(ms);
            }

            int size = br.ReadInt32();
            int res = br.ReadInt32();
            int dataOffset = br.ReadInt32();

            int hdrize = br.ReadInt32();

            int width = Math.Abs(br.ReadInt32());
            int height = br.ReadInt32();

            bool topToBot = height < 0;
            height = topToBot ? -height : height;

            ushort planes = br.ReadUInt16();
            byte bpp = (byte)br.ReadUInt16();

            byte cmpMode = (byte)br.ReadInt32();

            System.Diagnostics.Debug.Print($"BMP Info: {width} W, {height} H, Rev Y: {topToBot}, BPP: {bpp}, Cmp: {cmpMode}");

            switch (cmpMode)
            {
                default: return ImageDecodeResult.NotSupported;
                case 0: break; //None
                case 1: break; //8 Bit RLE
                case 3: break; //Bit Fields
            }
            int imgSize = br.ReadInt32();

            int pxXPerM = br.ReadInt32();
            int pxYPerM = br.ReadInt32();

            int colors = br.ReadInt32();
            int impColors = br.ReadInt32();

            ColorMode mode;

            switch (bpp)
            {
                case 16:
                case 32:
                    int r = br.ReadInt32();
                    int g = br.ReadInt32();
                    int b = br.ReadInt32();
                    int a = br.ReadInt32();
                    mode = ImageCodecExtensions.GetColorMode(bpp, r, g, b, a);
                    break;

                default:
                    mode = ImageCodecExtensions.GetColorMode(bpp);
                    break;
            }

            _texture = new Texture(width, height, mode);

            int bytesPerP = bpp >> 3;
            int padding = IOExtensions.NextDivBy(width * bytesPerP, 4) - (width * bytesPerP);

            unsafe
            {
               // byte* ptrPix = (byte*)_texture.LockBits();
                for (int y = 0; y < height; y++)
                {
                    int yP = topToBot ? y : height - 1 - y;
                    int scan = yP * _texture.Width;
                    for (int x = 0; x < width; x++)
                    {
                        _texture.SetPixel(scan + x, br.ReadColor(mode, true));
                    }
                    br.ReadBytes(padding);
                }
               // _texture.UnlockBits();
            }


            return ImageDecodeResult.Success;
        }
    }
}
