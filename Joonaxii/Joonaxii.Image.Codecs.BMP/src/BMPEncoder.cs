using Joonaxii.Data;
using Joonaxii.Image.Texturing;
using Joonaxii.IO;
using System.IO;

namespace Joonaxii.Image.Codecs.BMP
{
    public class BMPEncoder : ImageEncoderBase
    {
        private const float INCH_TO_METER = 1.0f / 39.3701f;

        public BMPEncoder(TextureFormat mode) : base(mode) { }
        public BMPEncoder(Texture original, TextureFormat mode) : base(original, mode) { }

        public override ImageEncodeResult Encode(Stream stream, bool leaveStreamOpen)
        {
            ValidateFormat(ref _format, ref _bpp);
            switch (_format)
            {
                case TextureFormat.Indexed4:
                case TextureFormat.Indexed8:
                case TextureFormat.RGB555:
                case TextureFormat.RGB565:
                case TextureFormat.ARGB555: return ImageEncodeResult.NotSupported;
            }

            int bmpSize = 0;
            int startOfData = 14 + 40;

            uint rMask = 0;
            uint gMask = 0;
            uint bMask = 0;
            uint aMask = 0;

            int cmpMode = 0;
            switch (_format) //Generate Masks
            {
                case TextureFormat.RGBA32:
                    rMask = 0x00_FF_00_00;
                    gMask = 0x00_00_FF_00;
                    bMask = 0x00_00_00_FF;
                    aMask = 0xFF_00_00_00;
                    startOfData += 16;
                    break;
                case TextureFormat.RGB565:
                    rMask = 0x1F;
                    gMask = 0x7E0;
                    bMask = 0xF800;
                    startOfData += 16;
                    break;
                case TextureFormat.RGB555:
                    rMask = 0x1F;
                    gMask = 0x3E0;
                    bMask = 0x7C00;
                    aMask = 0x00000;
                    startOfData += 16;
                    break;
                case TextureFormat.ARGB555:
                    rMask = 0x1F;
                    gMask = 0x3E0;
                    bMask = 0x7C00;
                    aMask = 0x8000;
                    startOfData += 16;
                    break;
            }

            int bytesPerP = _bpp / 8;
            int padding = IOExtensions.NextDivBy(_texture.Width * bytesPerP, 4) - (_texture.Width * bytesPerP);
            switch (_format) //Calculate Data Size
            {
                case TextureFormat.RGB24:
                    bmpSize += (_texture.Width * _texture.Height) * 3 + _texture.Height * padding;
                    break; 
                
                case TextureFormat.RGBA32:
                    bmpSize += (_texture.Width * _texture.Height) * 4;
                    cmpMode = 3;
                    break;

                case TextureFormat.RGB565:
                case TextureFormat.RGB555:
                case TextureFormat.ARGB555:
                    bmpSize += (_texture.Width * _texture.Height) * 2;
                    cmpMode = 3;
                    break;
            }

            int pxPerMX = (int)(_texture.Width * 96 * INCH_TO_METER);
            int pxPerMY = (int)(_texture.Height * 96 * INCH_TO_METER);

            ///bmpSize += startOfData;
            using (BitWriter bw = new BitWriter(stream, leaveStreamOpen))
            {
                HeaderManager.TryGetHeader(HeaderType.BMP, out var hdr);

                //File Header
                hdr.WriteHeader(bw);
                bw.Write(bmpSize + startOfData);
                bw.Write(0);
                bw.Write(startOfData);

                //DIB Header
                bw.Write(40);

                bw.Write(_texture.Width);
                bw.Write(-_texture.Height);

                bw.Write((ushort)1);
                bw.Write((ushort)_bpp);

                bw.Write(cmpMode);
                bw.Write(bmpSize);

                bw.Write(pxPerMX);
                bw.Write(-pxPerMY);

                bw.Write(0);
                bw.Write(0);

                switch (_format)
                {
                    case TextureFormat.RGBA32:
                    case TextureFormat.RGB555:
                    case TextureFormat.ARGB555:
                    case TextureFormat.RGB565:
                        bw.Write(rMask);
                        bw.Write(gMask);
                        bw.Write(bMask);
                        bw.Write(aMask);
                        break;
                }
               // bw.WriteColors(_pixels, _width, _height, _colorMode, true, padding);
            }
            return ImageEncodeResult.Success;
        }

        protected override void ValidateFormat(ref TextureFormat format, ref byte bpp)
        {
            base.ValidateFormat(ref format, ref bpp);
            switch (format)
            {
                case TextureFormat.ARGB555:
                    format = _texture.HasAlpha ? format : TextureFormat.RGB555;
                    break;

                case TextureFormat.RGBA32:
                    if (_texture.HasAlpha)
                    {
                        break;
                    }
                    format =  TextureFormat.RGB24;
                    _bpp = 24;
                    break;

                case TextureFormat.OneBit:
                case TextureFormat.Grayscale:
                    format = TextureFormat.RGB24;
                    _bpp = 24;
                    break;
                case TextureFormat.GrayscaleAlpha:
                case TextureFormat.Indexed4:
                case TextureFormat.Indexed8:
                case TextureFormat.Indexed:
                    format = TextureFormat.RGBA32;
                    _bpp = 32;
                    break;
            }
        }
    }
}
