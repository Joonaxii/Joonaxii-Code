﻿using Joonaxii.Data;
using Joonaxii.Data.Coding;
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
        public PNGFlags PNGFlags
        {
            get => _pngFlags;
            set
            {
                _pngFlags = value;
                _useBrokenSubFilter = _pngFlags.HasFlag(PNGFlags.UseBrokenSubFilter);
            }
        }

        private PNGFlags _pngFlags;
        private PNGFilterMethod _overrideFilter;

        private unsafe byte* _compressBufferPtr;
        private byte[] _compressBuffer;

        public PNGEncoder(int width, int height, byte bPP) : base(width, height, bPP)
        {
            PNGFlags = PNGFlags.None;
        }

        public PNGEncoder(int width, int height, ColorMode mode) : base(width, height, mode)
        {
            PNGFlags = PNGFlags.None;
        }

        public void SetOverrideFilter(PNGFilterMethod filter) => _overrideFilter = filter;

        private bool _useBrokenSubFilter;
        public override ImageEncodeResult Encode(Stream stream, bool leaveStreamOpen)
        {
            using (BinaryWriter bw = new BinaryWriter(stream, Encoding.UTF8, leaveStreamOpen))
            {
                bw.Write(0x474E5089);
                bw.Write(0x0A1A0A0D);

                GeneratePalette(_flags.HasFlag(ImageDecoderFlags.ForceRegenPalette));

                bool hasPalette =
                    _flags.HasFlag(ImageDecoderFlags.ForcePalette) & (_flags.HasFlag(ImageDecoderFlags.AllowBigIndices) | _palette.Count <= 256) |
                    (!_flags.HasFlag(ImageDecoderFlags.ForceRGB) & _palette.Count <= 256);
                PNGColorType pngType = hasPalette ? PNGColorType.PALETTE_IDX : _hasAlpha ? PNGColorType.RGB_ALPHA : PNGColorType.RGB;

                System.Diagnostics.Debug.Print($"{_colorMode}, {hasPalette}, {_pngFlags}, {_bpp}");
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
                            if (_flags.HasFlag(ImageDecoderFlags.ForceRGB))
                            {
                                pngType = _hasAlpha ? PNGColorType.RGB_ALPHA : PNGColorType.RGB;
                                SetColorMode(_hasAlpha ? ColorMode.RGBA32 : ColorMode.RGB24);
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
                    int requiredBits = Maths.BytesNeeded(_palette.Count) << 3;
                    int max = (_flags.HasFlag(ImageDecoderFlags.AllowBigIndices) ? 32 : 8);
                    requiredBits = requiredBits < 8 ? 8 : requiredBits > max ? max : requiredBits;

                    _bpp = (byte)requiredBits;
                    bps = _bpp;
                    _colorMode = ColorMode.Indexed;
                }
                IHDRChunk.Write(bw, _width, _height, bps, pngType);

                int posInBuf = 0;
                byte[] finalBuf = null;

                byte[] dataBuf = null;
                bool forceFilter = _pngFlags.HasFlag(PNGFlags.ForceFilter);
                if (hasPalette)
                {
                    PLTEChunk.Write(bw, _palette);
                    if (_hasAlpha) { tRNSChunk.Write(bw, _palette); }

                    if (!forceFilter)
                    {
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
                    {
                        int bytesPP = _bpp >> 3;
                        dataBuf = new byte[(_width * _height * bytesPP)];

                        posInBuf = 0;
                        for (int i = 0; i < _pixels.Length; i++)
                        {
                            int index = _paletteLut[_pixels[i]].index;
                            IOExtensions.WriteToByteArray(dataBuf, posInBuf, index, bytesPP, false);
                            posInBuf += bytesPP;
                        }
                    }
                }

                if (!hasPalette | forceFilter)
                {
#if DEBUG
                    Dictionary<PNGFilterMethod, int> filterCounts = new Dictionary<PNGFilterMethod, int>();
                    int totalFilters = 0;
#endif

                    posInBuf = 0;
                    dataBuf = dataBuf == null ? _pixels.ToBytes(PixelByteOrder.RGBA, false, _width, _height, _colorMode) : dataBuf;

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
                        lowFilter = _pngFlags.HasFlag(PNGFlags.OverrideFilter) ? _overrideFilter : PNGFilterMethod.None;
                        ApplyFilter(lowFilter, y, scanline, bytesPerPix, dataBuf, lowestSoFar, wW);

                        if (!_pngFlags.HasFlag(PNGFlags.OverrideFilter))
                        {
                            lowestVariation = GetByteVariation(lowestSoFar, 0, lowestSoFar.Length);
                            ApplyFilter(ref lowestVariation, ref lowFilter, PNGFilterMethod.Sub, y, scanline, bytesPerPix, dataBuf, writeBuf, lowestSoFar, wW);
                            ApplyFilter(ref lowestVariation, ref lowFilter, PNGFilterMethod.Up, y, scanline, bytesPerPix, dataBuf, writeBuf, lowestSoFar, wW);
                            ApplyFilter(ref lowestVariation, ref lowFilter, PNGFilterMethod.Average, y, scanline, bytesPerPix, dataBuf, writeBuf, lowestSoFar, wW);
                            ApplyFilter(ref lowestVariation, ref lowFilter, PNGFilterMethod.Paeth, y, scanline, bytesPerPix, dataBuf, writeBuf, lowestSoFar, wW);
                        }

                        finalBuf[posInBuf++] = (byte)lowFilter;
                        Buffer.BlockCopy(lowestSoFar, 0, finalBuf, posInBuf, wW);
                        posInBuf += wW;
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
                    const int CHUNK_MAX_SIZE = 8192;
                    GetCompressedData(bw, finalBuf, CompressionLevel.Optimal);

                    //int finBufPos = 0;
                    //using (MemoryStream ms = new MemoryStream(finalBuf))
                    //{
                    //    while (finBufPos < finalBuf.Length)
                    //    {
                    //        int len = Math.Min(CHUNK_MAX_SIZE, finalBuf.Length - finBufPos);
                    //        PNGChunk.Write(bw, PNGChunkType.IDAT, finalBuf, finBufPos, len);
                    //        finBufPos += len;
                    //    }
                    //}
                }

                //IEND Chunk
                PNGChunk.Write(bw, PNGChunkType.IEND, new byte[0], 0, 0);
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
                        PaethFilter(x, y, bytesPerPix, data, target);
                    }
                    break;
            }
        }

        private void ApplyFilter(ref int variation, ref PNGFilterMethod curFilter, PNGFilterMethod filter, int y, int scan, int bytesPerPix, byte[] data, byte[] target, byte[] curLowest, int ww)
        {
            ApplyFilter(filter, y, scan, bytesPerPix, data, target, ww);
            int vari = GetByteVariation(target, 0, target.Length);

            if (vari < variation)
            {
                variation = vari;
                curFilter = filter;
                Buffer.BlockCopy(target, 0, curLowest, 0, curLowest.Length);
            }
        }
        
        private const int CHUNK_MAX_SIZE = 8192;
        private const int CHUNK_DATA_SIZE = 8192 - 4;

        private unsafe void WriteCompressedData(BinaryWriter bw, MemoryStream defStream, DeflateStream compStream, byte* data, byte* chunkBuf, int length, bool first, int crc)
        {
            //Adler32Checksum.RunProgressive(data, length, ref sum1, ref sum2);
            byte* chunkPtr = chunkBuf;

            IOExtensions.WriteToByteArray(chunkPtr, 0, (int)PNGChunkType.IDAT, 4, true);
            chunkPtr += 4;

            if (first)
            {
                *chunkPtr++ = 0x78;
                *chunkPtr++ = 0xDA;
            }

            //IOExtensions.WriteToByteArray(chunkPtr, 0, crc, 4, true);
            if (length < CHUNK_DATA_SIZE)
            {
                int off = CHUNK_DATA_SIZE - length;

                if (off >= 4)
                {
                    IOExtensions.WriteToByteArray(chunkPtr, 0, crc, 4, true);
                    PNGChunk.Write(bw, chunkBuf, length + 4);
                    return;
                }

                int lo = 4 - off;
                IOExtensions.WriteToByteArray(chunkPtr, 0, crc, lo, true);
                PNGChunk.Write(bw, chunkBuf, CHUNK_MAX_SIZE);
            
                chunkPtr = chunkBuf;
                IOExtensions.WriteToByteArray(chunkPtr, 0, (int)PNGChunkType.IDAT, 4, true);
                chunkPtr += 4;

                IOExtensions.WriteToByteArray(chunkPtr, 4, crc, off, true);
                PNGChunk.Write(bw, chunkBuf, off);
                return;
            }
        }

        private unsafe void GetCompressedData(BinaryWriter bw, byte[] original, CompressionLevel cmpLevel)
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

            byte* chunk = stackalloc byte[CHUNK_MAX_SIZE];
            byte* dataPtr = chunk + 4;
            byte[] chunkRead = new byte[CHUNK_MAX_SIZE];

            int pos = 0;

            fixed (byte* orgBuf = chunkRead)
            {
                byte* ogBuf = orgBuf;
                using (MemoryStream ms = new MemoryStream())
                using (DeflateStream def = new DeflateStream(ms, cmpLevel, true))
                using (BufferedStream str = new BufferedStream(def, 8192))
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    long posPrev = ms.Position;

                    int len = (original.Length - pos) - 2;
                    len = len > CHUNK_DATA_SIZE ? CHUNK_DATA_SIZE : len;
                    str.Write(chunkRead, pos, len);
                    pos += len;
                    long posNow = ms.Position;

                    ms.Seek(posPrev, SeekOrigin.Begin);


                   // int crc = Adler32Checksum.Calculate(original);

                    bool first = true;
                    bool last = false;
                    while (len > 0)
                    {
                        byte* ptr = chunk;
                        IOExtensions.WriteToByteArray(ptr, 0, (int)PNGChunkType.IDAT, 4, true);

                        if (first)
                        {
                            *dataPtr++ = 0x78;
                            *dataPtr++ = v;
                            first = false;
                        }

                        BufferUtils.Memcpy(ogBuf, dataPtr, len);
                        ogBuf += len;

                        PNGChunk.Write(bw, chunk, len);

                        len = (original.Length - pos) - 2;
                        len = len > CHUNK_DATA_SIZE ? CHUNK_DATA_SIZE : len;
                        str.Write(chunkRead, pos, len);
                        last = len < CHUNK_DATA_SIZE;
                        pos += len;

                        if (last)
                        {
                            int off = CHUNK_DATA_SIZE - (len + 4);
                            if (off < 0)
                            {
                                off = -off;
                                dataPtr += len;
                            //    IOExtensions.WriteToByteArray(dataPtr, 0, crc, off, true);
                                len = CHUNK_DATA_SIZE;
                                PNGChunk.Write(bw, chunk, len);

                              //  crc >>= (off << 3);
                                len = 4 - off;
                                dataPtr = chunk;

                                IOExtensions.WriteToByteArray(ptr, 0, (int)PNGChunkType.IDAT, 4, true);
                                //IOExtensions.WriteToByteArray(dataPtr, 0, crc, len, true);
                                PNGChunk.Write(bw, chunk, len);
                                break;
                            }

                            dataPtr += len;
                           // IOExtensions.WriteToByteArray(dataPtr, 0, crc, 4, true);
                            len += 4;
                            PNGChunk.Write(bw, chunk, len + 4);
                            break;
                        }
                        PNGChunk.Write(bw, chunk, len);
                    }
                }
            }
        }

        protected override void ValidateAlpha(bool hasAlpha)
        {
            hasAlpha = _flags.HasFlag(ImageDecoderFlags.ForceAlpha) || (hasAlpha & !_flags.HasFlag(ImageDecoderFlags.ForceNoAlpha));
            base.ValidateAlpha(hasAlpha);
        }

        public override void ValidateFormat()
        {
            base.ValidateFormat();
            switch (_colorMode)
            {
                case ColorMode.RGBA32:
                    if (_flags.HasFlag(ImageDecoderFlags.ForceNoAlpha))
                    {
                        _colorMode = ColorMode.RGB24;
                        _bpp = 24;
                        break;
                    }
                    break;
                case ColorMode.ARGB555:
                    if (_flags.HasFlag(ImageDecoderFlags.ForceNoAlpha))
                    {
                        _colorMode = ColorMode.RGB24;
                        _bpp = 24;
                        break;
                    }
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

        private byte Sub(int x, int scanline, int bytesPerPix, byte[] data) => x < bytesPerPix ? (byte)0 : data[scanline - bytesPerPix];
        private byte Prior(int x, int y, int w, byte[] data) => (x < 0 | y < 1) ? (byte)0 : data[(y - 1) * w + x];

        private void SubFilter(int scanline, int x, int bytesPerPix, byte[] data, byte[] targetData)
        {
            int i = scanline + x;
            byte valA = data[i];
            byte valB = Sub(x, i, _useBrokenSubFilter ? 1 : bytesPerPix, data);

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
            byte valB = Sub(x, i, _useBrokenSubFilter ? 1 : bytesPerPix, data);
            byte valC = Prior(x, y, targetData.Length, data);

            targetData[x] = (byte)((valA - ((valB + valC) >> 1)) % 256);
        }

        private void PaethFilter(int x, int y, int bytesPerPix, byte[] data, byte[] targetData)
        {
            int i = y * targetData.Length + x;

            byte o = data[i];
            byte a = Sub(x, i, _useBrokenSubFilter ? 1 : bytesPerPix, data);
            byte b = Prior(x, y, targetData.Length, data);
            byte c = Prior(_useBrokenSubFilter ? x - 1 : x - bytesPerPix, y, targetData.Length, data);

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
