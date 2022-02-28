using Joonaxii.Data.Coding;
using Joonaxii.Image.Texturing;
using Joonaxii.MathJX;
using System;
using System.IO;

namespace Joonaxii.Image.Codecs
{
    public abstract class ImageDecoderBase : CodecBase
    {
        protected const int MAX_STACK_ALLOC = 128_000; 

        public int Width { get => IsDecoded ? _texture.Width : 0; }
        public int Height { get => IsDecoded ? _texture.Height : 0; }
        public byte BitsPerPixel { get => IsDecoded ? _texture.BitsPerPixel : (byte)0; }
        public ColorMode ColorMode { get => IsDecoded ? _texture.Format : ColorMode.RGBA32; }

        public bool IsDecoded { get => _texture != null; }

        protected Stream _stream;
        protected BinaryReader _br;
        protected bool _dispose;
        protected bool _releaseTexture;

        protected Texture _texture;

        public ImageDecoderBase(Stream stream) : this(stream, true) { }
        public ImageDecoderBase(Stream stream, bool releaseTexture)
        {
            _releaseTexture = releaseTexture;
            _stream = stream;
            _br = new BinaryReader(_stream);
            _dispose = true;
            _texture = null;
        }

        public ImageDecoderBase(BinaryReader br, bool dispose) : this(br, dispose, true) { }
        public ImageDecoderBase(BinaryReader br, bool dispose, bool releaseTexture)
        {
            _releaseTexture = releaseTexture;
            _stream = br.BaseStream;
            _br = br;
            _dispose = dispose;
            _texture = null;
        }

        public abstract ImageDecodeResult Decode(bool skipHeader);

        //public byte[] GetBytes(PixelByteOrder byteOrder, bool invertY) => _pixels.ToBytes(byteOrder, invertY, _width, _height, _colorMode);
        //public byte[] GetBytes(PixelByteOrder byteOrder, bool invertY, ColorMode mode) => _pixels.ToBytes(byteOrder, invertY, _width, _height, mode);

        public Texture GetTexture() => _texture;

        protected void GenerateTexture(int width, int height, ColorMode format, byte bpp)
        {
            width = Maths.Clamp(width, 1, ushort.MaxValue);
            height = Maths.Clamp(height, 1, ushort.MaxValue);

            if(_texture != null)
            {
                _texture.SetResolution(width, height);
                _texture.BitsPerPixel = bpp;
                _texture.Format = format;
                return;
            }
            _texture = new Texture(width, height, format, bpp);
        }

        public override void Dispose()
        {
            if (!_dispose) { return; }
            _stream.Dispose();
            _br.Dispose();

            _texture?.Dispose();
            _texture = null;
        }
    }
}
