using Joonaxii.Data;
using Joonaxii.IO;
using Joonaxii.MathJX;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Joonaxii.Image.Codecs.PNG
{
    public class PNGEncoder : ImageEncoderBase
    {
        private PNGFlags _flags;
        private PNGFilterMethod _overrideFilter;

        public PNGEncoder(int width, int height, byte bPP) : base(width, height, bPP)
        {
            _flags = PNGFlags.None;
        }

        public PNGEncoder(int width, int height, ColorMode mode) : base(width, height, mode)
        {
            _flags = PNGFlags.None;
        }

        public void SetOverrideFilter(PNGFilterMethod filter) => _overrideFilter = filter;

        public void SetFlags(PNGFlags flags)
        {
            _flags = flags;
        }

        public override ImageEncodeResult Encode(Stream stream, bool leaveStreamOpen)
        {
            using (BinaryWriter bw = new BinaryWriter(stream, Encoding.UTF8, leaveStreamOpen))
            {
                bw.Write(0x474E5089);
                bw.Write(0x0A1A0A0D);

                GeneratePalette();

                bool hasPalette =
                    _flags.HasFlag(PNGFlags.ForcePalette) & (_flags.HasFlag(PNGFlags.AllowBigIndices) | _palette.Count <= 256) |
                    (!_flags.HasFlag(PNGFlags.ForceRGB) & _palette.Count <= 256);
                PNGColorType pngType = hasPalette ? PNGColorType.PALETTE_IDX : _hasAlpha ? PNGColorType.RGB_ALPHA : PNGColorType.RGB;

                System.Diagnostics.Debug.Print($"{_colorMode}, {hasPalette}, {_flags}, {_bpp}");
                byte bps = (byte)(_bpp / (_hasAlpha ? 4 : 3));
                if (!hasPalette)
                {
                    switch (_colorMode)
                    {
                        case ColorMode.ARGB555:
                        case ColorMode.RGBA32:
                            SetColorMode(_hasAlpha ? ColorMode.RGBA32 : ColorMode.RGB24);
                            pngType = PNGColorType.RGB_ALPHA;
                            bps = 8;
                            break;
                        case ColorMode.Grayscale:
                        case ColorMode.GrayscaleAlpha:
                            pngType = _colorMode == ColorMode.GrayscaleAlpha ? PNGColorType.GRAY_ALPHA : PNGColorType.GRAYSCALE;
                            bps = _bpp;
                            break;

                        case ColorMode.RGB555:
                        case ColorMode.RGB565:
                            SetColorMode(ColorMode.RGB24);
                            bps = 8;
                            break;

                        case ColorMode.Indexed4:
                        case ColorMode.Indexed8:
                        case ColorMode.Indexed:
                            if (_flags.HasFlag(PNGFlags.ForceRGB))
                            {
                                pngType = _hasAlpha ? PNGColorType.RGB_ALPHA : PNGColorType.RGB;
                                SetColorMode(_hasAlpha ?  ColorMode.RGBA32 : ColorMode.RGB24);
                                bps = 8;
                                break;
                            }

                            pngType = PNGColorType.PALETTE_IDX;
                            hasPalette = true;
                            bps = _bpp;
                            break;
                    }
                }
                else
                {
                    int requiredBits = IOExtensions.BytesNeeded(_palette.Count) << 3;
                    int max = (_flags.HasFlag(PNGFlags.AllowBigIndices) ? 32 : 8);
                    requiredBits = requiredBits < 8 ? 8 : requiredBits > max ? max : requiredBits;

                    _bpp = (byte)requiredBits;
                    bps = _bpp;
                    _colorMode = ColorMode.Indexed;
                }

                IHDRChunk hdr = new IHDRChunk(_width, _height, bps, pngType);
                hdr.Write(bw);

                System.Diagnostics.Debug.Print(hdr.ToMinString());

                int posInBuf = 0;
                byte[] finalBuf = null;
                if (hasPalette)
                {
                    PLTEChunk plt = new PLTEChunk(_palette);
                    plt.Write(bw);
                    System.Diagnostics.Debug.Print(plt.ToMinString());

                    if (_hasAlpha)
                    {
                        tRNSChunk alph = new tRNSChunk(_palette);
                        alph.Write(bw);

                        System.Diagnostics.Debug.Print(alph.ToMinString());
                    }

                    int bytesPP = _bpp >> 3;
                    finalBuf = new byte[_height + (_width * _height * bytesPP)];
                    System.Diagnostics.Debug.Print($"Bytes Per Pixel: {bytesPP} // {finalBuf.Length}");
                    for (int y = 0; y < _height; y++)
                    {
                        int yP = y * _width;

                        finalBuf[posInBuf++] = 0;
                        for (int x = 0; x < _width; x++)
                        {
                            int i = yP + x;
                            int index = _paletteLut[_pixels[i]].index;

                            IOExtensions.WriteToByteArray(finalBuf, posInBuf, index, bytesPP, false);
                            posInBuf += bytesPP;
                        }
                    }
                }
                else
                {                    // return ImageEncodeResult.NotSupported;
#if DEBUG
                    Dictionary<PNGFilterMethod, int> filterCounts = new Dictionary<PNGFilterMethod, int>();
                    int totalFilters = 0;
#endif

                    posInBuf = 0;
                    byte[] dataBuf = _pixels.ToBytes(PixelByteOrder.RGBA, false, _width, _height, _colorMode);

                    int bytesPerPix = (_bpp >> 3);
                    int wW = _width * bytesPerPix;

                    int fScn = (_height - 1) * wW;

                    byte[] writeBuf = new byte[wW];
                    byte[] lowestSoFar = new byte[wW];

                    finalBuf = new byte[dataBuf.Length + _height];

                    int lowestVariation;
                    PNGFilterMethod lowFilter = PNGFilterMethod.None;

                    for (int y = 0; y < _height; y++)
                    {
                        int scanline = y * wW;
                        lowFilter = _flags.HasFlag(PNGFlags.OverrideFilter) ? _overrideFilter : PNGFilterMethod.None;
                        ApplyFilter(lowFilter, y, scanline, bytesPerPix, dataBuf, lowestSoFar, wW);

                        if (!_flags.HasFlag(PNGFlags.OverrideFilter)) 
                        {
                            lowestVariation = GetByteVariation(lowestSoFar, 0, lowestSoFar.Length);
                            ApplyFilter(ref lowestVariation, ref lowFilter, PNGFilterMethod.Sub,     y, scanline,bytesPerPix, dataBuf, writeBuf, lowestSoFar, wW);
                            ApplyFilter(ref lowestVariation, ref lowFilter, PNGFilterMethod.Up,      y, scanline,bytesPerPix, dataBuf, writeBuf, lowestSoFar, wW);
                            ApplyFilter(ref lowestVariation, ref lowFilter, PNGFilterMethod.Average, y, scanline,bytesPerPix, dataBuf, writeBuf, lowestSoFar, wW);
                            ApplyFilter(ref lowestVariation, ref lowFilter, PNGFilterMethod.Paeth,   y, scanline,bytesPerPix, dataBuf, writeBuf, lowestSoFar, wW);
                        }

                        finalBuf[posInBuf++] = (byte)lowFilter;
                        Buffer.BlockCopy(lowestSoFar, 0, finalBuf, posInBuf, wW);
                        posInBuf += wW;

                        if(y == _height -1)
                        {
                            string str = "";
                            for (int i = 0; i < wW; i++)
                            {
                                str += $"0x{Convert.ToString(finalBuf[posInBuf - wW + i], 16).PadLeft(2, '0')} ";
                            }
                            System.Diagnostics.Debug.Print(str);
                        }
#if DEBUG
                        totalFilters++;
                        if (filterCounts.ContainsKey(lowFilter))
                        {
                            filterCounts[lowFilter]++;
                            continue;
                        }
                        filterCounts.Add(lowFilter, 1);
#endif
                    }

#if DEBUG
                    foreach (var filters in filterCounts)
                    {
                        System.Diagnostics.Debug.Print($"Filter: {filters.Key}, {filters.Value} ({((filters.Value / (float)totalFilters) * 100.0f).ToString("F2")}%)");
                    }
                    System.Diagnostics.Debug.Print(new string('=', 32));
                    System.Diagnostics.Debug.Print("");
#endif
                }

                {
                    IDATChunk idat = new IDATChunk();
                    const int CHUNK_MAX_SIZE = 8192;
                    finalBuf = GetCompressedData(finalBuf, hasPalette && _palette.Count <= 256 ? CompressionLevel.NoCompression : CompressionLevel.Fastest);
                    byte[] chunkBUF = new byte[CHUNK_MAX_SIZE];

                    using(MemoryStream ms = new MemoryStream(finalBuf))
                    {
                        while (ms.Position < ms.Length)
                        {
                            int len = ms.Read(chunkBUF, 0, CHUNK_MAX_SIZE);

                            idat.SetData(chunkBUF, len);
                            idat.Write(bw);
                        }
                    }
                }

                //IEND Chunk
                RawChunk iend = new RawChunk(0, PNGChunkType.IEND, new byte[0], 0);
                iend.crc = iend.GetCrc();
                iend.Write(bw);
            }
            return ImageEncodeResult.Success;
        }

        private void ApplyFilter(PNGFilterMethod filter, int y, int scan, int bytesPerPix, byte[] data, byte[] target, int ww)
        {
            switch (filter)
            {
                default: Buffer.BlockCopy(data, scan, target, 0, target.Length); break;
                case PNGFilterMethod.Sub:
                    for (int x = 0; x < ww; x++)
                    {
                        SubFilter(scan, x, bytesPerPix, data, target);
                    }
                    break;
                case PNGFilterMethod.Up:
                    for (int x = 0; x < ww; x++)
                    {
                        UpFilter(x, y, data, target);
                    }
                    break;
                case PNGFilterMethod.Average:
                    for (int x = 0; x < ww; x++)
                    {
                        AvgFilter(x, y, bytesPerPix, data, target);
                    }
                    break;
                case PNGFilterMethod.Paeth:
                    for (int x = 0; x < ww; x++)
                    {
                        PaethFilter(x, y,bytesPerPix, data, target);
                    }
                    break;
            }
        }

        private void ApplyFilter(ref int variation, ref PNGFilterMethod curFilter, PNGFilterMethod filter, int y, int scan, int bytesPerPix, byte[] data, byte[] target, byte[] curLowest, int ww)
        {
            ApplyFilter(filter, y, scan, bytesPerPix, data, target, ww);
            int vari = GetByteVariation(target, 0, target.Length);

            if(vari < variation)
            {
                variation = vari;
                curFilter = filter;
                Buffer.BlockCopy(target, 0, curLowest, 0, curLowest.Length);
            }
        }

        private byte[] GetCompressedData(byte[] original, CompressionLevel cmpLevel)
        {
            byte v = 1;
            switch (cmpLevel)
            {
                case CompressionLevel.NoCompression:
                    v = 0x01;
                    break;
                case CompressionLevel.Fastest:
                    v = 0x9C;
                    break;
                case CompressionLevel.Optimal:
                    v = 0xDA;
                    break;
            }
            //uint crc = PNGChunk.CalcualteCrc(original, 0, original.Length);
            
            using(MemoryStream ms = new MemoryStream())
            using(DeflateStream def = new DeflateStream(ms, cmpLevel, true))
            using(MemoryStream msA = new MemoryStream())
            {
                def.Write(original, 0, original.Length);
                def.Close();

                ms.Seek(0, SeekOrigin.Begin);
                ms.CopyTo(msA);

                byte[] data = new byte[msA.Length + 2];

                data[0] = 0x78;
                data[1] = v;

                Buffer.BlockCopy(msA.ToArray(), 0, data, 2, (int)msA.Length);

                //int start = data.Length - 4;
                //int ii = 24;
                //for (int i = start; i < start + 4; i++)
                //{
                //    data[i] = (byte)((crc >> ii) & 0xFF);
                //    ii -= 8;
                //}
                return data;
            }
        }

        private Dictionary<FastColor, ColorContainer> _paletteLut = null;
        private List<ColorContainer> _palette = null;
        private void GeneratePalette()
        {
            _hasAlpha = _flags.HasFlag(PNGFlags.ForceAlpha);
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
                case ColorMode.Indexed8:
                    _colorMode = ColorMode.Indexed;
                    _bpp = 8;
                    break;
            }
        }

        //The data array is the original bytes, targetData is the current scanline to write
        //so it's length is the width of the image in bytes
        //The scanline parameter is current Y pos times the width of the image in bytes.

        private byte Sub(int x, int scanline, int bytesPerPix, byte[] data)   =>      x < bytesPerPix ? (byte)0 : data[scanline - bytesPerPix];
        private byte Prior(int x, int y, int w, byte[] data) => (x < 0 | y < 1) ? (byte)0 : data[(y - 1)  * w + x];

        private void SubFilter(int scanline, int x, int bytesPerPix, byte[] data, byte[] targetData)
        {
            int i = scanline + x;
            byte valA = data[i];
            byte valB = Sub(x, i, bytesPerPix, data);

            targetData[x] = (byte)((valA - valB) % 256);
        }

        private void UpFilter(int x, int y, byte[] data, byte[] targetData)
        {
            int i = y * targetData.Length + x;
            byte valA = data[i];
            byte valB = Prior(x, y, targetData.Length, data);

            targetData[x] = (byte)((valA - valB) % 256);
        }

        private void AvgFilter(int x, int y, int bytesPerPix, byte[] data, byte[] targetData)
        {
            int i = y * targetData.Length + x;
            byte valA = data[i];
            byte valB = Sub(x, i,bytesPerPix, data);
            byte valC = Prior(x, y, targetData.Length, data);

            targetData[x] = (byte)((valA - ((valB + valC) >> 1)) % 256);
        }

        private void PaethFilter(int x, int y, int bytesPerPix, byte[] data, byte[] targetData)
        {
            int i = y * targetData.Length + x;

            byte o = data[i];
            byte a = Sub(x, i, bytesPerPix, data);
            byte b = Prior(x, y, targetData.Length, data);
            byte c = Prior(x - bytesPerPix, y, targetData.Length, data);

            int p = a + b - c;

            int pA = Math.Abs(p - a);
            int pB = Math.Abs(p - b);
            int pC = Math.Abs(p - c);

            if (pA <= pB & pA <= pC)
            {
                targetData[x] = (byte)((o - a) % 256);
                return;
            }
            targetData[x] = (byte)((o - (pB <= pC ? b : c)) % 256);
        }

        private int GetByteVariation(byte[] bytes, int start, int len)
        {
            byte v = bytes[start];
            int variation = 0;
            for (int i = start + 1; i < start + len; i++)
            {
                var b = bytes[i];
                if (b != v)
                {
                    v = b;
                    variation++;
                }
            }
            return variation;
        }
    }
}
