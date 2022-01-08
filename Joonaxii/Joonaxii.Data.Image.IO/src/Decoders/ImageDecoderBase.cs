using System;
using System.IO;

namespace Joonaxii.Data.Image.IO
{
    public abstract class ImageDecoderBase : IDisposable
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

        protected byte _bpp;
        protected ColorMode _colorMode;
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

        public abstract ImageDecodeResult Decode(bool skipHeader);

        public FastColor[] GetPixels()
        {
            FastColor[] pix = new FastColor[_pixels.Length];
            Array.Copy(_pixels, pix, pix.Length);
            return pix;
        }

        public int GetPixels(FastColor[] buffer)
        {
            int min = buffer.Length < _pixels.Length ? _pixels.Length : buffer.Length;
            Array.Copy(_pixels, buffer, min);
            return min;
        }

        public FastColor GetPixel(int i) => _pixels[i];
        public FastColor GetPixel(int x, int y) => _pixels[y * _width + x];

        public virtual void Dispose()
        {
            if (!_dispose) { return; }
            _stream.Dispose();
            _br.Dispose();

            _pixels = null;
        }

    }
}
