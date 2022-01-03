using Joonaxii.IO;
using Joonaxii.MathJX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.Data.Image.IO
{
    public class BmpDecoder : ImageDecoderBase
    {
        public BmpDecoder(Stream stream) : base(stream) { }
        public BmpDecoder(BinaryReader br, bool dispose) : base(br, dispose) { }

        //private FastColor[] _colorTable;

        public override ImageDecodeResult Decode(bool skipHeader)
        {
            if (!skipHeader)
            {
                var hdr = ImageDecoder.GetFileType(_br, false);
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
            if(cmpMode != 0 | _bpp != 24) { return ImageDecodeResult.NotSupported; }
            
            int imgSize = br.ReadInt32();

            int pxXPerM = br.ReadInt32();
            int pxYPerM = br.ReadInt32();

            int colors = br.ReadInt32();
            int impColors = br.ReadInt32();
            _pixels = new FastColor[_width * _height];

            int padding = IOExtensions.NextPowerOf(_width * 3, 4) - (_width * 3);
            for (int y = 0; y < _height; y++)
            {
                int yP = topToBot ? y : _height - 1 - y;
                for (int x = 0; x < _width; x++)
                {
                    byte b = br.ReadByte();
                    byte g = br.ReadByte();
                    byte r = br.ReadByte();
                    _pixels[yP * _width + x] = new FastColor(r, g, b);
                }
                br.ReadBytes(padding);
            }

            return ImageDecodeResult.Success;
        }
    }
}
