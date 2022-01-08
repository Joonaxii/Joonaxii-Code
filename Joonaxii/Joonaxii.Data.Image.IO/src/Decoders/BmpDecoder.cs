using Joonaxii.IO;
using System;
using System.IO;

namespace Joonaxii.Data.Image.IO
{
    public class BmpDecoder : ImageDecoderBase
    {
        public BmpDecoder(Stream stream) : base(stream) { }
        public BmpDecoder(BinaryReader br, bool dispose) : base(br, dispose) { }

        public override ImageDecodeResult Decode(bool skipHeader)
        {
            if (!skipHeader)
            {
                var hdr = HeaderManager.GetFileType(_br, false);
                if(hdr != HeaderType.BMP) { return ImageDecodeResult.InvalidImageFormat; }
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

            int hdrSize = br.ReadInt32();

            _width = Math.Abs(br.ReadInt32());
            _height = br.ReadInt32();

            bool topToBot = _height < 0;
            _height = topToBot ? -_height : _height;

            ushort planes = br.ReadUInt16();
            _bpp = (byte)br.ReadUInt16();

            byte cmpMode = (byte)br.ReadInt32();
            switch (cmpMode)
            {
                default: return ImageDecodeResult.NotSupported;
                case 0:break;

                case 3:
                    if(_bpp == 32) { return ImageDecodeResult.NotSupported; }
                    break;
            }
            int imgSize = br.ReadInt32();

            int pxXPerM = br.ReadInt32();
            int pxYPerM = br.ReadInt32();

            int colors = br.ReadInt32();
            int impColors = br.ReadInt32();
            _pixels = new FastColor[_width * _height];

            switch (_bpp)
            {
                case 16:
                    int r = br.ReadInt32();
                    int g = br.ReadInt32();
                    int b = br.ReadInt32();
                    int a = br.ReadInt32();
                    _colorMode = ImageIOExtensions.GetColorMode(_bpp, r, g, b, a);
                    break;

                default:
                    _colorMode = ImageIOExtensions.GetColorMode(_bpp);
                    break;
            }

            int bytesPerP = _bpp / 8;
            int padding = IOExtensions.NextPowerOf(_width * bytesPerP, 4) - (_width * bytesPerP);
            for (int y = 0; y < _height; y++)
            {
                int yP = topToBot ? y : _height - 1 - y;
                for (int x = 0; x < _width; x++)
                {
                    _pixels[yP * _width + x] = br.ReadColor(_colorMode, true);
                }
                br.ReadBytes(padding);
            }

            return ImageDecodeResult.Success;
        }
    }
}
