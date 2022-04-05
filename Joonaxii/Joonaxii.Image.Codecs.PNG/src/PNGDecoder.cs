using Joonaxii.Collections;
using Joonaxii.Data;
using Joonaxii.Data.Coding;
using Joonaxii.Image.Texturing;
using Joonaxii.IO;
using Joonaxii.MathJX;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Joonaxii.Image.Codecs.PNG
{
    public class PNGDecoder : ImageDecoderBase
    {
        public const float DEFAULT_GAMMA = 1.0f / 2.2f;

        private PNGGammaReadMode _gammaMode;

        private IHDRChunk _header;
        private List<PNGChunk> _dataChunks = null;

        private int _requiredData = 0;

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

            Dictionary<PNGChunkType, PNGChunk> chunkLut = new Dictionary<PNGChunkType, PNGChunk>();
            unsafe
            {
                PLTEChunk paletteChnk = null;
                gAMAChunk gammaChnk = null;

                _dataChunks = new List<PNGChunk>();

                List<SPLTChunk> sPalettes = new List<SPLTChunk>();
#if DEBUG
                List<PNGChunk> chunks = new List<PNGChunk>();
                Dictionary<PNGFilterMethod, int> filterCounts = new Dictionary<PNGFilterMethod, int>();
                int totalFilters = 0;
#endif

                int stateIDAT = 0;
                while (_stream.Length > _stream.Position)
                {
                    var chnk = PNGChunk.Read(_br, _stream, ValidateChunk);
                    if (!chnk.IsValid) { return ImageDecodeResult.HashMismatch; }
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
                                    SetHeaderChunk(chnk, true);
                                    System.Diagnostics.Debug.Print($"{_header}");

                                    break;
                                case PNGChunkType.PLTE:
                                    paletteChnk = chnk as PLTEChunk;
                                    break;
                            }
                            break;

                        case PNGChunkType.tRNS:
                            if (paletteChnk != null)
                            {
                                paletteChnk.ApplyTransparency(_stream, chnk as tRNSChunk);
                            }
                            break;

                        case PNGChunkType.IDAT:
                            switch (stateIDAT)
                            {
                                case 0: stateIDAT = 1; break;
                                case 2: return ImageDecodeResult.DataMisalignment;
                            }

                            _dataChunks.Add(chnk);
                            _requiredData += chnk.length;
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

                    if (_header == null) { return ImageDecodeResult.DataCorrupted; }
                }
  
                byte[] dataBuffer = new byte[_requiredData];
                int bufPos = 0;

                long pos = _stream.Position;
                foreach (var chnk in _dataChunks)
                {
                    _stream.Seek(chnk.dataStart, SeekOrigin.Begin);

                    int len = chnk.length;
                    _stream.Read(dataBuffer, bufPos, len);
                    bufPos += len;
                }
                
                _dataChunks.Clear();
                _stream.Seek(pos, SeekOrigin.Begin);

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

                if (_texture.Format == TextureFormat.Indexed)
                {
                    _texture.SetPalette(paletteChnk.pixels);
                }

                var bytesPerPix = (byte)(_header.GetBytesPerPixel() * ((_header.bitDepth + 7) >> 3));
                System.Diagnostics.Debug.Print($"{_texture.Format} => {_texture.BitsPerPixel}, {bytesPerPix}");

                using (MemoryStream ms = new MemoryStream(dataBuffer))
                using (DeflateStream brDat = new DeflateStream(ms, CompressionMode.Decompress))
                using (BufferedStream msOut = new BufferedStream(brDat, 8192))
                {
                    ms.Seek(2, SeekOrigin.Begin);

                    int bytesPerLine = _header.GetBytesPerScanline();
                    int bufSize = bytesPerLine << 1;

                    bool usePalette = paletteChnk != null && _header.colorType == PNGColorType.PALETTE_IDX;
                    int bufferOffset = bytesPerPix << 1;
                    byte[] scanData = new byte[bufSize + bufferOffset];

                    int readOffset = bytesPerLine + bufferOffset;
                    int endOffset = bufSize + bufferOffset;
                    var scanPtr = (byte*)_texture.LockBits();
                    fixed (byte* scan = scanData)
                    {
                        byte* prior = scan + bytesPerPix;
                        byte* prev = scan + bytesPerLine + bytesPerPix;
                        byte* cur = scan + readOffset;

                        switch (_header.interlaceMethod)
                        {
                            case InterlaceMethod.None:
                                for (int i = 0; i < _texture.Height; i++)
                                {
                                    PNGFilterMethod filterMode = (PNGFilterMethod)msOut.ReadByte();
                                    msOut.Read(scanData, readOffset, bytesPerLine);
#if DEBUG
                                    if (filterCounts.ContainsKey(filterMode)) { filterCounts[filterMode]++; }
                                    else { filterCounts.Add(filterMode, 1); }
                                    totalFilters++;
#endif
                                    if (filterMode != PNGFilterMethod.None)
                                    {
                                        ReverseFilter(prior, scan, prev, cur, filterMode, bytesPerLine);
                                    }
                                    BufferUtils.Memcpy(prior, cur, bytesPerLine);
                                  
                                    if (usePalette)
                                    {
                                        BufferUtils.Memcpy(scanPtr, cur, bytesPerLine);
                                        scanPtr += bytesPerLine;
                                        continue;
                                    }

                                    switch (bytesPerPix)
                                    {
                                        default:
                                            BufferUtils.Memcpy(scanPtr, cur, bytesPerLine);
                                            scanPtr += bytesPerLine;
                                            break;
                                        case 8:
                                            const float SHORT_TO_BYTE = (1.0f / ushort.MaxValue) * 255;
                                            for (int j = 0; j < bytesPerLine; j += 8)
                                            {
                                                ushort r = (ushort)(cur[j + 1] + (cur[j + 0] << 8));
                                                ushort g = (ushort)(cur[j + 3] + (cur[j + 2] << 8));
                                                ushort b = (ushort)(cur[j + 5] + (cur[j + 4] << 8));
                                                ushort a = (ushort)(cur[j + 7] + (cur[j + 6] << 8));

                                                *scanPtr++ = gammaTable[(byte)(r * SHORT_TO_BYTE)];
                                                *scanPtr++ = gammaTable[(byte)(g * SHORT_TO_BYTE)];
                                                *scanPtr++ = gammaTable[(byte)(b * SHORT_TO_BYTE)];
                                                *scanPtr++ = (byte)(a * SHORT_TO_BYTE);
                                            }
                                            break;
                                    }
                                }
                                break;
                            default: return ImageDecodeResult.NotSupported;
                        }
                    }

                    _texture.UnlockBits();
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
            return ImageDecodeResult.Success;
        }

        protected override ImageDecodeResult LoadGeneralTextureInfo(BinaryReader br)
        {
            var hdr = HeaderManager.GetFileType(br, false);
            if (hdr == HeaderType.PNG)
            {
                PNGChunk dummy = new PNGChunk();
                while (_header == null)
                {
                    var chnk = PNGChunk.Read(br, _stream, (PNGChunkType curT) =>
                    {
                        switch (curT)
                        {
                            case PNGChunkType.IHDR:
                                return _header;
                            default: return dummy;
                        }
                    });

                    if (chnk.chunkType == PNGChunkType.IHDR)
                    {
                        SetHeaderChunk(chnk, false);
                    }
                }
                return ImageDecodeResult.Success;
            }

            return ImageDecodeResult.InvalidImageFormat;
        }
        public override void LoadGeneralInformation(long pos)
        {
            base.LoadGeneralInformation(pos);
            long cur = _stream.Position;
            _stream.Seek(pos, SeekOrigin.Begin);
            LoadGeneralTextureInfo(_br);
            _stream.Seek(cur, SeekOrigin.Begin);
        }

        public override int GetDataCRC(long pos)
        {
            int crc = 0;
            long cur = _stream.Position;
            _stream.Seek(pos, SeekOrigin.Begin);

            var hdr = HeaderManager.GetFileType(_br, false);
            PinnableList<uint> crcList = new PinnableList<uint>(16);
            if (hdr == HeaderType.PNG)
            {
                PNGChunk dummy = new PNGChunk();

                while (true)
                {
                    uint? crcIn = PNGChunk.ReadCrc(_br, _stream, out PNGChunkType chunkType, (PNGChunkType curT) =>
                    {
                        switch (curT)
                        {
                            case PNGChunkType.PLTE:
                            case PNGChunkType.tRNS:
                            case PNGChunkType.sPLT:
                            case PNGChunkType.IHDR:
                            case PNGChunkType.IDAT:
                                return false;

                            default: return true;
                        }
                    });
                    if(chunkType == PNGChunkType.IEND) { break; }

                    if (crcIn == null) { continue; }
                    crcList.Add(crcIn.GetValueOrDefault());
                }
            }
            _stream.Seek(cur, SeekOrigin.Begin);
            unsafe
            {
                byte* b = (byte*)crcList.Pin();
                crc = (int)CRC.Calculate(b, 0, crcList.Count * sizeof(uint));
            }
            return crc;
        }

        private void SetHeaderChunk(PNGChunk chunk, bool generateTexture)
        {
            _header = chunk as IHDRChunk;
            var bpp = _header.bitDepth;
            TextureFormat format = TextureFormat.RGBA32;
            switch (_header.colorType)
            {
                case PNGColorType.GRAYSCALE:
                    bpp = 8;
                    format = TextureFormat.Grayscale;
                    break;
                case PNGColorType.PALETTE_IDX:
                    format = TextureFormat.Indexed;
                    bpp = _header.bitDepth;
                    break;

                case PNGColorType.RGB:
                    bpp = 24;
                    format = ImageCodecExtensions.GetColorMode(bpp);
                    break;

                case PNGColorType.GRAY_ALPHA:
                case PNGColorType.RGB_ALPHA:
                    bpp = 32;
                    format = ImageCodecExtensions.GetColorMode(bpp);
                    break;
            }

            _general.bitsPerPixel = bpp;
            _general.width = (ushort)_header.width;
            _general.height = (ushort)_header.height;

            if (!generateTexture) { return; }
            GenerateTexture(_header.width, _header.height, format, _header.bitDepth);
        }

        private PNGChunk ValidateChunk(PNGChunkType type)
        {
            switch (type)
            {
                default: return null;
                case PNGChunkType.IHDR: return _header;
            }
        }

        private unsafe void ReverseFilter(byte* prior, byte* priorPrev, byte* prev, byte* scan, PNGFilterMethod filterMode, int scanSize)
        {
            switch (filterMode)
            {
                default: System.Diagnostics.Debug.Print($"Filter: {filterMode} not implemented!"); break;

                case PNGFilterMethod.Sub:
                    while (scanSize-- > 0)
                    {
                        *scan++ += *prev++;
                    }
                    break;

                case PNGFilterMethod.Up:
                    while (scanSize-- > 0)
                    {
                        *scan++ += *prior++;
                    }
                    break;

                case PNGFilterMethod.Average:
                    while (scanSize-- > 0)
                    {
                        *scan++ += (byte)((*prev++ + *prior++) >> 1);
                    }
                    break;
                case PNGFilterMethod.Paeth:
                    while (scanSize-- > 0)
                    {
                        *scan++ += GetPaethValue(*prev++, *prior++, *priorPrev++);
                    }
                    break;
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

        public override void Dispose()
        {
            base.Dispose();
            _texture?.Dispose();
            _texture = null;
        }
    }
}
