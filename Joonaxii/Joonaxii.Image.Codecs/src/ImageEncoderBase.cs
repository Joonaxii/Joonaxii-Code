using Joonaxii.Data.Coding;
using System;
using System.IO;

namespace Joonaxii.Image.Codecs
{
    public abstract class ImageEncoderBase : CodecBase
    {
        public int Width { get => _width; }
        public int Height { get => _height; }
        public byte BitsPerPixel { get => _bpp; }
        public ColorMode ColorMode { get => _colorMode; }

        protected int _width;
        protected int _height;

        protected byte _bpp;
        protected ColorMode _colorMode;
        protected FastColor[] _pixels;

        public ImageEncoderBase(int width, int height, byte bPP)
        {
            _width = Math.Abs(width);
            _height = Math.Abs(height);

            SetBitsPerPixel(bPP);

            _pixels = new FastColor[width * height];
        }

        public ImageEncoderBase(int width, int height, ColorMode mode)
        {
            _width = Math.Abs(width);
            _height = Math.Abs(height);

            SetColorMode(mode);

            _pixels = new FastColor[width * height];
        }

        public abstract void ValidateFormat();

        public virtual ImageEncodeResult Encode(ImageDecoderBase decoder, Stream stream, bool leaveStreamOpen)
        {
            if (!decoder.IsDecoded)
            {
                switch (decoder.Decode(false))
                {
                    case ImageDecodeResult.Success: break;
                    default: return ImageEncodeResult.EncodeFailed;
                }
            }

            CopyFrom(decoder);
            return Encode(stream, leaveStreamOpen);
        }
        public abstract ImageEncodeResult Encode(Stream stream, bool leaveStreamOpen);

        public void SetBitsPerPixel(byte bPP)
        {
            _colorMode = ImageCodecExtensions.GetColorMode(bPP, g: 0x7E0);
            _bpp = bPP;
            ValidateFormat();
        }

        public void SetColorMode(ColorMode cmMode)
        {
            _colorMode = cmMode;
            _bpp = cmMode.GetBPP();
            ValidateFormat();
        }

        public void Resize(int width, int height)
        {
            width = Math.Abs(width);
            height = Math.Abs(height);

            int reso = width * height;
            if(reso == 0) { return; }

            _width = width;
            _height = height;
            Array.Resize(ref _pixels, reso);
        }

        public void SetPixels(FastColor[] colors)
        {
            if(colors == null) { return; }

            int len = Math.Min(_pixels.Length, colors.Length);
            Array.Copy(colors, _pixels, len);
        }

        public void SetPixelsRef(ref FastColor[] colors)
        {
            if (colors == null) { return; }
            _pixels = colors;
        }

        public bool Save(string path)
        {
            using(FileStream stream = new FileStream(path, FileMode.Create))
            {
                if (Save(stream))
                {
                    return true;
                }
                if (File.Exists(path)) { File.Delete(path); }
            }
            return false;
        }

        public virtual bool Save(Stream stream) => false;

        public void CopyFrom(ImageDecoderBase decoder)
        {
            if (!decoder.IsDecoded) { return; }

            _width = Math.Abs(decoder.Width);
            _height = Math.Abs(decoder.Height);

            _bpp = decoder.BitsPerPixel;
            _colorMode = decoder.ColorMode;
            ValidateFormat();

            int reso = _width * _height;
            if (reso == 0) { return; }
            Array.Resize(ref _pixels, reso);
            decoder.GetPixels(_pixels);
        }

        public void SetPixel(int i, FastColor c) => _pixels[i] = c;
        public void SetPixel(int x, int y, FastColor c) => _pixels[y * _width + x] = c;

        public override void Dispose()
        {
            _pixels = null;
        }
    }
}
