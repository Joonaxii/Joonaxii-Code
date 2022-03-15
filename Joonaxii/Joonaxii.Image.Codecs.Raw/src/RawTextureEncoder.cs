using Joonaxii.Image.Processing;
using Joonaxii.Data.Compression.Huffman;
using Joonaxii.Data.Compression.RLE;
using System.Collections.Generic;
using Joonaxii.IO;
using System.IO;
using System;
using Joonaxii.Debugging;
using Joonaxii.Data;
using Joonaxii.MathJX;
using Joonaxii.Image.Texturing;

namespace Joonaxii.Image.Codecs.Raw
{
    public class RawTextureEncoder : ImageEncoderBase
    {
        private RawTextureCompressMode _compressMode;
        private bool _compressPixelData;

        public RawTextureEncoder(TextureFormat pFmt) : this(null, RawTextureCompressMode.None, false, pFmt) { }
        public RawTextureEncoder(Texture original, TextureFormat pFmt) : this(original, RawTextureCompressMode.None, false, pFmt) { }
        public RawTextureEncoder(Texture original, RawTextureCompressMode compress, bool compressPixelData, TextureFormat pFmt) : base(original, pFmt)
        {
            _compressMode = compress;
            _compressPixelData = compressPixelData;
        }

        public void SetCompressionMode(RawTextureCompressMode mode, bool compressPixelData)
        {
            _compressMode = mode;
            _compressPixelData = compressPixelData;
        }

        public ImageEncodeResult Encode(RawTextureCompressMode mode, bool compressPixelData, Stream stream, bool leaveStreamOpen)
        {
            SetCompressionMode(mode, compressPixelData);
            return Encode(stream, leaveStreamOpen);
        }

        private RawTextureCompressMode NoiseLevelToCompressMode(float noise)
        {
            if (noise >= 0.40f) { return RawTextureCompressMode.IdxHuffman; }
            if (noise >= 0.25f) { return RawTextureCompressMode.IdxRLEHuffman; }
            return RawTextureCompressMode.IdxRLE;
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
                   // res = NoiseLevelToCompressMode(new ImageNoiseDetector().Process(new PixelArray(_pixels), _width, _height, _bpp));
                    stamp.Stamp();
                }

                bool writePalette = true;
                var palMode = _format;
                switch (res)
                {
                    case RawTextureCompressMode.aRLE:
                        writePalette = false;

                        switch (_format)
                        {
                            case TextureFormat.ARGB555:
                            case TextureFormat.RGBA32: break;
                            default: res = RawTextureCompressMode.None; break;
                        }
                        break;
                    case RawTextureCompressMode.IdxaRLE:
                        switch (_format)
                        {
                            case TextureFormat.ARGB555:
                            case TextureFormat.RGBA32: break;
                            default: res = RawTextureCompressMode.IdxRLE; break;
                        }

                        if (res != RawTextureCompressMode.IdxaRLE) { break; }
                        switch (palMode)
                        {
                            case TextureFormat.RGBA32:
                            case TextureFormat.ARGB555:
                                palMode = TextureFormat.RGB24;
                                break;
                        }
                        break;
                }


                stamp.Start("Header Writing");
                hdr.WriteHeader(bw);

               //bw.Write((byte)_colorMode);
               //bw.Write((byte)res, 7);
               //bw.Write(_compressPixelData);
               //
               //bw.Write((ushort)Math.Min(_width, ushort.MaxValue));
               //bw.Write((ushort)Math.Min(_height, ushort.MaxValue));
                stamp.Stamp();

                //Write Uncompressed Raw Colors if compression mode is 0
                if (res == RawTextureCompressMode.None)
                {
                    //_compressPixelData &= (_width * _height) >= RawTextureDecoder.PIXEL_COMPRESS_THRESHOLD;
                    if (_compressPixelData)
                    {
                        stamp.Start("Huffman Compress pixel bytes");
                        //byte[] data = _pixels.ToBytes(PixelByteOrder.RGBA, false, _width, _height, _colorMode);
                        //Huffman.CompressToStream(bw, data, true, out byte bbb);
                        stamp.Stamp();
                    }
                    else
                    {
                        stamp.Start("Write Raw Pixel Data");
                       // bw.WriteColors(_pixels, _colorMode);
                        stamp.Stamp();
                    }
 
                    Console.WriteLine(stamp.ToString());
                    return ImageEncodeResult.Success;
                }
          
                Dictionary<FastColor, int> palette = new Dictionary<FastColor, int>();
                if (writePalette) 
                {
                    stamp.Start("Palette Gen");
                    //Generate palette and save it to the stream 
                    List<FastColor> paletteL = new List<FastColor>();
                    //foreach (var c in _pixels)
                    //{
                    //    if (palette.ContainsKey(c)) { continue; }
                    //    palette.Add(c, palette.Count);
                    //    paletteL.Add(c);
                    //}

                    int enc = Maths.Encode7Bit(paletteL.Count);
                    int dec = Maths.Decode7Bit(enc);

                    System.Diagnostics.Debug.Print($"TEST: {paletteL.Count} => {enc} => {dec}");

                    bw.Write7BitInt(paletteL.Count);
                    bw.WriteColors(paletteL, palMode);
                    stamp.Stamp();
                    paletteL.Clear();
                }
                List<int> paletteIndices = new List<int>();
                byte[] alph;

                //Compress the indices with RLE, Huffman Coding or both
                switch (res)
                {
                    case RawTextureCompressMode.aRLE:
                        stamp.Start("Alpha Splitting");
                        //alph = new byte[_pixels.Length];
                        //for (int i = 0; i < _pixels.Length; i++)
                        //{
                        //    alph[i] = _pixels[i].a;
                        //}
                        stamp.Stamp();

                        stamp.Start("Alpha RLE");
                      //  RLE.CompressToStream(bw, alph);
                        stamp.Stamp();

                       // bw.WriteColors(_pixels, TextureFormat.RGB24);
                        break;

                    case RawTextureCompressMode.IdxaRLE:
                        stamp.Start("Alpha Splitting");
                        //alph = new byte[_pixels.Length];
                        //for (int i = 0; i < _pixels.Length; i++)
                        //{
                        //    alph[i] = _pixels[i].a;
                        //}
                        stamp.Stamp();

                        stamp.Start("Alpha RLE");
                        //RLE.CompressToStream(bw, alph);
                        stamp.Stamp();

                        stamp.Start("RLE Index Compression");
                        //for (int i = 0; i < _pixels.Length; i++)
                        //{
                        //    paletteIndices.Add(palette[_pixels[i]]);
                        //}

                       // RLE.CompressToStream(bw, paletteIndices);
                        stamp.Stamp();
                        break;

                    case RawTextureCompressMode.IdxHuffman:
                        stamp.Start("Huffman Compression");
                        //for (int i = 0; i < _pixels.Length; i++)
                        //{
                        //    paletteIndices.Add(palette[_pixels[i]]);
                        //}

                        Huffman.CompressToStream(bw, paletteIndices, true, out byte padded);
                        stamp.Stamp();
                        break;

                    case RawTextureCompressMode.IdxRLE:
                        stamp.Start("RLE Compression");
                        //for (int i = 0; i < _pixels.Length; i++)
                        //{
                        //    paletteIndices.Add(palette[_pixels[i]]);
                        //}

                        RLE.CompressToStream(bw, paletteIndices);
                        stamp.Stamp();
                        break;

                    case RawTextureCompressMode.IdxRLEHuffman:

                        stamp.Start("RLE Index Gen");
                        List<int> indices = new List<int>();
                        //for (int i = 0; i < _pixels.Length; i++)
                        //{
                        //    indices.Add(palette[_pixels[i]]);
                        //}
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
                paletteIndices.Clear();
                return ImageEncodeResult.Success;
            }
        }

        protected override void ValidateFormat(ref TextureFormat format, ref byte bpp)
        {
            base.ValidateFormat(ref format, ref bpp);
            switch (format)
            {
                case TextureFormat.ARGB555:
                    format = TextureFormat.RGBA32;
                    _bpp = 32;
                    break;

                case TextureFormat.RGB555:
                case TextureFormat.RGB565:
                    format = TextureFormat.RGB24;
                    _bpp = 24;
                    break;

                case TextureFormat.OneBit:
                case TextureFormat.Indexed4:
                case TextureFormat.Indexed8:
                case TextureFormat.Grayscale:
                    format = TextureFormat.RGB24;
                    _bpp = 24;
                    break;
            }
        }
    }
}
