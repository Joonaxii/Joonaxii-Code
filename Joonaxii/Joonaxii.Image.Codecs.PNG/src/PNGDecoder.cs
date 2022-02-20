using Joonaxii.Data;
using Joonaxii.MathJX;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Joonaxii.Image.Codecs.PNG
{
    public class PNGDecoder : ImageDecoderBase
    {
        public const float DEFAULT_GAMMA = 1.0f / 2.2f;

        private PNGGammaReadMode _gammaMode;

        public PNGDecoder(Stream stream) : this(PNGGammaReadMode.Ignore, stream) { }
        public PNGDecoder(PNGGammaReadMode gammaMode, Stream stream) : base(stream) { _gammaMode = gammaMode; }
        public PNGDecoder(BinaryReader br, bool dispose) : this(PNGGammaReadMode.Ignore, br, dispose) { }
        public PNGDecoder(PNGGammaReadMode gammaMode, BinaryReader br, bool dispose) : base(br, dispose) { _gammaMode = gammaMode; }

        public override ImageDecodeResult Decode(bool skipHeader)
        {
            if (!skipHeader)
            {
                var hdr = HeaderManager.GetFileType(_br, false);
                if (hdr != HeaderType.PNG) { return ImageDecodeResult.InvalidImageFormat; }
            }

            var strm = _br.BaseStream;
            Dictionary<PNGChunkType, PNGChunk> chunkLut = new Dictionary<PNGChunkType, PNGChunk>();

            IHDRChunk hdrChnk = null;
            PLTEChunk paletteChnk = null;
            gAMAChunk gammaChnk = null;

            List<SPLTChunk> sPalettes = new List<SPLTChunk>();
#if DEBUG
            List<PNGChunk> chunks = new List<PNGChunk>();
            Dictionary<PNGFilterMethod, int> filterCounts = new Dictionary<PNGFilterMethod, int>();
            int totalFilters = 0;
#endif
            int stateIDAT = 0;
            using (MemoryStream ms = new MemoryStream())
            {
                while (strm.Length > strm.Position)
                {
                    var chnk = PNGChunk.Read(_br);
                    if (!chnk.IsValid)
                    {
                        uint val = chnk.GetCrc();
                        System.Diagnostics.Debug.Print($"{chnk.ToMinString()}\n -Hash was: {val} [0x{Convert.ToString(val, 16).PadLeft(8, '0')}]\n -Should be: {chnk.crc} [0x{Convert.ToString(chnk.crc, 16).PadLeft(8, '0')}]");
                        return ImageDecodeResult.HashMismatch;
                    }
#if DEBUG
                    chunks.Add(chnk);
#endif
                    switch (chnk.chunkType)
                    {
                        case PNGChunkType.IEND: break;

                        default:
                            if (chunkLut.ContainsKey(chnk.chunkType)) 
                            {
                                System.Diagnostics.Debug.Print($"Found a duplicate of '{chnk.chunkType}'");
                                return ImageDecodeResult.DuplicateChunkFound; 
                            }

                            chunkLut.Add(chnk.chunkType, chnk);
                            switch (chnk.chunkType)
                            {
                                case PNGChunkType.IHDR:
                                    hdrChnk = chnk as IHDRChunk;
                                    System.Diagnostics.Debug.Print($"{hdrChnk}");
                                    break;
                                case PNGChunkType.PLTE:
                                    paletteChnk = chnk as PLTEChunk;
                                    break;
                            }
                            break;

                        case PNGChunkType.tRNS:
                            if (paletteChnk != null)
                            {
                                paletteChnk.ApplyTransparency(chnk.data);
                            }
                            break;

                        case PNGChunkType.IDAT:
                            switch (stateIDAT)
                            {
                                case 0: stateIDAT = 1; break;
                                case 2: return ImageDecodeResult.DataMisalignment;
                            }
                            ms.Write(chnk.data, 0, chnk.data.Length);
                            break;

                        case PNGChunkType.sPLT:
                            sPalettes.Add(chnk as SPLTChunk);
                            break;
                        case PNGChunkType.gAMA:
                            gammaChnk = chnk as gAMAChunk;
                            break;

                        case PNGChunkType.tEXt:
                        case PNGChunkType.iTXt:
                        case PNGChunkType.zTXt:
                            break;
                    }

                    switch (stateIDAT)
                    {
                        case 1:
                            if (chnk.chunkType != PNGChunkType.IDAT) { stateIDAT = 2; }
                            break;
                    }

                    if (hdrChnk == null) { return ImageDecodeResult.DataCorrupted; }
                }

                float gamma = 1.0f;
                bool applyGamma = false;

                switch (_gammaMode)
                {
                    case PNGGammaReadMode.UseDefault:
                        applyGamma = true;
                        gamma = DEFAULT_GAMMA;
                        break;
                    case PNGGammaReadMode.UseDefaultIfMissing:
                        applyGamma = true;
                        if (gammaChnk != null)
                        {
                            gamma = gammaChnk.gamma;
                            break;
                        }
                        gamma = DEFAULT_GAMMA;
                        break;
                    case PNGGammaReadMode.IgnoreIfMissing:
                        if (gammaChnk != null)
                        {
                            applyGamma = true;
                            gamma = gammaChnk.gamma;
                        }
                        break;
                }

                const float BYTE_TO_FLOAT = 1.0f / 255.0f;
                byte[] gammaTable = new byte[256];
                for (int i = 0; i < 256; i++)
                {
                    int val = applyGamma ? (int)Math.Round(Math.Pow(i * BYTE_TO_FLOAT, gamma) * 255) : i;
                    val = val < 0 ? 0 : val > 255 ? 255 : val;
                    gammaTable[i] = (byte)val;
                }
                paletteChnk?.ApplyGamma(gammaTable);

                ms.Seek(0, SeekOrigin.Begin);
                byte flagA = (byte)ms.ReadByte();
                byte flagB = (byte)ms.ReadByte();
                ms.Seek(2, SeekOrigin.Begin);

                _width = hdrChnk.width;
                _height = hdrChnk.height;
                _bpp = hdrChnk.bitDepth;

                switch (hdrChnk.colorType)
                {
                    case PNGColorType.GRAYSCALE:
                        _bpp = 8;
                        _colorMode = ColorMode.Grayscale;
                        break;
                    case PNGColorType.PALETTE_IDX:
                        _colorMode = ColorMode.Indexed;
                        _bpp = hdrChnk.bitDepth;
                        break;

                    case PNGColorType.RGB:
                        _bpp = 24;
                        _colorMode = ImageCodecExtensions.GetColorMode(_bpp);
                        break;

                    case PNGColorType.GRAY_ALPHA:
                    case PNGColorType.RGB_ALPHA:
                        _bpp = 32;
                        _colorMode = ImageCodecExtensions.GetColorMode(_bpp);
                        break;
                }

                _pixels = new FastColor[_width * _height];
                var bytesPerPix = (byte)(hdrChnk.GetBytesPerPixel() * ((hdrChnk.bitDepth + 7) >> 3));
                System.Diagnostics.Debug.Print($"{_colorMode} => {_bpp}, {bytesPerPix}");

                using (DeflateStream brDat = new DeflateStream(ms, CompressionMode.Decompress))
                using (MemoryStream msIn = new MemoryStream())
                {
                    brDat.CopyTo(msIn);
                    msIn.Seek(0, SeekOrigin.Begin);

                    byte[] buffer = new byte[8];

                    var data = new byte[msIn.Length];
                    msIn.Read(data, 0, data.Length);
                    int posGottenTo = 0;
                    switch (hdrChnk.interlaceMethod)
                    {
                        case InterlaceMethod.None:
                            var bytesPerLine = hdrChnk.GetBytesPerScanline();

                            for (int i = 0; i < _height; i++)
                            {
                                int index = i * (bytesPerLine + 1);
                                int pixI = i * _width;
                                PNGFilterMethod filterMode = (PNGFilterMethod)data[index++];
#if DEBUG
                                if (filterCounts.ContainsKey(filterMode)) { filterCounts[filterMode]++; }
                                else { filterCounts.Add(filterMode, 1); }
                                totalFilters++;
#endif
                                int end = index + bytesPerLine;
                                if (filterMode != PNGFilterMethod.None)
                                {
                                    ReverseFilter(data, filterMode, bytesPerPix, i, bytesPerLine);
                                }
                                int x = 0;

                                if (paletteChnk != null && hdrChnk.colorType == PNGColorType.PALETTE_IDX)
                                {
                                    posGottenTo = index;
                                    for (int j = 0; j < _width; j++)
                                    {
                                        Buffer.BlockCopy(data, index + j * bytesPerPix, buffer, 0, bytesPerPix);

                                        int indexP = 0;
                                        for (int p = 0; p < bytesPerPix; p++)
                                        {
                                            indexP |= (buffer[p] << (p << 3));
                                        }
                                        _pixels[pixI + j] = indexP >= paletteChnk.pixels.Length ? FastColor.clear : paletteChnk.pixels[indexP];
                                        posGottenTo += bytesPerPix;
                                    }
                                    continue;
                                }

                                posGottenTo = end;
                                for (int j = index; j < end; j += bytesPerPix)
                                {
                                    Buffer.BlockCopy(data, j, buffer, 0, bytesPerPix);
                                    switch (bytesPerPix)
                                    {
                                        case 1:
                                            _pixels[pixI + x] = new FastColor(buffer[0]);
                                            break;
                                        case 2:
                                            _pixels[pixI + x] = new FastColor(buffer[1], buffer[1], buffer[1], buffer[0]);
                                            break;
                                        case 3:
                                            _pixels[pixI + x] = new FastColor(gammaTable[buffer[0]], gammaTable[buffer[1]], gammaTable[buffer[2]]);
                                            break;

                                        case 4:
                                            _pixels[pixI + x] = new FastColor(gammaTable[buffer[0]], gammaTable[buffer[1]], gammaTable[buffer[2]], buffer[3]);
                                            break;
                                        case 8:
                                            const float SHORT_TO_BYTE = (1.0f / ushort.MaxValue) * 255;
                                            ushort r = (ushort)(buffer[1] + (buffer[0] << 8));
                                            ushort g = (ushort)(buffer[3] + (buffer[2] << 8));
                                            ushort b = (ushort)(buffer[5] + (buffer[4] << 8));
                                            ushort a = (ushort)(buffer[7] + (buffer[6] << 8));

                                            _pixels[pixI + x] = new FastColor(
                                                gammaTable[(byte)(r * SHORT_TO_BYTE)],
                                                gammaTable[(byte)(g * SHORT_TO_BYTE)],
                                                gammaTable[(byte)(b * SHORT_TO_BYTE)],
                                                (byte)(a * SHORT_TO_BYTE));
                                            break;
                                    }
                                    x++;
                                }
                            }
                            break;
                        default: return ImageDecodeResult.NotSupported;
                    }


                    System.Diagnostics.Debug.Print($"ZLIB Flags [0x{Convert.ToString(flagA, 16).PadLeft(2, '0')}, 0x{Convert.ToString(flagB, 16).PadLeft(2, '0')}]\nFinal Read Pos: {posGottenTo}, {data.Length - posGottenTo} diff to length");
                }

            }
#if DEBUG
            foreach (var chunk in chunks)
            {
                System.Diagnostics.Debug.Print($"{chunk.ToMinString()}");
            }
            System.Diagnostics.Debug.Print(new string('=', 32));
            System.Diagnostics.Debug.Print("");

            foreach (var filters in filterCounts)
            {
                System.Diagnostics.Debug.Print($"Filter: {filters.Key}, {filters.Value} ({((filters.Value / (float)totalFilters) * 100.0f).ToString("F2")}%)");
            }
            System.Diagnostics.Debug.Print(new string('=', 32));
            System.Diagnostics.Debug.Print("");
#endif
            return ImageDecodeResult.Success;
        }

        private void ReverseFilter(byte[] data, PNGFilterMethod filterMode, int bytesPerPixel, int scanLine, int bytesPerLine)
        {
            bytesPerLine++;
            int prior = (scanLine - 1) * bytesPerLine;
            scanLine *= bytesPerLine;

            for (int i = 1; i < bytesPerLine; i++)
            {
                int j = scanLine + i;
                int x = i;
                switch (filterMode)
                {
                    case PNGFilterMethod.Sub:
                        data[j] += Raw(x - bytesPerPixel);
                        break;

                    case PNGFilterMethod.Up:
                        data[j] += Prior(x);
                        break;

                    default: System.Diagnostics.Debug.Print($"Filter: {filterMode} not implemented! [{x}, {scanLine / bytesPerLine}]"); break;
                    case PNGFilterMethod.Average:
                        data[j] += (byte)((Raw(x - bytesPerPixel) + Prior(x)) >> 1);
                        break;
                    case PNGFilterMethod.Paeth:
                        byte l = Raw(x - bytesPerPixel);
                        byte a = Prior(x);
                        byte aL = Prior(x - bytesPerPixel);
                        data[j] += GetPaethValue(l, a, aL);
                        break;
                }
            }

            byte Prior(int i)
            {
                if (i < 1 | prior < 0) { return 0; }

                int v = prior + i;
                return data[v];
            }

            byte Raw(int i)
            {
                if (i < 1) { return 0; }

                int v = scanLine + i;
                return data[v];
            }
        }

        private byte GetPaethValue(byte a, byte b, byte c)
        {
            int p = a + b - c;

            int pA = Math.Abs(p - a);
            int pB = Math.Abs(p - b);
            int pC = Math.Abs(p - c);

            if (pA <= pB & pA <= pC) { return a; }
            return pB <= pC ? b : c;
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
    }
}
