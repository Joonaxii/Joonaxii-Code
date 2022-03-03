using Joonaxii.Data.Coding;
using Joonaxii.Image.Texturing;
using System;
using System.Collections.Generic;
using System.IO;

namespace Joonaxii.Image.Codecs
{
    public abstract class ImageEncoderBase : CodecBase
    {
        public int Width { get => _texture.Width; }
        public int Height { get => _texture.Height; }

        public byte BitsPerPixel 
        {
            get => _bpp;
            set
            {
                _bpp = value;
                ValidateFormat(ref _format, ref _bpp);
            }
        }
        protected byte _bpp;

        public TextureFormat Format 
        { 
            get => _format;
            set
            {
                _format = value;
                ValidateFormat(ref _format, ref _bpp);
            }
        }
        protected TextureFormat _format;

        public ImageDecoderFlags Flags { get => _flags; set => _flags = value; }
        protected ImageDecoderFlags _flags;

        protected Texture _texture;
        protected Texture _source;

        protected bool _disposeTexture;
        public ImageEncoderBase(TextureFormat format) : this(format, 8, true) { }
        public ImageEncoderBase(Texture source, TextureFormat format) : this(source, format, 8, true) { }

        protected ImageEncoderBase(TextureFormat format, byte bpp, bool disposeTexture)
        {
            Flags = ImageDecoderFlags.None;
   
            _bpp = bpp;
            Format = format;
            _disposeTexture = disposeTexture;
        }

        protected ImageEncoderBase(Texture source, TextureFormat format, byte bpp, bool disposeTexture)
        {
            Flags = ImageDecoderFlags.None;

            _bpp = bpp;
            Format = format;

            _source = source;
            _disposeTexture = disposeTexture;
        }

        public abstract ImageEncodeResult Encode(Stream stream, bool leaveStreamOpen);
        
        protected virtual void ValidateFormat(ref TextureFormat format, ref byte bpp)
        {
            switch (format)
            {
                case TextureFormat.Indexed4:
                case TextureFormat.Indexed8:
                case TextureFormat.Indexed:
                    if (_flags.HasFlag(ImageDecoderFlags.ForceNoPalette))
                    {
                        format = TextureFormat.RGBA32;
                        bpp = 32;
                        return;
                    }
                    break;
            }
        }

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

        public void SetSourceTexture(Texture texture)
        {
            _source = texture;
            if(texture == null) { return; }
            _texture.ConvertTo(_source, _format, _bpp);
        }

        public void Resize(int width, int height)
        {
            width = Math.Abs(width);
            height = Math.Abs(height);

            int reso = width * height;
            if(reso == 0) { return; }

            _texture.SetResolution(width, height);
        }

        protected void GenerateTexture(TextureFormat format, byte bpp)
        {
            if(_texture != null)
            {
                _texture.ConvertTo(_source, format, bpp);
                return;
            }
            _texture = new Texture(_source, format, bpp);
        }

        public Texture GetTexture() => _texture;

        public void CopyFrom(ImageDecoderBase decoder)
        {
            if (!decoder.IsDecoded) { return; }

            _source = decoder.GetTexture();
            BitsPerPixel = _source.BitsPerPixel;
            Format = _source.Format;
        }

        public override void Dispose()
        {
            if (!_disposeTexture) { return; }
            _texture.Dispose();
        }
    }
}
