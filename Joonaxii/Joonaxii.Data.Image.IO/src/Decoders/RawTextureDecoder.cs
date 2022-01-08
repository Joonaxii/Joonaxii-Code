using Joonaxii.Data.Compression.Huffman;
using Joonaxii.Data.Compression.RLE;
using Joonaxii.Debugging;
using Joonaxii.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace Joonaxii.Data.Image.IO
{
    public class RawTextureDecoder : ImageDecoderBase
    {
        public RawTextureDecoder(Stream stream) : base(new BitReader(stream), false) { }
        public RawTextureDecoder(BinaryReader br, bool dispose) : base(br, dispose) { }

        public override ImageDecodeResult Decode(bool skipHeader)
        {
            if (!skipHeader)
            {
                if(HeaderManager.GetFileType(_br, false) != HeaderType.RAW_TEXTURE) { return ImageDecodeResult.InvalidImageFormat; }
            }

            _colorMode = (ColorMode)_br.ReadByte();
            _bpp = _colorMode.GetBPP();
            RawTextureCompressMode compressMode = (RawTextureCompressMode)_br.ReadByte();

            _width = _br.ReadUInt16();
            _height = _br.ReadUInt16();
            _pixels = new FastColor[_width * _height];

            TimeStamper stamp = new TimeStamper($"Raw Texture Decode with [{compressMode}]");
            if (compressMode == RawTextureCompressMode.None)
            {
                stamp.Start("Read Raw Pixel Data");
                _br.ReadColors(_pixels, _colorMode);
                stamp.Stamp();

                Console.WriteLine(stamp.ToString());
                return ImageDecodeResult.Success;
            }

            MemoryStream stream = null;
            BitReader _brI = _br as BitReader;
            bool isBitReader = _brI != null;

            if (!isBitReader)
            {
                stamp.Start($"Copying to Memory [NOT BIT READER!]");
                stream = new MemoryStream();
                _br.BaseStream.CopyToWithPos(stream);
                _brI = new BitReader(stream);
                stamp.Stamp();
            }

            stamp.Start("Palette Prep");
            int paletteSize = _brI.Read7BitInt();
            FastColor[] palette = new FastColor[paletteSize];
            stamp.Stamp();

            stamp.Start("Palette Read");
            _brI.ReadColors(palette, _colorMode);
            stamp.Stamp();

            List<int> indices = new List<int>();
            switch (compressMode)
            {
                case RawTextureCompressMode.Huffman:
                    stamp.Start("Decompress Huffman");
                    Huffman.DecompressFromStream(_brI, indices);
                    for (int i = 0; i < _pixels.Length; i++)
                    {
                        _pixels[i] = palette[indices[i]];
                    }
                    stamp.Stamp();
                    break;

                case RawTextureCompressMode.RLE:
                    stamp.Start("Decompress RLE");
                    RLE.DecompressFromStream(_brI, indices);
                    for (int i = 0; i < _pixels.Length; i++)
                    {
                        _pixels[i] = palette[indices[i]];
                    }
                    stamp.Stamp();
                    break;

                case RawTextureCompressMode.RLEHuffman:

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
    }
}
