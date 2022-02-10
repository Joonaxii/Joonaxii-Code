using Joonaxii.Data.Compression.Huffman;
using Joonaxii.Data.Compression.RLE;
using Joonaxii.Debugging;
using Joonaxii.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace Joonaxii.Data.Image.Conversion
{
    public class RawTextureDecoder : ImageDecoderBase
    {
        public const int PIXEL_COMPRESS_THRESHOLD = 64 * 64;

        public RawTextureDecoder(Stream stream) : base(new BitReader(stream), false) { }
        public RawTextureDecoder(BinaryReader br, bool dispose) : base(br, dispose) { }

        public override ImageDecodeResult Decode(bool skipHeader)
        {
            if (!skipHeader)
            {
                if(HeaderManager.GetFileType(_br, false) != HeaderType.RAW_TEXTURE) { return ImageDecodeResult.InvalidImageFormat; }
            }
   
            MemoryStream stream = null;
            BitReader _brI = _br as BitReader;
            bool isBitReader = _brI != null;
            TimeStamper stamp = new TimeStamper($"Raw Texture Decode!");

            if (!isBitReader)
            {
                stamp.Start($"Copying to Memory [NOT BIT READER!]");
                stream = new MemoryStream();
                _br.BaseStream.CopyToWithPos(stream);
                _brI = new BitReader(stream);
                stamp.Stamp();
            }

            _colorMode = (ColorMode)_brI.ReadByte();
            _bpp = _colorMode.GetBPP();
            RawTextureCompressMode compressMode = (RawTextureCompressMode)_brI.ReadByte(7);
            bool compressPixels = _brI.ReadBoolean();

            _width = _br.ReadUInt16();
            _height = _br.ReadUInt16();
            int l = _width * _height;
 
            if (compressMode == RawTextureCompressMode.None)
            {
                compressPixels &= l >= PIXEL_COMPRESS_THRESHOLD;
                if (compressPixels)
                {
                    stamp.Start("Huffman Decompression of Color bytes");
                    List<int> ints = new List<int>();
                    Huffman.DecompressFromStream(_brI, ints);
                    byte[] bytes = new byte[ints.Count];
                    for (int i = 0; i < ints.Count; i++)
                    {
                        var intT = ints[i];
                        bytes[i] = (byte)(intT > 255 ? 255 : intT < 0 ? 0 : intT);
                    }
                    ints.Clear();
                    _pixels = ImageIOExtensions.ToFastColor(bytes, _colorMode);
                    stamp.Stamp();
                }
                else
                {
                    _pixels = new FastColor[l];
                    stamp.Start("Read Raw Pixel Data");
                    _br.ReadColors(_pixels, _colorMode);
                    stamp.Stamp();
                }

                Console.WriteLine(stamp.ToString());
                if (!isBitReader)
                {
                    _br.BaseStream.Position = _brI.BaseStream.Position;

                    stream.Dispose();
                    _brI.Dispose();
                }
                return ImageDecodeResult.Success;
            }

            bool readPalette = false;
            //ColorMode palMode = _colorMode;
            switch (compressMode)
            {
                case RawTextureCompressMode.IdxHuffman:
                case RawTextureCompressMode.IdxRLE:
                case RawTextureCompressMode.IdxRLEHuffman:
                    readPalette = true;
                    break;
                case RawTextureCompressMode.IdxaRLE:
                    readPalette = true;
                    break;
            }

            _pixels = new FastColor[l];
            FastColor[] palette = null;
            int paletteSize = 0;

            if (readPalette)
            {
                stamp.Start("Palette Prep");
                paletteSize = _brI.Read7BitInt();
                palette = new FastColor[paletteSize];
                stamp.Stamp();

                stamp.Start("Palette Read");
                _brI.ReadColors(palette, _colorMode);
                stamp.Stamp();
            }

            List<int> indices = new List<int>();
            switch (compressMode)
            {
                case RawTextureCompressMode.aRLE:
                    stamp.Start("Decompress Alpha RLE");
                    RLE.DecompressFromStream(_brI, indices);
                    stamp.Stamp();

                    _brI.ReadColors(_pixels, ColorMode.RGB24);
                    for (int i = 0; i < _pixels.Length; i++)
                    {
                        _pixels[i].a = (byte)indices[i];
                    }
                    break;

                case RawTextureCompressMode.IdxaRLE:
                    List<int> alphaInd = new List<int>(); 
                    stamp.Start("Decompress Alpha RLE");
                    RLE.DecompressFromStream(_brI, alphaInd);
                    stamp.Stamp();

                    stamp.Start("Decompress Idx RLE");
                    RLE.DecompressFromStream(_brI, indices);
                    stamp.Stamp();

                    for (int i = 0; i < _pixels.Length; i++)
                    {
                        var ind = indices[i];
                        var color = palette[ind];
                        color.a = (byte)alphaInd[ind];
                        _pixels[i] = color;
                    }
                    break;

                case RawTextureCompressMode.IdxHuffman:
                    stamp.Start("Decompress Huffman");
                    Huffman.DecompressFromStream(_brI, indices);
                    for (int i = 0; i < _pixels.Length; i++)
                    {
                        _pixels[i] = palette[indices[i]];
                    }
                    stamp.Stamp();
                    break;

                case RawTextureCompressMode.IdxRLE:
                    stamp.Start("Decompress RLE");
                    RLE.DecompressFromStream(_brI, indices);
                    for (int i = 0; i < _pixels.Length; i++)
                    {
                        _pixels[i] = palette[indices[i]];
                    }
                    stamp.Stamp();
                    break;

                case RawTextureCompressMode.IdxRLEHuffman:

                    stamp.Start("Read RLE Chunk Palette");
                    byte lenBits = (byte)(_brI.ReadByte(3) + 1);
                    byte valBits = (byte)(_brI.ReadByte(5) + 1);

                    RLEChunk[] chunks = new RLEChunk[_brI.Read7BitInt()];  
                    for (int i = 0; i < chunks.Length; i++)
                    {
                        chunks[i].Read(_brI, lenBits, valBits);
                    }
                    stamp.Stamp();

                    stamp.Start("Decompress RLE Indices with Huffman");
                    Huffman.DecompressFromStream(_brI, indices);
                    stamp.Stamp();

                    stamp.Start("Index Writing");
                    int ii = 0;
                    for (int i = 0; i < indices.Count; i++)
                    {
                        var chnk = chunks[indices[i]];
                        var c = palette[chnk.value.ToInt32];
                        for (int j = 0; j <= chnk.count; j++)
                        {
                            _pixels[ii++] = c;
                        }
                    }
                    stamp.Stamp();
                    break;
            }

            if (!isBitReader)
            {
                _br.BaseStream.Position = _brI.BaseStream.Position;

                stream.Dispose();
                _brI.Dispose();
            }

            Console.WriteLine(stamp.ToString());
            indices.Clear();
            return ImageDecodeResult.Success;
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
                case ColorMode.Indexed4:
                case ColorMode.Indexed8:
                case ColorMode.Grayscale:
                    _colorMode = ColorMode.RGB24;
                    _bpp = 24;
                    break;
            }
        }
    }
}
