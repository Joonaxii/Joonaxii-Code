using System;
using System.IO;

namespace Joonaxii.Data.Image.IO
{
    public abstract class ImageDecoderBase : IDisposable
    {
        public int Width { get => _width; }
        public int Height { get => _height; }
        public byte BitsPerPixel { get => _bpp; }

        protected Stream _stream;
        protected BinaryReader _br;
        protected bool _dispose;

        protected int _width;
        protected int _height;

        protected byte _bpp;
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
