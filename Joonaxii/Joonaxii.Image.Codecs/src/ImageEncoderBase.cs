using Joonaxii.Data.Coding;
using System;
using System.Collections.Generic;
using System.IO;

namespace Joonaxii.Image.Codecs
{
    public abstract class ImageEncoderBase : CodecBase
    {
        public bool HasPalette { get => _palette != null && _palette.Count > 0; }

        public int Width { get => _width; }
        public int Height { get => _height; }
        public byte BitsPerPixel { get => _bpp; }
        public ColorMode ColorMode { get => _colorMode; }

        protected int _width;
        protected int _height;

        protected byte _bpp;
        protected ColorMode _colorMode;
        protected FastColor[] _pixels;
        protected bool _hasAlpha;

        public ImageDecoderFlags Flags { get => _flags; set => _flags = value; }
        protected ImageDecoderFlags _flags;

        protected Dictionary<FastColor, ColorContainer> _paletteLut = null;
        protected List<ColorContainer> _palette = null;

        public ImageEncoderBase(int width, int height, byte bPP)
        {
            Flags = ImageDecoderFlags.None;
            _width = Math.Abs(width);
            _height = Math.Abs(height);

            SetBitsPerPixel(bPP);
            _pixels = new FastColor[0];
        }

        public ImageEncoderBase(int width, int height, ColorMode mode)
        {
            Flags = ImageDecoderFlags.None;
            _width = Math.Abs(width);
            _height = Math.Abs(height);

            SetColorMode(mode);
            _pixels = new FastColor[0];
        }

        public void SetPalette(IList<FastColor> palette, bool? hasAlpha = null)
        {
            if(palette == null)
            {
                _paletteLut = null;
                _palette = null;
                return;
            }

            _paletteLut = new Dictionary<FastColor, ColorContainer>();
            _palette = new List<ColorContainer>();
            _hasAlpha = hasAlpha != null && hasAlpha.GetValueOrDefault();
            for (int i = 0; i < palette.Count; i++)
            {
                var c = palette[i];
                if (_paletteLut.TryGetValue(c, out ColorContainer val))
                {
                    val.count++;
                    continue;
                }
                val = new ColorContainer(c, 1, _palette.Count);
                _paletteLut.Add(c, val);
                _palette.Add(val);

                if (hasAlpha == null && c.a < 255) { _hasAlpha = true; }
            }

            _palette.Sort();
            for (int i = 0; i < _palette.Count; i++)
            {
                _palette[i].index = i;
            }

            ValidateAlpha(_hasAlpha);
        }
        public void SetPalette(IList<ColorContainer> palette, bool? hasAlpha = null)
        {
            if (palette == null)
            {
                _paletteLut = null;
                _palette = null;
                return;
            }

            _paletteLut = new Dictionary<FastColor, ColorContainer>();
            _palette = new List<ColorContainer>();
            _hasAlpha = hasAlpha != null && hasAlpha.GetValueOrDefault();
            for (int i = 0; i < palette.Count; i++)
            {
                var c = palette[i];
                if (_paletteLut.TryGetValue(c.color, out ColorContainer val)) { continue; }

                val = new ColorContainer(c.color, 1, _palette.Count);
                _paletteLut.Add(c.color, c);
                _palette.Add(val);

                if (hasAlpha == null && c.color.a < 255) { _hasAlpha = true; }
            }

            _palette.Sort();
            for (int i = 0; i < _palette.Count; i++)
            {
                _palette[i].index = i;
            }

            ValidateAlpha(_hasAlpha);
        }

        protected void GeneratePalette(bool force)
        {
            if (_flags.HasFlag(ImageDecoderFlags.ForceNoPalette))
            {
                if (HasPalette)
                {
                    _paletteLut = null;
                    _palette = null;
                }
                return;
            }

            if(!force & HasPalette) { return; }

            _hasAlpha = false;
            _paletteLut = new Dictionary<FastColor, ColorContainer>();
            _palette = new List<ColorContainer>();
            for (int i = 0; i < _pixels.Length; i++)
            {
                var c = _pixels[i];
                if (_paletteLut.TryGetValue(c, out ColorContainer val))
                {
                    val.count++;
                    continue;
                }
                val = new ColorContainer(c, 1, _palette.Count);
                _paletteLut.Add(c, val);
                _palette.Add(val);

                if (c.a < 255) { _hasAlpha = true; }
            }

            _palette.Sort();
            for (int i = 0; i < _palette.Count; i++)
            {
                _palette[i].index = i;
            }

            ValidateAlpha(_hasAlpha);
        }

        public virtual void ValidateFormat()
        {
            switch (_colorMode)
            {
                case ColorMode.Indexed4:
                case ColorMode.Indexed8:
                case ColorMode.Indexed:
                    if (_flags.HasFlag(ImageDecoderFlags.ForceNoPalette))
                    {
                        SetColorMode(ColorMode.RGBA32);

                        if (HasPalette)
                        {
                            _palette = null;
                            _paletteLut = null;
                        }
                        return;
                    }
                    break;
            }
        }
        public abstract ImageEncodeResult Encode(Stream stream, bool leaveStreamOpen);

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

            _hasAlpha = false;
            foreach (var item in _pixels)
            {
                if (item.a < 255) { _hasAlpha = true; break; }
            }
        }

        public void SetPixelsRef(ref FastColor[] colors)
        {
            if (colors == null) { return; }
            _pixels = colors;

            _hasAlpha = false;
            foreach (var item in _pixels)
            {
                if(item.a < 255) { _hasAlpha = true; break; }
            }

            ValidateAlpha(_hasAlpha);
        }

        protected virtual void ValidateAlpha(bool hasAlpha)
        {
            _hasAlpha = hasAlpha;
            switch (_colorMode)
            {
                case ColorMode.ARGB555:
                case ColorMode.RGBA32:
                    SetColorMode(hasAlpha ? _colorMode : ColorMode.RGB24);
                    ValidateFormat();
                    break;

                case ColorMode.RGB24:
                case ColorMode.RGB555:
                case ColorMode.RGB565:
                    SetColorMode(!hasAlpha ? _colorMode : ColorMode.RGBA32);
                    ValidateFormat();
                    break;
            }
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
