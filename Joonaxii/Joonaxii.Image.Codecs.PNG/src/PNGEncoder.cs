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

        public PNGEncoder(int width, int height, byte bPP) : base(width, height, bPP)
        {
            _flags = PNGFlags.None;
        }

        public PNGEncoder(int width, int height, ColorMode mode) : base(width, height, mode)
        {
            _flags = PNGFlags.None;
        }

        public void SetFlags(PNGFlags flags)
        {
            _flags = flags;
        }

        public override ImageEncodeResult Encode(Stream stream, bool leaveStreamOpen)
        {
            using (BinaryWriter bw = new BinaryWriter(stream, Encoding.UTF8, leaveStreamOpen))
            {
                if (HeaderManager.TryGetHeader(HeaderType.PNG, out var header))
                {
                    header.WriteHeader(bw);
                }
                else
                {
                    bw.Write(0x89504E47);
                    bw.Write(0x0D0A1A0A);
                }
                GeneratePalette();

                bool hasPalette =
                    _flags.HasFlag(PNGFlags.ForcePalette) & (_flags.HasFlag(PNGFlags.AllowBigIndices) | _palette.Count <= 256) |
                    (!_flags.HasFlag(PNGFlags.ForceRGB) & _palette.Count <= 256);
                PNGColorType pngType = hasPalette ? PNGColorType.PALETTE_IDX : PNGColorType.RGB;

                if (!hasPalette)
                {
                    switch (_colorMode)
                    {
                        case ColorMode.ARGB555:
                        case ColorMode.RGBA32:
                            pngType = PNGColorType.RGB_ALPHA;
                            break;
                        case ColorMode.Grayscale:
                        case ColorMode.GrayscaleAlpha:
                            pngType = _colorMode == ColorMode.GrayscaleAlpha ? PNGColorType.GRAY_ALPHA : PNGColorType.GRAYSCALE;
                            break;
                        case ColorMode.Indexed4:
                        case ColorMode.Indexed8:
                            pngType = PNGColorType.PALETTE_IDX;
                            hasPalette = true;
                            break;
                    }
                }
                else
                {
                    int requiredBits = IOExtensions.BitsNeeded(Maths.NextPowerOf2(_palette.Count) - 1);
                    int max = (_flags.HasFlag(PNGFlags.AllowBigIndices) ? 32 : 8);
                    requiredBits = requiredBits < 8 ? 8 : requiredBits > max ? max : requiredBits;

                    _bpp = (byte)requiredBits;
                    _colorMode = ColorMode.Indexed;
                }

                IHDRChunk hdr = new IHDRChunk(_width, _height, _bpp, pngType);
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
                    for (int y = 0; y < _height; y++)
                    {
                        int yP = y * _width;

                        finalBuf[posInBuf++] = 0;
                        for (int x = 0; x < _width; x++)
                        {
                            int i = yP + x;
                            int index = _paletteLut[_pixels[i]].index;

                            IOExtensions.WriteToByteArray(finalBuf, posInBuf, index, bytesPP, true);
                            posInBuf += bytesPP;
                        }
                    }
                }
                else
                {
                    return ImageEncodeResult.NotSupported;

                    byte[] dataBuf = _pixels.ToBytes(PixelByteOrder.RGBA, false, _width, _height, _colorMode);
                    byte[] scanLineBuf = new byte[_width * (_bpp >> 3)];
                    byte[] lowestSoFar = new byte[scanLineBuf.Length];
                    byte[] writeBuf = new byte[scanLineBuf.Length];

                    finalBuf = new byte[dataBuf.Length + _height];

                    int lowestVariation;

                    for (int y = 0; y < _height; y++)
                    {
                        int yP = y * _width;

                        Buffer.BlockCopy(dataBuf, yP, scanLineBuf, 0, scanLineBuf.Length);
                        Buffer.BlockCopy(dataBuf, yP, writeBuf, 0, writeBuf.Length);
                        Buffer.BlockCopy(writeBuf, 0, lowestSoFar, 0, lowestSoFar.Length);

                        lowestVariation = GetByteVariation(writeBuf, 0, writeBuf.Length);

                    }
                }

                byte hdrA = 0;
                byte hdrB = 0;

                using (MemoryStream msDef = new MemoryStream())
                using (DeflateStream defS = new DeflateStream(msDef, CompressionLevel.Fastest, true))
                {
                    defS.Write(finalBuf, 0, finalBuf.Length);
                    defS.Close();

                    msDef.Seek(0, SeekOrigin.Begin);
                    hdrA = (byte)msDef.ReadByte();
                    hdrB = (byte)msDef.ReadByte();
                    
                    IDATChunk idat = new IDATChunk();
                    const int DEF_HDR = 4;
                    const int CHK_SUM = 0;
                    const int CHUNK_MAX_SIZE = 8192 - DEF_HDR - CHK_SUM;
                    byte[] chunkBUF = new byte[CHUNK_MAX_SIZE + DEF_HDR + CHK_SUM];

                    System.Diagnostics.Debug.Print($"{msDef.Length}/{finalBuf.Length}");
                    while (msDef.Position < msDef.Length)
                    {
                        var l = (msDef.Length - msDef.Position);
                        int len = (l > CHUNK_MAX_SIZE ? CHUNK_MAX_SIZE : (int)l);

                        chunkBUF[0] = hdrA;
                        chunkBUF[1] = hdrB;

                        chunkBUF[2] = 120;
                        chunkBUF[3] = 1;

                        msDef.Read(chunkBUF, 4, len);

                        //for (int i = len; i < len + CHK_SUM; i++)
                        //{
                        //    chunkBUF[i] = 0;
                        //}

                        idat.SetData(chunkBUF, len + DEF_HDR + CHK_SUM);
                        idat.Write(bw);

                        System.Diagnostics.Debug.Print(idat.ToMinString());
                    }
                }

                //IEND Chunk
                RawChunk iend = new RawChunk(0, PNGChunkType.IEND, new byte[0], 0);
                iend.crc = iend.GetCrc();
                iend.Write(bw);
            }
            return ImageEncodeResult.Success;
        }

        private Dictionary<FastColor, ColorContainer> _paletteLut = null;
        private List<ColorContainer> _palette = null;
        private bool _hasAlpha;
        private void GeneratePalette()
        {
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

            switch (_colorMode)
            {
                case ColorMode.ARGB555:
                case ColorMode.RGBA32:
                    _colorMode = _hasAlpha ? _colorMode : ColorMode.RGB24;
                    break;

                case ColorMode.RGB24:
                case ColorMode.RGB555:
                case ColorMode.RGB565:
                    _colorMode = !_hasAlpha ? _colorMode : ColorMode.RGBA32;
                    break;
            }
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
                    _colorMode = ColorMode.Indexed8;
                    _bpp = 8;
                    break;
            }
        }

        private byte[] _idatBuffer = new byte[8192];
        private int _idatSize;
        private void WriteScanline(BinaryWriter bw, PNGFilterMethod filterMode, byte[] data, byte[] buff)
        {
            IDATChunk chnk = new IDATChunk();


        }

        private void SubFilter(int scanLine, int x, byte[] data, byte[] targetData)
        {
            int i = scanLine + x;
            byte valA = data[i];
            byte valB = Sub(x, i, data);

            targetData[i] = (byte)(valA - valB);
        }

        private void UpFilter(int x, int y, int w, byte[] data, byte[] targetData)
        {
            int i = y * w + x;
            byte valA = data[i];
            byte valB = Prior(x, y, w, data);

            targetData[i] = (byte)(valA - valB);
        }

        private void AvgFilter(int x, int y, int w, byte[] data, byte[] targetData)
        {
            int i = y * w + x;
            byte valA = data[i];
            byte valB = Sub(x, i, data);
            byte valC = Prior(x, y, w, data);

            targetData[i] = (byte)(valA - ((valB + valC) >> 1));
        }

        private void PaethFilter(int x, int y, int w, byte[] data, byte[] targetData)
        {
            int i = y * w + x;

            byte a = Sub(x, i, data);
            byte b = Prior(x, y, w, data);
            byte c = Prior(x - 1, y, w, data);


            int p = a - b + c;

            int pA = Math.Abs(p - a);
            int pB = Math.Abs(p - b);
            int pC = Math.Abs(p - c);

            if (pA <= pB & pA <= pC)
            {
                targetData[i] = a;
                return;
            }
            targetData[i] = pB <= pC ? b : c;
        }

        private byte Sub(int x, int scanline, byte[] data) => x < 1 ? (byte)0 : data[scanline + x - 1];
        private byte Prior(int x, int y, int w, byte[] data) => (x < 0 | y < 1) ? (byte)0 : data[(y - 1) * w + x];

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
