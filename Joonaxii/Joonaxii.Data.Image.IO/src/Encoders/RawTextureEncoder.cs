using Joonaxii.Data.Image.IO.Processing;
using Joonaxii.Data.Compression.Huffman;
using Joonaxii.Data.Compression.RLE;
using System.Collections.Generic;
using Joonaxii.IO;
using System.IO;
using System;
using Joonaxii.Debugging;

namespace Joonaxii.Data.Image.IO
{
    public class RawTextureEncoder : ImageEncoderBase
    {
        private RawTextureCompressMode _compressMode;

        public RawTextureEncoder(int width, int height, byte bPP) : this(RawTextureCompressMode.None, width, height, bPP) { }
        public RawTextureEncoder(RawTextureCompressMode compress, int width, int height, byte bPP) : base(width, height, bPP)
        {
            _compressMode = compress;
        }

        public RawTextureEncoder(int width, int height, ColorMode pFmt) : this(RawTextureCompressMode.None, width, height, pFmt) { }
        public RawTextureEncoder(RawTextureCompressMode compress, int width, int height, ColorMode pFmt) : base(width, height, pFmt)
        {
            _compressMode = compress;
        }

        public void SetCompressionMode(RawTextureCompressMode mode) => _compressMode = mode;

        public ImageEncodeResult Encode(RawTextureCompressMode mode, Stream stream, bool leaveStreamOpen)
        {
            SetCompressionMode(mode);
            return Encode(stream, leaveStreamOpen);
        }

        private RawTextureCompressMode NoiseLevelToCompressMode(float noise)
        {
            if (noise >= 0.40f) { return RawTextureCompressMode.Huffman; }
            if (noise >= 0.25f) { return RawTextureCompressMode.RLEHuffman; }
            return RawTextureCompressMode.RLE;
        }

        public override ImageEncodeResult Encode(Stream stream, bool leaveStreamOpen)
        {
            using (BitWriter bw = new BitWriter(stream, leaveStreamOpen))
            {
                if (!HeaderManager.TryGetHeader(HeaderType.RAW_TEXTURE, out var hdr))
                {
                    return ImageEncodeResult.EncodeFailed;
                }

                TimeStamper stamp = new TimeStamper($"Raw Texture Encode with [{_compressMode}]");

                var res = _compressMode;
                if (res == RawTextureCompressMode.Auto)
                {
                    stamp.Start("Automatic Noisiness Detection");
                    res = NoiseLevelToCompressMode(new ImageNoiseDetector(true).Process(_pixels, _width, _height, _bpp));
                    stamp.Stamp();
                }

                stamp.Start("Header Writing");
                hdr.WriteHeader(bw);

                bw.Write((byte)_colorMode);
                bw.Write((byte)res);

                bw.Write((ushort)Math.Min(_width, ushort.MaxValue));
                bw.Write((ushort)Math.Min(_height, ushort.MaxValue));
                stamp.Stamp();

                //Write Uncompressed Raw Colors if compression mode is 0
                if (res == RawTextureCompressMode.None)
                {
                    stamp.Start("Write Raw Pixel Data");
                    bw.WriteColors(_pixels, _colorMode);
                    stamp.Stamp();

                    Console.WriteLine(stamp.ToString());
                    return ImageEncodeResult.Success;
                }

                stamp.Start("Palette Gen");
                //Generate palette and save it to the stream
                Dictionary<FastColor, int> palette = new Dictionary<FastColor, int>();
                List<FastColor> paletteL = new List<FastColor>();
                foreach (var c in _pixels)
                {
                    if (palette.ContainsKey(c)) { continue; }
                    palette.Add(c, palette.Count);
                    paletteL.Add(c);
                }
                bw.Write7BitInt(paletteL.Count);
                bw.WriteColors(paletteL.ToArray(), _colorMode);
                stamp.Stamp();

                List<int> paletteIndices = new List<int>();

                //Compress the indices with RLE, Huffman Coding or both
                switch (res)
                {
                    case RawTextureCompressMode.Huffman:
                        stamp.Start("Huffman Compression");
                        for (int i = 0; i < _pixels.Length; i++)
                        {
                            paletteIndices.Add(palette[_pixels[i]]);
                        }

                        Huffman.CompressToStream(bw, paletteIndices, true, out byte padded);
                        stamp.Stamp();
                        break;

                    case RawTextureCompressMode.RLE:
                        stamp.Start("RLE Compression");
                        for (int i = 0; i < _pixels.Length; i++)
                        {
                            paletteIndices.Add(palette[_pixels[i]]);
                        }

                        RLE.CompressToStream(bw, paletteIndices);
                        stamp.Stamp();
                        break;

                    case RawTextureCompressMode.RLEHuffman:

                        stamp.Start("RLE Index Gen");
                        List<int> indices = new List<int>();
                        for (int i = 0; i < _pixels.Length; i++)
                        {
                            indices.Add(palette[_pixels[i]]);
                        }
                        stamp.Stamp();

                        stamp.Start("RLE LUT Gen");
                        Dictionary<RLEChunk, int> lut = new Dictionary<RLEChunk, int>();
                        List<RLEChunk> lutList = new List<RLEChunk>();
                        RLE.CompressToLUT(indices, lut, lutList, paletteIndices, out byte lenBits, out byte valBits);

                        bw.Write(lenBits - 1, 3);
                        bw.Write(valBits - 1, 5);

                        bw.Write7BitInt(lutList.Count);
                        foreach (var item in lutList)
                        {
                            item.Write(bw, lenBits, valBits);
                        }
                        stamp.Stamp();

                        lut.Clear();
                        lutList.Clear();
                        indices.Clear();

                        stamp.Start("RLE Index Compression via Huffman");
                        Huffman.CompressToStream(bw, paletteIndices, true, out padded);
                        stamp.Stamp();
                        break;
                }
                Console.WriteLine(stamp.ToString());

                palette.Clear();
                paletteL.Clear();
                paletteIndices.Clear();
                return ImageEncodeResult.Success;
            }
        }
    }
}
