using Joonaxii.Data.Coding;
using System;
using System.IO;

namespace Joonaxii.Image.Codecs
{
    public abstract class ImageDecoderBase : CodecBase
    {
        public int Width { get => _width; }
        public int Height { get => _height; }
        public byte BitsPerPixel { get => _bpp; }
        public ColorMode ColorMode { get => _colorMode; }
        public bool IsDecoded { get => _pixels != null; }

        protected Stream _stream;
        protected BinaryReader _br;
        protected bool _dispose;

        protected int _width;
        protected int _height;

        protected byte _bpp
        {
            get => _bppPriv;
            set
            {
                _bppPriv = value;
                ValidateFormat();
            }
        }
        private byte _bppPriv;

        protected ColorMode _colorMode 
        {
            get => _colorModePriv;
            set
            {
                _colorModePriv = value;
                ValidateFormat();
            }
        }
        private ColorMode _colorModePriv;
        protected FastColor[] _pixels;
        
        public ImageDecoderBase(Stream stream)
        {
            _stream = stream;
            _br = new BinaryReader(_stream);
            _dispose = true;
            _pixels = null;
        }
        public ImageDecoderBase(BinaryReader br, bool dispose)
        {
            _stream = br.BaseStream;
            _br = br;
            _dispose = dispose;
            _pixels = null;
        }

        public abstract void ValidateFormat();
        public abstract ImageDecodeResult Decode(bool skipHeader);

        public byte[] GetBytes(PixelByteOrder byteOrder, bool invertY) => _pixels.ToBytes(byteOrder, invertY, _width, _height, _colorMode);
        public byte[] GetBytes(PixelByteOrder byteOrder, bool invertY, ColorMode mode) => _pixels.ToBytes(byteOrder, invertY, _width, _height, mode);

        public FastColor[] GetPixels()
        {
            FastColor[] pix = new FastColor[_pixels.Length];
            Array.Copy(_pixels, pix, pix.Length);
            return pix;
        }

        public FastColor[] GetPixelsRef() => _pixels;

        public int GetPixels(FastColor[] buffer)
        {
            int min = buffer.Length < _pixels.Length ? _pixels.Length : buffer.Length;
            Array.Copy(_pixels, buffer, min);
            return min;
        }

        public FastColor GetPixel(int i) => _pixels[i];
        public FastColor GetPixel(int x, int y) => _pixels[y * _width + x];

        public override void Dispose()
        {
            if (!_dispose) { return; }
            _stream.Dispose();
            _br.Dispose();

            _pixels = null;
        }
    }
}
