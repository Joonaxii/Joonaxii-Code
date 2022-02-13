using System;
using System.IO;
using System.Text;

namespace Joonaxii.Image.Codecs.PNG
{
    public class PNGEncoder : ImageEncoderBase
    {
        public PNGEncoder(int width, int height, byte bPP) : base(width, height, bPP)
        {
        }

        public PNGEncoder(int width, int height, ColorMode mode) : base(width, height, mode)
        {
           
        }

        public override ImageEncodeResult Encode(Stream stream, bool leaveStreamOpen)
        {
            using(BinaryWriter bw = new BinaryWriter(stream, Encoding.UTF8, leaveStreamOpen))
            {
                byte[] dataBuf = _pixels.ToBytes(PixelByteOrder.RGBA, false, _width, _height, _colorMode);
                byte[] scanLineBuf = new byte[_width * (_bpp >> 3)];
                byte[] lowestSoFar = new byte[scanLineBuf.Length];
                byte[] writeBuf = new byte[scanLineBuf.Length];
                int posInBuf = 0;
                byte[] finalBuf = new byte[dataBuf.Length + _height];

                int lowestVariation;

                for (int y = 0; y < _height; y++)
                {
                    int yP = y * _width;

                    Buffer.BlockCopy(dataBuf, yP, scanLineBuf, 0, scanLineBuf.Length);
                    Buffer.BlockCopy(dataBuf, yP, writeBuf, 0, writeBuf.Length);
                    Buffer.BlockCopy(writeBuf, 0, lowestSoFar, 0, lowestSoFar.Length);

                    lowestVariation = GetByteVariation(writeBuf, 0, writeBuf.Length);

                }

            }
        }

        public override void ValidateFormat()
        {
            switch (_colorMode)
            {
                case ColorMode.ARGB555:
                    _colorMode = ColorMode.RGBA32;
                    _bpp = 32;
                    break;

                case ColorMode.RGB555:
                case ColorMode.RGB565:
                    _colorMode = ColorMode.RGB24;
                    _bpp = 24;
                    break;

                case ColorMode.OneBit:
                    _colorMode = ColorMode.Grayscale;
                    _bpp = 8;
                    break;

                case ColorMode.Indexed4:
                    _colorMode = ColorMode.Indexed8;
                    _bpp = 8;
                    break;
            }
        }

        private byte[] _idatBuffer = new byte[8192];
        private int _idatSize;
        private void WriteScanline(BinaryWriter bw, PNGFilterMethod filterMode, byte[] data, byte[] buff)
        {
            IDATChunk chnk = new IDATChunk();


        }

        private void SubFilter(int scanLine, int x, byte[] data, byte[] targetData)
        {
            int i = scanLine + x;
            byte valA = data[i];
            byte valB = Sub(x, i, data);

            targetData[i] = (byte)(valA - valB);
        }

        private void UpFilter(int x, int y, int w, byte[] data, byte[] targetData)
        {
            int i = y * w + x;
            byte valA = data[i];
            byte valB = Prior(x, y, w, data);

            targetData[i] = (byte)(valA - valB);
        }

        private void AvgFilter(int x, int y, int w, byte[] data, byte[] targetData)
        {
            int i = y * w + x;
            byte valA = data[i];
            byte valB = Sub(x, i, data);
            byte valC = Prior(x, y, w, data);

            targetData[i] = (byte)(valA - ((valB + valC) >> 1));
        }

        private void PaethFilter(int x, int y, int w, byte[] data, byte[] targetData)
        {
            int i = y * w + x;

            byte a = Sub(x, i, data);
            byte b = Prior(x, y, w, data);
            byte c = Prior(x - 1, y, w, data);

            
            int p = a - b + c;

            int pA = Math.Abs(p - a);
            int pB = Math.Abs(p - b);
            int pC = Math.Abs(p - c);

            if(pA <= pB & pA <= pC)
            {
                targetData[i] = a;
                return;
            }
            targetData[i] = pB <= pC ? b : c;
        }

        private byte Sub(int x, int scanline, byte[] data) => x < 1 ? (byte)0 : data[scanline + x - 1];
        private byte Prior(int x, int y, int w, byte[] data) => (x < 0 | y < 1) ? (byte)0 : data[(y - 1) * w + x];

        private int GetByteVariation(byte[] bytes, int start, int len)
        {
            byte v = bytes[start];
            int variation = 0;
            for (int i = start + 1; i < start + len; i++)
            {
                var b = bytes[i];
                if(b != v)
                {
                    v = b;
                    variation++;
                }
            }
            return variation;
        }
    }
}
