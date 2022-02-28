using Joonaxii.MathJX;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Joonaxii.Image.Texturing
{
    public class Texture : IDisposable
    {
        private unsafe delegate FastColor PixelAction(byte* ptr, int bpp, IList<FastColor> palette);
        private unsafe delegate void BytePixelAction(byte* ptr, byte* data, int bpp, IList<FastColor> palette);

        public int Width { get => _width; }
        public int Height { get => _height; }

        public bool HasPalette { get => _palette != null; }
        public int GetPaletteLength { get => HasPalette ? _palette.Count : 0; }

        public IntPtr Scan { get => _scan; }
        private IntPtr _scan;

        public ColorMode Format
        {
            get => _format;
            set => SetFormat(value, true);
        }
        public int ScanSize { get => _scanSize; }
        public byte BitsPerPixel 
        { 
            get => _bpp;
            set
            {
                switch (Format)
                {
                    default:return;
                    case ColorMode.Indexed: break;
                }

                if(_bpp != value)
                {
                    ReadjustToNewBpp(value, Format);
                }
            }
        }
        public byte BytesPerPixel { get => _bytesPerPix; }

        public TextureDataMode PixelIterationMode 
        { 
            get => _dataLayout;
            set 
            {
                if(value == _dataLayout) { return; }

                int reso = _width * _height;
                if (value == TextureDataMode.Auto)
                {
                    _dataLayout = ResolutionToLayout(reso);
                    return;
                }
                _dataLayout = value;

                if((reso % (int)_dataLayout) != 0)
                {
                    _dataLayout = ResolutionToLayout(reso);
                }
            }
        }

        private ColorMode _format;
        private int _scanSize;
        private byte _bpp;
        private byte _bytesPerPix;

        private ushort _width;
        private ushort _height;

        private ushort _pW;
        private ushort _pH;
        private ResizeMode _mode;

        private byte[] _data;

        private TextureModification _pendingMods;
        private GCHandle _handle;

        private FastColor _fillColor;

        private Dictionary<FastColor, int> _paletteLut = null;
        private List<FastColor> _palette = null;
        private TextureDataMode _dataLayout;

        public Texture(int width, int height, ColorMode format) : this(width, height, format, 8, TextureDataMode.Auto, FastColor.black) { }
        public Texture(int width, int height, ColorMode format, byte bpp) : this(width, height, format, bpp, TextureDataMode.Auto, FastColor.black) { }
        public Texture(int width, int height, ColorMode format, FastColor fillColor) : this(width, height, format, 8, TextureDataMode.Auto, fillColor) { }
        private Texture(int width, int height, ColorMode format, byte bpp, TextureDataMode iterationMode, FastColor fillColor)
        {
            _width = (ushort)Maths.Clamp(width, 0, ushort.MaxValue);
            _height = (ushort)Maths.Clamp(height, 0, ushort.MaxValue);

            _bpp = bpp;
            SetFormat(format, false);

            int reso = _width * _height;
            _dataLayout = iterationMode == TextureDataMode.Auto ? ResolutionToLayout(reso) : iterationMode;

            _fillColor = fillColor;
            unsafe
            {
                _data = new byte[reso * _bytesPerPix];
                fixed (byte* ptr = _data)
                {
                    for (int i = 0; i < reso; i++)
                    {
                        int iD = i * _bytesPerPix;
                        SetColor(ptr, iD, _bytesPerPix, fillColor, _format);
                    }
                }
            }

            _handle = default(GCHandle);
            _scan = IntPtr.Zero;
            _pendingMods = TextureModification.None;
        }

        public Texture(Texture other)
        {
            _width = other._width;
            _height = other._height;

            SetFormat(other.Format, false);

        }

        private TextureDataMode ResolutionToLayout(int reso)
        {
            if (reso < 2) { return TextureDataMode.DB1; }

            if((reso % 32) == 0)  { return TextureDataMode.DB32; }
            if((reso % 16) == 0)  { return TextureDataMode.DB16; }
            if((reso % 8 ) == 0)  { return TextureDataMode.DB8; }
            if((reso % 4 ) == 0)  { return TextureDataMode.DB4; }
            if((reso % 3 ) == 0)  { return TextureDataMode.DB3; }

            return TextureDataMode.DB2;
        }

        public FastColor GetPixel(int x, int y) => GetPixel(y * _width + x);
        public FastColor GetPixel(int i)
        {
            if (i < 0 | i >= _width * _height) { throw new IndexOutOfRangeException("Index is out of range of the pixel array!"); }
            unsafe
            {
                int bpp = _bpp >> 3;
                fixed (byte* ptr = _data)
                {
                    return ColorExtensions.GetColor(ptr + (i * bpp), bpp, _format, _palette);
                }
            }
        }

        public FastColor[] GetPixels()
        {
            if (_data == null) { return null; }
            FastColor[] pix = new FastColor[_width * _height];
            unsafe
            {
                fixed(FastColor* cPtr = pix)
                {
                    GetPixels((byte*)cPtr, pix.Length);
                }
            }
            return pix;
        }

        public void GetPixels(FastColor[] pixels)
        {
            int len = Math.Min(pixels.Length, _width * _height);
            unsafe
            {
                fixed(FastColor* cPtr = pixels)
                {
                    GetPixels((byte*)cPtr, len);
                }
            }
        }

        private unsafe void GetPixels(byte* cPtr, int len)
        {
            unsafe
            {
                fixed (byte* ptr = _data)
                {
                    byte* ptrData = ptr;
                    int bytesPerPix = BitsPerPixel >> 3;

                    BytePixelAction pxAct;
                    switch (_format)
                    {
                        default: pxAct = ColorExtensions.GetIndexedPtr; break;

                        case ColorMode.RGB24: pxAct = ColorExtensions.GetRGB24Ptr; break;
                        case ColorMode.RGBA32: pxAct = ColorExtensions.GetRGBA32Ptr; break;

                        case ColorMode.RGB555: pxAct = ColorExtensions.GetRGB555Ptr; break;
                        case ColorMode.RGB565: pxAct = ColorExtensions.GetRGB565Ptr; break;
                        case ColorMode.ARGB555: pxAct = ColorExtensions.GetARGB555Ptr; break;

                        case ColorMode.Grayscale: pxAct = ColorExtensions.GetGrayscalePtr; break;
                        case ColorMode.GrayscaleAlpha: pxAct = ColorExtensions.GetGrayscaleAlphaPtr; break;
                    }

                    switch (_dataLayout)
                    {
                        default:
                            while (len-- > 0)
                            {
                                pxAct.Invoke(ptrData, cPtr, _bytesPerPix, _palette);
                                cPtr += 4;
                                ptrData += bytesPerPix;
                            }
                            break;

                        case TextureDataMode.DB2:
                            bytesPerPix <<= 1;
                            len >>= 1;
                            while (len-- > 0)
                            {
                                for (int i = 0; i < 2; i++)
                                {
                                    pxAct.Invoke(ptrData, cPtr, _bytesPerPix, _palette);
                                }
                                cPtr += 4 * 2;
                                ptrData += bytesPerPix;
                            }
                            break;

                        case TextureDataMode.DB3:
                            bytesPerPix *= 3;
                            len /= 3;
                            while (len-- > 0)
                            {
                                for (int i = 0; i < 3; i++)
                                {
                                    pxAct.Invoke(ptrData, cPtr, _bytesPerPix, _palette);
                                }
                                cPtr += 4 * 3;
                                ptrData += bytesPerPix;
                            }
                            break;

                        case TextureDataMode.DB4:
                            bytesPerPix <<= 2;
                            len >>= 2;
                            while (len-- > 0)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    pxAct.Invoke(ptrData, cPtr, _bytesPerPix, _palette);
                                }
                                cPtr += 4 * 4;
                                ptrData += bytesPerPix;
                            }
                            break;

                        case TextureDataMode.DB5:
                            bytesPerPix *= 5;
                            len /= 5;
                            while (len-- > 0)
                            {
                                for (int i = 0; i < 5; i++)
                                {
                                    pxAct.Invoke(ptrData, cPtr, _bytesPerPix, _palette);
                                }
                                cPtr += 4 * 5;
                                ptrData += bytesPerPix;
                            }
                            break;

                        case TextureDataMode.DB8:
                            bytesPerPix <<= 3;
                            len >>= 3;
                            while (len-- > 0)
                            {
                                for (int i = 0; i < 8; i++)
                                {
                                    pxAct.Invoke(ptrData, cPtr, _bytesPerPix, _palette);
                                }
                                cPtr += 4 * 8;
                                ptrData += bytesPerPix;
                            }
                            break;

                        case TextureDataMode.DB16:
                            bytesPerPix <<= 4;
                            len >>= 4;
                            while (len-- > 0)
                            {
                                for (int i = 0; i < 16; i++)
                                {
                                    pxAct.Invoke(ptrData, cPtr, _bytesPerPix, _palette);
                                }
                                cPtr += 4 * 16;
                                ptrData += bytesPerPix;
                            }
                            break;

                        case TextureDataMode.DB32:
                            bytesPerPix <<= 5;
                            len >>= 5;
                            while (len-- > 0)
                            {
                                for (int i = 0; i < 32; i++)
                                {
                                    pxAct.Invoke(ptrData, cPtr, _bytesPerPix, _palette);
                                }
                                cPtr += 4 * 32;
                                ptrData += bytesPerPix;
                            }
                            break;
                    }
                }
            }
        }

        public FastColor GetPaletteColor(int i) => HasPalette ? _palette[i] : FastColor.clear;

        private void SetFormat(ColorMode format, bool adjustBpp)
        {
            byte bpp = 8;
            ColorMode fmt = format;
            switch (fmt)
            {
                case ColorMode.Grayscale:
                    bpp = 8;
                    break;

                case ColorMode.OneBit:
                case ColorMode.Indexed4:
                case ColorMode.Indexed8:
                    fmt = ColorMode.Indexed;
                    bpp = 8;
                    break;

                case ColorMode.RGB24:
                    bpp = 24;
                    break;

                case ColorMode.RGBA32:
                    bpp = 32;
                    break;

                case ColorMode.RGB555:
                case ColorMode.RGB565:
                case ColorMode.ARGB555:
                case ColorMode.GrayscaleAlpha:
                    bpp = 16;
                    break;
            }

            if (!adjustBpp || !ReadjustToNewBpp(bpp, fmt))
            {
                _bpp = bpp;
                _format = fmt;
                _bytesPerPix = (byte)(bpp >> 3);

                _scanSize = _width * _bytesPerPix;


                ValidatePalette();
                return;
            }
            _bytesPerPix = (byte)(bpp >> 3);
        }

        private unsafe void SetColor(byte* ptr, int iD, int bpp, FastColor color, ColorMode format)
        {
            int ind = 0;
            switch (format)
            {
                case ColorMode.Indexed:
                    if (!_paletteLut.TryGetValue(color, out var index))
                    {
                        _paletteLut.Add(color, index = _palette.Count);
                        _palette.Add(color);

                        var bt = Maths.BytesNeeded(_palette.Count) << 3;
                        if (bt != _bpp)
                        {
                            ReadjustToNewBpp((byte)bt, _format);
                            bpp = bt >> 3;
                        }
                    }
                    ind = index;
                    break;
                case ColorMode.RGB24:
                    ind = (int)color & 0xFF_FF_FF;
                    break;
                case ColorMode.RGBA32:
                    ind = (int)color;
                    break;

                case ColorMode.RGB565:
                    ind = ColorExtensions.ToRGB565(color, false);
                    break;
                case ColorMode.RGB555:
                    ind = ColorExtensions.ToRGB555(color, false);
                    break;
                case ColorMode.ARGB555:
                    ind = ColorExtensions.ToARGB555(color, false);
                    break;

                case ColorMode.Grayscale:
                    ptr[iD] = ColorExtensions.ToGrayscale(color);
                    return;
                case ColorMode.GrayscaleAlpha:
                    ptr[iD] = ColorExtensions.ToGrayscale(color);
                    ptr[iD + 1] = color.a;
                    return;
            }

            for (int i = 0; i < bpp; i++)
            {
                ptr[iD + i] = (byte)((ind >> (i << 3)) & 0xFF);
            }
        }

        public void CopyTo(Texture other)
        {
            if(other.Format == Format)
            {
                if(Format == ColorMode.Indexed)
                {
                    other.SetPalette(_palette, _paletteLut);
                }

                unsafe
                {
                    fixed (byte* ptrDest = other._data)
                    {
                        fixed (byte* ptrSrc = _data)
                        {
                            BufferUtils.Memcpy(ptrDest, ptrSrc, _width * _height * _bytesPerPix);
                        }
                    }
                }
                return;
            }

            FastColor[] temp = GetPixels();
            other.SetPixels(temp);
        }

        public void SetPixel(int x, int y, FastColor color) => SetPixel(y * _width + x, color);
        public void SetPixel(int i, FastColor color)
        {
            if (i < 0 | i >= _width * _height)
            {
                throw new IndexOutOfRangeException("Index is out of range of the pixel array!");
            }

            unsafe
            {
                int bpp = _bpp >> 3;
                fixed (byte* ptr = _data)
                {
                    SetColor(ptr, i * bpp, bpp, color, _format);
                }
            }
        }

        public void SetPixels(FastColor pixel)
        {
            int res = _width * _height;
            unsafe
            {
                int bpp = _bpp >> 3;
                fixed (byte* ptr = _data)
                {
                    int l = _width * _height * _bytesPerPix;
                    byte* ptrPix = ptr;

                    while (l-- > 0)
                    {
                        SetColor(ptrPix++, 0, bpp, pixel, _format);
                    }
                }
            }
        }

        public void SetPixels(FastColor[] pixels)
        {
            if (pixels == null) { throw new NullReferenceException("Given pixel array was null!"); }

            int res = _width * _height;
            if (pixels.Length != res) { throw new ArgumentException("Pixel array's length isn't the same as the resolution of the texture!"); }

            unsafe
            {
                int bpp = _bpp >> 3;
                fixed (byte* ptr = _data)
                {
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        SetColor(ptr, i * bpp, bpp, pixels[i], _format);
                    }
                }
            }
        }

        public void Resize(int width, int height, ResizeMode mode)
        {
            if (width == _width & height == _height) { return; }

            _mode = mode;

            _pW = (ushort)Maths.Clamp(width, 0, ushort.MaxValue);
            _pH = (ushort)Maths.Clamp(height, 0, ushort.MaxValue);

            _pendingMods |= TextureModification.Resize;
        }

        public IntPtr LockBits()
        {
            if (_handle.IsAllocated) { return _scan; }
            _handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
            _scan = _handle.AddrOfPinnedObject();
            return _scan;
        }

        public void UnlockBits()
        {
            if (!_handle.IsAllocated) { return; }
            _scan = IntPtr.Zero;
            _handle.Free();
        }

        public void Dispose()
        {
            if (_data == null) { return; }
            UnlockBits();
            _data = null;

            if (_palette != null)
            {
                _palette.Clear();
                _paletteLut.Clear();

                _palette = null;
                _paletteLut = null;
            }
        }

        public void SetPalette(IList<FastColor> colors)
        {
            if (HasPalette)
            {
                _palette.Clear();
                _paletteLut.Clear();
            }
            else
            {
                _palette = new List<FastColor>();
                _paletteLut = new Dictionary<FastColor, int>();
            }

            foreach (var c in colors)
            {
                if (_paletteLut.TryGetValue(c, out int i)) { continue; }
                _paletteLut.Add(c, _palette.Count);
                _palette.Add(c);
            }
        }

        public void SetPalette(IList<FastColor> colors, IDictionary<FastColor, int> paletteLut)
        {

        }

        public bool AddColor(FastColor color)
        {
            if (!HasPalette) { return false; }
            if (_paletteLut.TryGetValue(color, out var i)) { return false; }

            _paletteLut.Add(color, _palette.Count);
            _palette.Add(color);

            int newBpp = Maths.BytesNeeded(_palette.Count);
            int bpp = _bpp >> 3;
            if (newBpp != bpp)
            {
                RecalcualteColorIndices(_width * _height, bpp, newBpp, _palette, _paletteLut);
            }
            return true;
        }

        public void ClearPalette()
        {
            bool lockedBits = _handle.IsAllocated;

            int bpp = _bpp >> 3;
            int sizeCur = sizeCur = _width * _height * bpp;

            if (bpp > 1)
            {
                if (lockedBits)
                {
                    UnlockBits();
                }
                _bpp = 8;
                sizeCur = _width * _height;

                _data = new byte[sizeCur];

                if (lockedBits)
                {
                    LockBits();
                }
            }

            _palette.Clear();
            _paletteLut.Clear();

            _palette.Add(FastColor.black);
            _paletteLut.Add(FastColor.black, 0);

            unsafe
            {
                fixed (byte* ptr = _data)
                {
                    BufferUtils.Memset(ptr, 0, 0, sizeCur);
                }
            }
        }

        public void TrimPalette()
        {
            if (_palette != null && _format == ColorMode.Indexed)
            {
                unsafe
                {
                    Dictionary<FastColor, int> lut = new Dictionary<FastColor, int>();
                    List<FastColor> pal = new List<FastColor>();

                    int bpp = _bpp >> 3;
                    int reso = _width * _height;
                    fixed (byte* ptr = _data)
                    {
                        for (int i = 0; i < reso; i += bpp)
                        {
                            int v = 0;
                            for (int j = 0; j < bpp; j++) { v += (ptr[i + j] << (j << 3)); }
                            var c = _palette[v];
                            if (lut.TryGetValue(c, out int vI)) { continue; }

                            lut.Add(c, lut.Count);
                            if (lut.Count >= _palette.Count) { return; }
                        }
                    }

                    int nBpp = Maths.BytesNeeded(lut.Count);
                    if (nBpp != bpp)
                    {
                        RecalcualteColorIndices(reso, bpp, nBpp, pal, lut);
                        return;
                    }

                    fixed (byte* ptr = _data)
                    {
                        for (int i = 0; i < reso; i += bpp)
                        {
                            int v = 0;
                            for (int j = 0; j < bpp; j++) { v += (ptr[i + j] << (j << 3)); }
                            v = lut[_palette[v]];

                            for (int j = 0; j < bpp; j++) { ptr[i + j] = (byte)((v >> (j << 3)) & 0xFF); }
                        }
                    }

                    _paletteLut = lut;
                    _palette = pal;
                }
            }
        }

        public void SetResolution(int width, int height)
        {
            if(width == _width & height == _height) { return; }

            width = Maths.Clamp(width, 1, ushort.MaxValue);
            height = Maths.Clamp(height, 1, ushort.MaxValue);

            int newResolution = width * height * _bytesPerPix;
            int currentRes = _data.Length;

            if(newResolution == currentRes) { return; }

            bool alloc = _handle.IsAllocated;
            if (alloc)
            {
                UnlockBits();
            }
            Array.Resize(ref _data, newResolution);

            if(newResolution > currentRes)
            {
                int diff = newResolution - currentRes;
                unsafe
                {
                    fixed (byte* bb = _data)
                    {
                        switch (_format)
                        {
                            case ColorMode.Indexed:
                                if (!_paletteLut.TryGetValue(_fillColor, out var index))
                                {
                                    _paletteLut.Add(_fillColor, index = _palette.Count);
                                    _palette.Add(_fillColor);

                                    var bt = Maths.BytesNeeded(_palette.Count) << 3;
                                    if (bt != _bpp)
                                    {
                                        ReadjustToNewBpp((byte)bt, _format);
                                    }
                                }
                                BufferUtils.Memset(bb, index, _bytesPerPix, currentRes, diff);
                                break;
                            default:
                                FastColor* pixPtr = (FastColor*)bb;
                                pixPtr += currentRes;
                                while(diff-- > 0)
                                {
                                    *pixPtr++ = _fillColor;
                                }
                                break;
                        }
                    }
                }
            }

            if (alloc)
            {
                LockBits();
            }
        }

        private unsafe void RecalcualteColorIndices(int reso, int bpp, int nBpp, List<FastColor> pal, Dictionary<FastColor, int> lut)
        {
            int sizeCur = reso * bpp;
            int sizeNew = reso * nBpp;

            byte* temp = (byte*)Marshal.AllocHGlobal(sizeCur);

            //Copy current data to temp buffer
            fixed (byte* ptr = _data)
            {
                for (int i = 0; i < sizeCur; i++) { temp[i] = ptr[i]; }
            }

            //Free current & allocate new with the new size
            _data = new byte[sizeNew];

            if (_handle.IsAllocated)
            {
                _handle.Free();
                _handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
                _scan = GCHandle.ToIntPtr(_handle);
            }

            fixed (byte* ptr = _data)
            {
                for (int i = 0; i < reso; i += bpp)
                {
                    int v = 0;
                    for (int j = 0; j < bpp; j++) { v += (temp[i + j] << (j << 3)); }
                    v = lut[_palette[v]];

                    for (int j = 0; j < nBpp; j++) { ptr[i + j] = (byte)((v >> (j << 3)) & 0xFF); }
                }

            }
            _paletteLut = lut;
            _palette = pal;

            _bpp = (byte)(nBpp << 3);
            _scanSize = _width * nBpp;

            Marshal.FreeHGlobal((IntPtr)temp);
        }

        private unsafe bool ReadjustToNewBpp(byte newBpp, ColorMode newFormat)
        {
            bool isSame = _format == newFormat;
            if (isSame & (_format != ColorMode.Indexed | _bpp == newBpp)) { return false; }

            int reso = _width * _height;

            int bpp = (_bpp >> 3);
            int nBpp = (newBpp >> 3);

            int sizeCur = reso * bpp;
            int sizeNew = reso * nBpp;

            byte* temp = (byte*)Marshal.AllocHGlobal(sizeCur);
            fixed (byte* ptr = _data)
            {
                //Copy current data to temp buffer
                for (int i = 0; i < sizeCur; i++) { temp[i] = ptr[i]; }

                _data = new byte[sizeNew];
                if (_handle.IsAllocated)
                {
                    _handle.Free();
                    _handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
                    _scan = GCHandle.ToIntPtr(_handle);
                }
            }

            fixed (byte* ptr = _data)
            {
                //Readjust/Convert data
                switch (_format)
                {
                    case ColorMode.Indexed:
                        switch (newFormat)
                        {
                            case ColorMode.Indexed4:
                            case ColorMode.Indexed8:
                            case ColorMode.OneBit:
                            case ColorMode.Indexed:
                                int newMask = (1 << newBpp) - 1;
                                for (int i = 0; i < reso; i++)
                                {
                                    int iDP = i * bpp;
                                    int iD = i * nBpp;

                                    int oldV = 0;
                                    for (int j = 0; j < bpp; j++) { oldV += (temp[iDP + j] << (j << 3)); }
                                    oldV &= newMask;
                                    for (int j = 0; j < nBpp; j++) { ptr[iD + j] = (byte)((oldV >> (j << 3)) & 0xFF); }
                                }
                                break;

                            default:
                                for (int i = 0; i < reso; i++)
                                {
                                    int iDP = i * bpp;
                                    int iD = i * nBpp;

                                    var c = ColorExtensions.GetColor(temp + iDP, bpp, _format, _palette);
                                    SetColor(ptr, iD, nBpp, c, newFormat);
                                }
                                break;
                        }
                        break;
                    default:
                        for (int i = 0; i < reso; i++)
                        {
                            int iDP = i * bpp;
                            int iD = i * nBpp;

                            var c = ColorExtensions.GetColor(temp + iDP, bpp, _format, _palette);
                            SetColor(ptr, iD, nBpp, c, newFormat);
                        }
                        break;
                }
            }

            _format = newFormat;
            _bpp = newBpp;
            _scanSize = _width * nBpp;
            _bytesPerPix = (byte)(bpp >> 3);

            ValidatePalette();
            //Free temp
            Marshal.FreeHGlobal((IntPtr)temp);
            return true;
        }

        private void ValidatePalette()
        {
            switch (_format)
            {
                case ColorMode.Indexed:
                    if (_palette == null || _palette.Count < 1)
                    {
                        _palette = new List<FastColor>();
                        _palette.Add(FastColor.black);

                        _paletteLut = new Dictionary<FastColor, int>();
                        _paletteLut.Add(FastColor.black, 0);
                    }
                    break;
            }
        }
    }
}
