using Joonaxii.MathJX;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Joonaxii.Image.Texturing
{
    public class Texture : IDisposable
    {
        public int Width { get => _width; }
        public int Height { get => _height; }

        public IntPtr Scan { get => _scan; }
        private IntPtr _scan;

        public ColorMode Format
        {
            get => _format;
            set => SetFormat(value, true);
        }
        public int ScanSize { get => _scanSize; }
        public byte BytesPerPixel { get => _bpp; }

        private ColorMode _format;
        private int _scanSize;
        private byte _bpp;

        private ushort _width;
        private ushort _height;

        private ushort _pW;
        private ushort _pH;
        private ResizeMode _mode;

        private IntPtr _data;

        private TextureModification _pendingMods;
        private GCHandle _handle;

        private Dictionary<FastColor, int> _paletteLut = null;
        private List<FastColor> _palette = null;

        public Texture(int width, int height, ColorMode format) : this(width, height, format, FastColor.black) { }
        public Texture(int width, int height, ColorMode format, FastColor fillColor)
        {
            _width = (ushort)Maths.Clamp(width, 0, ushort.MaxValue);
            _height = (ushort)Maths.Clamp(height, 0, ushort.MaxValue);
            SetFormat(format, false);

            unsafe
            {
                int reso = _width * _height;
                _data = Marshal.AllocHGlobal(_width * _height * (_bpp >> 3));

                int bpp = _bpp >> 3;
                byte* ptr = (byte*)_data;
                for (int i = 0; i < reso; i++)
                {
                    int iD = i * bpp;
                    SetColor(ptr, iD, _bpp, fillColor, _format);
                }
            }

            _handle = default(GCHandle);
            _scan = IntPtr.Zero;
            _pendingMods = TextureModification.None;
        }

        public FastColor GetPixel(int x, int y) => GetPixel(y * _width + x);
        public FastColor GetPixel(int i)
        {
            if (i < 0 | i >= _width * _height) { throw new IndexOutOfRangeException("Index is out of range of the pixel array!"); }
            unsafe
            {
                int bpp = _bpp >> 3;
                byte* ptr = (byte*)_data;
                return GetColor(ptr, i * bpp, bpp, _format);
            }
        }

        public FastColor[] GetPixels()
        {
            if (_data == IntPtr.Zero) { return null; }

            FastColor[] pix = new FastColor[_width * _height];
            unsafe
            {
                int bpp = _bpp >> 3;
                byte* ptr = (byte*)_data;
                for (int i = 0; i < pix.Length; i++)
                {
                    int iD = i * bpp;
                    pix[i] = GetColor(ptr, iD, bpp, _format);
                }
            }
            return pix;
        }

        private unsafe FastColor GetColor(byte* ptr, int iD, int bpp, ColorMode format)
        {
            int ind;
            switch (format)
            {
                case ColorMode.Indexed:
                    ind = 0;
                    for (int j = 0; j < bpp; j++)
                    {
                        ind += (ptr[iD + j] << (j << 3));
                    }
                    return  _palette[ind];

                case ColorMode.RGB24: return new FastColor(ptr[iD], ptr[iD + 1], ptr[iD + 2]);
                case ColorMode.RGBA32: return new FastColor(ptr[iD], ptr[iD + 1], ptr[iD + 2], ptr[iD + 3]);

                case ColorMode.RGB565:
                    ind = 0;
                    for (int j = 0; j < bpp; j++)
                    {
                        ind += (ptr[iD + j] << (j << 3));
                    }
                    return ColorExtensions.FromRGB565(ind, false);
      
                case ColorMode.RGB555:
                    ind = 0;
                    for (int j = 0; j < bpp; j++)
                    {
                        ind += (ptr[iD + j] << (j << 3));
                    }
                    return ColorExtensions.FromRGB555(ind, false);
                case ColorMode.ARGB555:
                    ind = 0;
                    for (int j = 0; j < bpp; j++)
                    {
                        ind += (ptr[iD + j] << (j << 3));
                    }
                    return ColorExtensions.FromARGB555(ind, false);
                case ColorMode.Grayscale: return new FastColor(ptr[iD]);
                case ColorMode.GrayscaleAlpha: return new FastColor(ptr[iD], ptr[iD + 1]);
            }
            return FastColor.clear;
        }

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
                _scanSize = _width * (_bpp >> 3);

                ValidatePalette();
                return; 
            }
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
                byte* ptr = (byte*)_data;
                SetColor(ptr, i * bpp, bpp, color, _format);
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
                byte* ptr = (byte*)_data;
                for (int i = 0; i < pixels.Length; i++)
                {
                    SetColor(ptr, i * bpp, bpp, pixels[i], _format);
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

        public void Apply()
        {
            //TO-DO: Actually Implement resizing with IntPtrs

            //if(_pendingMods > 0)
            //{
            //    if (_pendingMods.HasFlag(TextureModification.Resize))
            //    {
            //        int res = _width * _height;
            //        if (_pixels == null)
            //        {
            //            _refPixels = false;
            //            _pixels = new FastColor[res];
            //            return;
            //        }
            //        else
            //        {
            //            if (_pixels.Length < res)
            //            {
            //                Array.Resize(ref _pixels, res);
            //            }
            //            Resize(ref _pixels, _width, _height, _pW, _pH, _mode);
            //        }
            //    }
            //}

            _pendingMods = TextureModification.None;
        }

        //public static bool Resize(ref FastColor[] pixels, int originalW, int originalH, int width, int height, ResizeMode mode)
        //{
        //    FastColor[] temp = new FastColor[originalW * originalH];
        //    Array.Copy(pixels, temp, temp.Length);

        //    Array.Resize(ref pixels, width * height);

        //    float wL = 1.0f / (width < 2 ? 1.0f : width - 1.0f);
        //    float hL = 1.0f / (height < 2 ? 1.0f : height - 1.0f);
        //    switch (mode)
        //    {
        //        default: return false;
        //        case ResizeMode.NearestNeighbor:
        //            for (int y = 0; y < height; y++)
        //            {
        //                int yP = y * width;
        //                int yOP = (int)(y * hL * originalH) * originalW;
        //                for (int x = 0; x < width; x++)
        //                {
        //                    int xOP = (int)(x * wL * originalW);
        //                    int iOP = yOP + xOP;
        //                    int i = yP + x;
        //                    pixels[i] = temp[iOP];
        //                }
        //            }
        //            return true;
        //    }
        //}

        public IntPtr LockBits()
        {
            if (_handle.IsAllocated) { return _scan; }
            _handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
            _scan = GCHandle.ToIntPtr(_handle);
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
            if (_data == IntPtr.Zero) { return; }
            UnlockBits();
            Marshal.FreeHGlobal(_data);

            if(_palette != null)
            {
                _palette.Clear();
                _paletteLut.Clear();

                _palette = null;
                _paletteLut = null;
            }
        }

        public void TrimPalette()
        {          
            if(_palette != null && _format == ColorMode.Indexed)
            {
                unsafe
                {
                    Dictionary<FastColor, int> lut = new Dictionary<FastColor, int>();
                    List<FastColor> pal = new List<FastColor>();
                 
                    int bpp = _bpp >> 3;
                    byte* ptr = (byte*)_data;
                    int reso = _width * _height;
                    for (int i = 0; i < reso; i+=bpp)
                    {
                        int v = 0;
                        for (int j = 0; j < bpp; j++) { v += (ptr[i + j] << (j << 3)); }
                        var c = _palette[v];
                        if (lut.TryGetValue(c, out int vI)) { continue; }

                        lut.Add(c, lut.Count);      
                        if(lut.Count >= _palette.Count) { return; }
                    }

                    int nBpp = Maths.BytesNeeded(lut.Count); 
                    if(nBpp != bpp)
                    {
                        int sizeCur = reso * bpp;
                        int sizeNew = reso * nBpp;

                        byte* temp = (byte*)Marshal.AllocHGlobal(sizeCur);
             
                        //Copy current data to temp buffer
                        for (int i = 0; i < sizeCur; i++) { temp[i] = ptr[i]; }

                        //Free current & allocate new with the new size
                        Marshal.FreeHGlobal(_data);
                        ptr = (byte*)(_data = Marshal.AllocHGlobal(sizeNew));

                        if (_handle.IsAllocated)
                        {
                            _handle.Free();
                            _handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
                            _scan = GCHandle.ToIntPtr(_handle);
                        }

                        for (int i = 0; i < reso; i += bpp)
                        {
                            int v = 0;
                            for (int j = 0; j < bpp; j++) { v += (temp[i + j] << (j << 3)); }
                            v = lut[_palette[v]];

                            for (int j = 0; j < nBpp; j++) { ptr[i + j] = (byte)((v >> (j << 3)) & 0xFF); }
                        }

                        _paletteLut = lut;
                        _palette = pal;

                        _bpp = (byte)(nBpp << 3);
                        _scanSize = _width * nBpp;

                        Marshal.FreeHGlobal((IntPtr)temp);
                        return;
                    }

                    for (int i = 0; i < reso; i += bpp)
                    {
                        int v = 0;
                        for (int j = 0; j < bpp; j++) { v += (ptr[i + j] << (j << 3)); }
                        v = lut[_palette[v]];

                        for (int j = 0; j < bpp; j++) { ptr[i + j] = (byte)((v >> (j << 3)) & 0xFF); }
                    }

                    _paletteLut = lut;
                    _palette = pal;
                }
            }
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
            byte* ptr  = (byte*)_data;

            //Copy current data to temp buffer
            for (int i = 0; i < sizeCur; i++) { temp[i] = ptr[i]; }

            //Free current & allocate new with the new size
            Marshal.FreeHGlobal(_data);
            ptr = (byte*)(_data = Marshal.AllocHGlobal(sizeNew));

            if (_handle.IsAllocated)
            {
                _handle.Free();
                _handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
                _scan = GCHandle.ToIntPtr(_handle);
            }

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

                                var c = GetColor(temp, iDP, bpp, _format);
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

                        var c = GetColor(temp, iDP, bpp, _format);
                        SetColor(ptr, iD, nBpp, c, newFormat);
                    }
                    break;
            }

            _format = newFormat;
            _bpp = newBpp;
            _scanSize = _width * nBpp;

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
                    if(_palette == null | _palette.Count < 1)
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
