using Joonaxii.Data;
using Joonaxii.IO;
using System.IO;

namespace Joonaxii.Image.Codecs.BMP
{
    public class BMPEncoder : ImageEncoderBase
    {
        private const float INCH_TO_METER = 1.0f / 39.3701f;

        public BMPEncoder(int width, int height, byte bPP) : base(width, height, bPP) { }
        public BMPEncoder(int width, int height, ColorMode mode) : base(width, height, mode) { }

        public override ImageEncodeResult Encode(Stream stream, bool leaveStreamOpen)
        {
            switch (_colorMode)
            {
                case ColorMode.Indexed4:
                case ColorMode.Indexed8:
                case ColorMode.RGB555:
                case ColorMode.RGB565:
                case ColorMode.ARGB555: return ImageEncodeResult.NotSupported;
            }

            int bmpSize = 0;
            int startOfData = 14 + 40;

            uint rMask = 0;
            uint gMask = 0;
            uint bMask = 0;
            uint aMask = 0;

            int cmpMode = 0;
            switch (_colorMode) //Generate Masks
            {
                case ColorMode.RGBA32:
                    rMask = 0x00_FF_00_00;
                    gMask = 0x00_00_FF_00;
                    bMask = 0x00_00_00_FF;
                    aMask = 0xFF_00_00_00;
                    startOfData += 16;
                    break;
                case ColorMode.RGB565:
                    rMask = 0x1F;
                    gMask = 0x7E0;
                    bMask = 0xF800;
                    startOfData += 16;
                    break;
                case ColorMode.RGB555:
                    rMask = 0x1F;
                    gMask = 0x3E0;
                    bMask = 0x7C00;
                    aMask = 0x00000;
                    startOfData += 16;
                    break;
                case ColorMode.ARGB555:
                    rMask = 0x1F;
                    gMask = 0x3E0;
                    bMask = 0x7C00;
                    aMask = 0x8000;
                    startOfData += 16;
                    break;
            }

            int bytesPerP = _bpp / 8;
            int padding = IOExtensions.NextPowerOf(_width * bytesPerP, 4) - (_width * bytesPerP);
            switch (_colorMode) //Calculate Data Size
            {
                case ColorMode.RGB24:
                    bmpSize += _pixels.Length * 3 + _height * padding;
                    break; 
                
                case ColorMode.RGBA32:
                    bmpSize += _pixels.Length * 4;
                    cmpMode = 3;
                    break;

                case ColorMode.RGB565:
                case ColorMode.RGB555:
                case ColorMode.ARGB555:
                    bmpSize += _pixels.Length * 2;
                    cmpMode = 3;
                    break;
            }

            int pxPerMX = (int)(_width * 96 * INCH_TO_METER);
            int pxPerMY = (int)(_height * 96 * INCH_TO_METER);

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

                bw.Write(_width);
                bw.Write(-_height);

                bw.Write((ushort)1);
                bw.Write((ushort)_bpp);

                bw.Write(cmpMode);
                bw.Write(bmpSize);

                bw.Write(pxPerMX);
                bw.Write(-pxPerMY);

                bw.Write(0);
                bw.Write(0);

                switch (_colorMode)
                {
                    case ColorMode.RGBA32:
                    case ColorMode.RGB555:
                    case ColorMode.ARGB555:
                    case ColorMode.RGB565:
                        bw.Write(rMask);
                        bw.Write(gMask);
                        bw.Write(bMask);
                        bw.Write(aMask);
                        break;
                }
                bw.WriteColors(_pixels, _width, _height, _colorMode, true, padding);
            }
            return ImageEncodeResult.Success;
        }

        public override void ValidateFormat()
        {
            switch (_colorMode)
            {
                case ColorMode.OneBit:
                case ColorMode.Indexed4:
                case ColorMode.Indexed8:
                case ColorMode.Grayscale:
                    _colorMode = ColorMode.RGB24;
                    _bpp = 24;
                    break;
            }
        }
    }
}
