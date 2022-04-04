using Joonaxii.Data;
using Joonaxii.Data.Coding;
using Joonaxii.IO;
using Joonaxii.IO.BitStream;
using Joonaxii.MathJX;
using Joonaxii.Pooling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.Image.Codecs.JPEG
{
    public class JPEGDecoder : ImageDecoderBase
    {
        public const int CHUNK_SIZE = 8 * 8;

        public static byte[] DEFAULT_LUMINANCE_TABLE
        {
            get => new byte[CHUNK_SIZE] {
            16, 11, 10, 16, 24,  40,  51,  61,
            12, 12, 14, 19, 26,  58,  60,  55,
            14, 13, 16, 24, 40,  57,  69,  56,
            14, 17, 22, 29, 51,  87,  80,  62,
            18, 22, 37, 56, 68,  109, 103, 77,
            24, 36, 55, 64, 81,  104, 113, 92,
            49, 64, 78, 87, 103, 121, 120, 101,
            72, 92, 95, 98, 112, 100, 103, 99
        };
        }

        public static byte[] DEFAULT_CHROMINANCE_TABLE
        {
            get => new byte[CHUNK_SIZE] {
            17, 18, 24, 47, 99, 99, 99, 99,
            18, 21, 26, 66, 99, 99, 99, 99,
            24, 26, 56, 99, 99, 99, 99, 99,
            47, 66, 99, 99, 99, 99, 80, 99,
            99, 99, 99, 99, 99, 99, 99, 99,
            99, 99, 99, 99, 99, 99, 99, 99,
            99, 99, 99, 99, 99, 99, 99, 99,
            99, 99, 99, 99, 99, 99, 99, 99
          };
        }

        private JPEGMode _mode;

        private List<byte> _quantMapping = new List<byte>();

        private byte[] _luminanceTable = new byte[CHUNK_SIZE];
        private byte[] _chrominanceTable = new byte[CHUNK_SIZE];

        private List<JPEGHuffmanNode> _huffmanTablesAC;
        private List<JPEGHuffmanNode> _huffmanTablesDC;

        private static GenericObjectPool<IDCT> _idctPool;

        static JPEGDecoder()
        {
            _idctPool = new GenericObjectPool<IDCT>(6, () => { return new IDCT(); });
        }

        public JPEGDecoder(Stream stream) : this(new BinaryReader(stream, Encoding.UTF8, true), true)
        {
        }

        private JPEGDecoder(BinaryReader br, bool dispose) : base(br, dispose)
        {
            _luminanceTable = DEFAULT_LUMINANCE_TABLE;
            _chrominanceTable = DEFAULT_CHROMINANCE_TABLE;

            _mode = JPEGMode.Baseline;
            _huffmanTablesAC = new List<JPEGHuffmanNode>();
            _huffmanTablesDC = new List<JPEGHuffmanNode>();
        }

        public override ImageDecodeResult Decode(bool skipHeader)
        {
            _mode = JPEGMode.Baseline;
            _huffmanTablesAC.Clear();
            _huffmanTablesDC.Clear();

            ImageDecodeResult result = ImageDecodeResult.InvalidImageFormat;
            JPEGMarker marker;
            while (_stream.Position < _stream.Length)
            {
                marker = (JPEGMarker)_br.ReadUInt16BigEndian();
                if (marker == JPEGMarker.Padding | ((int)marker >> 8) != 0xFF) { continue; }
                if (!ProcessMarker(marker, ref result)) { break; }
            }
            return result;
        }

        private static bool TryGetLength(Stream stream, BinaryReader br, out ushort length)
        {
            if (stream.Position < stream.Length - 2)
            {
                length = br.ReadUInt16BigEndian();
                return true;
            }
            length = 0;
            return false;
        }

        private byte[] GetQuantTable(int i)
        {
            switch (i)
            {
                default: return _luminanceTable;
                case 0: return _luminanceTable;
                case 1: return _chrominanceTable;
            }
        }

        private bool ProcessMarker(JPEGMarker marker, ref ImageDecodeResult result)
        {
            var prev = result;
            result = ImageDecodeResult.Success;
            ushort len;
            byte[] data;
            switch (marker)
            {
                default: result = prev; break;
                case JPEGMarker.SOF0:
                case JPEGMarker.SOF1:
                case JPEGMarker.SOF2:
                    if (!TryGetLength(_stream, _br, out len))
                    {
                        result = ImageDecodeResult.DataCorrupted;
                        return false;
                    }

                    switch (marker)
                    {
                        default: _mode = JPEGMode.Baseline; break;
                        case JPEGMarker.SOF2: _mode = JPEGMode.Progressive; break;
                    }

                    byte precision = _br.ReadByte();
                    _general.height = _br.ReadUInt16BigEndian();
                    _general.width = _br.ReadUInt16BigEndian();
                    byte components = _br.ReadByte();

                    //_pixels = new FastColor[_width * _height];
                    var bpp = 24;
                    uint[] cmp = new uint[components];
                    for (int i = 0; i < components; i++)
                    {
                        cmp[i] = (uint)(_br.ReadUInt16() + (_br.ReadByte() << 16));
                    }

                    _general.bitsPerPixel = (byte)(components * precision);
                    System.Diagnostics.Debug.Print($"{marker}-->: ");
                    System.Diagnostics.Debug.Print($"   -JPEG Mode: {_mode}");
                    System.Diagnostics.Debug.Print($"   -Precision: {precision}");
                    System.Diagnostics.Debug.Print($"   -Width: {_texture.Width}");
                    System.Diagnostics.Debug.Print($"   -Height: {_texture.Height}");
                    System.Diagnostics.Debug.Print($"   -Components: {components}");
                    for (int i = 0; i < components; i++)
                    {
                        var v = cmp[i];

                        var a = (byte)(v & 0xFF);
                        var b = (byte)((v >> 8) & 0xFF);
                        var c = (byte)((v >> 16) & 0xFF);
                        System.Diagnostics.Debug.Print($"       -CMP #{i}: {a} :: 0x{Convert.ToString(b, 16).PadLeft(2, '0')} || {c}");
                        _quantMapping.Add(c);
                    }

                    result = ImageDecodeResult.Success;
                    return false;
                    break;

                case JPEGMarker.SOF3:
                case JPEGMarker.SOF5:
                case JPEGMarker.SOF6:
                case JPEGMarker.SOF7:
                case JPEGMarker.SOF8:
                case JPEGMarker.SOF9:
                case JPEGMarker.SOF10:
                case JPEGMarker.SOF11:
                case JPEGMarker.SOF13:
                case JPEGMarker.SOF14:
                case JPEGMarker.SOF15:
                    if (!TryGetLength(_stream, _br, out len))
                    {
                        result = ImageDecodeResult.DataCorrupted;
                        return false;
                    }
                    result = ImageDecodeResult.NotSupported;
                    return false;

                case JPEGMarker.SOS:
                    if (!TryGetLength(_stream, _br, out len))
                    {
                        result = ImageDecodeResult.DataCorrupted;
                        return false;
                    }

                    _br.ReadBytes(len - 2);

                    long lenMark = 0;
                    using(MemoryStream ms = new MemoryStream())
                    using(BitReader br = new BitReader(ms))
                    {
                        GetLengthToNextMarker(ms);
                        lenMark = ms.Length;

                        ms.Seek(0, SeekOrigin.Begin);
                        int oldLumCoe = 0, oldCbdCoe = 0, oldCrdCoe = 0;

                        IDCT matLum = _idctPool.Get();
                        IDCT matCr = _idctPool.Get();
                        IDCT matCb = _idctPool.Get();
                        //for (int y = 0; y < _height >> 3; y++)
                        //{
                        //    for (int x = 0; x < _width >> 3; x++)
                        //    {
                        //        BuildMatrix(br, 0, GetQuantTable(_quantMapping[0]), oldLumCoe, matLum, out oldLumCoe);
                        //        BuildMatrix(br, 1, GetQuantTable(_quantMapping[1]), oldCrdCoe, matCr, out oldCrdCoe);
                        //        BuildMatrix(br, 1, GetQuantTable(_quantMapping[2]), oldCbdCoe, matCb, out oldCbdCoe);

                        //        ApplyMatrix(x, y, matLum, matCb, matCr);

                        //    }
                        //}

                        _idctPool.Return(matLum);
                        _idctPool.Return(matCr);
                        _idctPool.Return(matCb);
                    }

                    System.Diagnostics.Debug.Print($"Found SOS Marker: [{len}] {lenMark} bytes");
                    break;

                case JPEGMarker.DEF_RESTART_INT:
                    System.Diagnostics.Debug.Print($"Found Marker: [{marker}]");
                    break;

                case JPEGMarker.DEF_HUFF:
                    if (!TryGetLength(_stream, _br, out len))
                    {
                        result = ImageDecodeResult.DataCorrupted;
                        return false;
                    }
                    byte flgs = _br.ReadByte();
                    bool isAC = Maths.GetRange(flgs, 4, 4) > 0;
                    byte tableNum = Maths.GetRange(flgs, 0, 4);

                    byte[] numOfSymb = new byte[16];
              
                    _br.Read(numOfSymb, 0, 16);

                    List<byte>[] symbols = new List<byte>[16];
                    List<byte> allSymbols = new List<byte>();

                    for (int i = 0; i < numOfSymb.Length; i++)
                    {
                        List<byte> symbolsL = new List<byte>();
                        for (int j = 0; j < numOfSymb[i]; j++)
                        {
                            byte symb = _br.ReadByte();
                            symbolsL.Add(symb);
                            allSymbols.Add(symb);
                        }
                        symbols[i] = symbolsL;
                    }
              
                    JPEGHuffmanNode root = new JPEGHuffmanNode();

                    root.left = new JPEGHuffmanNode(root);
                    root.right = new JPEGHuffmanNode(root);

                    var leftMost = root.left;
                    for (int i = 0; i < 16; i++)
                    {
                        JPEGHuffmanNode cur;
                        if (numOfSymb[i] == 0)
                        {
                            cur = leftMost;
                            while (cur != null)
                            {
                                cur.left = cur.left == null ? new JPEGHuffmanNode(cur) : cur.left;
                                cur.right = cur.right == null ? new JPEGHuffmanNode(cur) : cur.right;

                                cur = JPEGHuffmanNode.GetRightLevelNode(cur);
                            }
                            leftMost = leftMost.left;
                            continue;
                        }

                        foreach (var symbol in symbols[i])
                        {
                            leftMost.value = symbol;
                            leftMost = JPEGHuffmanNode.GetRightLevelNode(leftMost);
                        }

                        leftMost.left = new JPEGHuffmanNode(leftMost);
                        leftMost.right = new JPEGHuffmanNode(leftMost);
                        cur = JPEGHuffmanNode.GetRightLevelNode(leftMost);
                        leftMost = leftMost.left;

                        while (cur != null)
                        {
                            cur.left = cur.left == null ? new JPEGHuffmanNode(cur) : cur.left;
                            cur.right = cur.right == null ? new JPEGHuffmanNode(cur) : cur.right;

                            cur = JPEGHuffmanNode.GetRightLevelNode(cur);
                        }
                    }

                    (isAC ? _huffmanTablesAC : _huffmanTablesDC).Add(root);
                    System.Diagnostics.Debug.Print($"Huffman Info: {tableNum}, {(isAC ? "AC" : "DC")} [0b{Convert.ToString(flgs, 2).PadLeft(8, '0').Insert(4, "-")}, {flgs}, {allSymbols.Count} symbols]");
                    break;

                case JPEGMarker.DEF_QUANT:
                    if (!TryGetLength(_stream, _br, out len))
                    {
                        result = ImageDecodeResult.DataCorrupted;
                        return false;
                    }
                    byte qFlg = _br.ReadByte();
                    byte mode = Maths.GetRange(qFlg, 0, 4);
                    byte prec = Maths.GetRange(qFlg, 4, 4);

                    Buffer.BlockCopy(_br.ReadBytes(64), 0, (mode == 0 ? _luminanceTable : _chrominanceTable), 0, 64);
                    System.Diagnostics.Debug.Print($"Defining Quantization Table '{(mode == 0 ? "Luminance" : "Chrominance")}', {(prec + 1) << 3} bit");
                    break;

                case JPEGMarker.SOI:
                case JPEGMarker.DEF_RESTART_0:
                case JPEGMarker.DEF_RESTART_1:
                case JPEGMarker.DEF_RESTART_2:
                case JPEGMarker.DEF_RESTART_3:
                case JPEGMarker.DEF_RESTART_4:
                case JPEGMarker.DEF_RESTART_5:
                case JPEGMarker.DEF_RESTART_6:
                case JPEGMarker.DEF_RESTART_7:
                    break;

                case JPEGMarker.EOI: result = ImageDecodeResult.Success; return false;
            }

            return true;
        }

        public static bool IsJPEG(Stream stream, out GeneralTextureInfo header)
        {
            header = new GeneralTextureInfo(0, 0, 0);
            using (BinaryReader br = new BinaryReader(stream, Encoding.UTF8, true))
            {
                ushort len;
                while (stream.Position < stream.Length)
                {
                    var diff = stream.Length - stream.Position;
                    if(diff < 2) { break; }

                    byte mA = br.ReadByte();
                    if(mA != 0xFF) { continue; }

                    mA = br.ReadByte();
                    switch (mA)
                    {
                        default: continue;
                        case 0xC0:
                        case 0xC1:
                        case 0xC2:
                        case 0xC3:
                        case 0xC5:
                        case 0xC6:
                        case 0xC7:
                        case 0xC8:
                        case 0xC9:
                        case 0xCA:
                        case 0xCB:
                        case 0xCD:
                        case 0xCE:
                        case 0xCF:
                            break;
                    }

                    if (!TryGetLength(stream, br, out len)) { continue; }

                    byte precision = br.ReadByte();
                    header.height = br.ReadUInt16BigEndian();
                    header.width = br.ReadUInt16BigEndian();
                    byte components = br.ReadByte();

                    header.bitsPerPixel = (byte)(components * precision);
                    return true;
                }
            }
            return false;
        }

        private FastColor ColorConversion(double y, double cR, double cB)
        {
            float r = (float)(cR * (2 - 2 * 0.299) + y);
            float b = (float)(cB * (2 - 2 * 0.114) + y);
            float g = (float)((y - 0.144 * b - 0.299 * r) / 0.587);

            r += 128;
            g += 128;
            b += 128;

            return new FastColor(
                (byte)(Maths.Clamp(r, 0, 255)),
                (byte)(Maths.Clamp(g, 0, 255)),
                (byte)(Maths.Clamp(b, 0, 255)));
        }

        private void ApplyMatrix(int x, int y, IDCT matL, IDCT matCb, IDCT matCr)
        {
            //for (int yy = 0; yy < 8; yy++)
            //{
            //    int y1 = (y + yy);
            //    if(y1 >= _height) { break; }
            //    int yP = yy * 8;
            //    int y1P = y1 * _width;
            //    for (int xx = 0; xx < 8; xx++)
            //    {
            //        int x1 = (x + xx);
            //        if(x1 >= _width) { continue; }

            //        int i = yP + xx;
            //        int iI = y1P + x1;
            //        _pixels[iI] = ColorConversion(matL.baseValues[i], matCr.baseValues[i], matCb.baseValues[i]);
            //    }
            //}
        }

        private int DecodeNum(byte code, int bits)
        {
            int l = (int)Math.Pow(2, code - 1);
            return bits >= l ? bits : bits - (2 * l - 1);
        }

        private int GetBitN(BitReader bit, byte l)
        {
            int val = 0;
            for (int i = 0; i < l; i++)
            {
                val = val * 2 + (bit.ReadBoolean() ? 1 : 0);
            }
            return val;
        }

        private void BuildMatrix(BitReader br, int id, byte[] quant, int oldCoeff, IDCT mat, out int outCoeff)
        {
            mat.Reset();

            byte code = _huffmanTablesDC[id].Read(br);
            int bits = GetBitN(br, code);
            outCoeff = DecodeNum(code, bits) + oldCoeff;

            mat.baseValues[0] = outCoeff * quant[0];
            int l = 1;

            while(l < 64)
            {
                code = _huffmanTablesAC[id].Read(br);
                if(code == 0) { break; }

                if(code > 15)
                {
                    l += code >> 4;
                    code &= 0x0F;
                }
                bits = GetBitN(br, code);

                if(l < 64)
                {
                    var coeff = DecodeNum(code, bits);
                    mat.baseValues[l] = coeff * quant[l];
                    l++;
                }
            }

              mat.RearrangeZigZag();
                mat.Run();
        }

        private const int SEARCH_BUFFER_SIZE = 8192;
        private byte[] _searchBuffer = new byte[SEARCH_BUFFER_SIZE];

        private void GetLengthToNextMarker(MemoryStream ms)
        {
            int bufPos;
            int bufLen;
            byte[] buf = new byte[1];
            while (_stream.Position < _stream.Length)
            {
                bufPos = 0;
                bufLen = _br.Read(_searchBuffer, 0, SEARCH_BUFFER_SIZE);

                while(bufPos < bufLen)
                {
                    byte bA = _searchBuffer[bufPos++];
                    if (bA != 0xFF)
                    {
                        buf[0] = bA;
                        ms.Write(buf, 0, 1);
                        if (_stream.Position >= _stream.Length & bufPos >= bufLen) { return; }
                        continue;
                    }
                    if(bufPos >= bufLen) 
                    {
                        buf[0] = bA;
                        ms.Write(buf, 0, 1);
                        break; 
                    }

                    byte bB = _searchBuffer[bufPos++];
                    if (bB == 0)
                    {
                        buf[0] = bA;
                        ms.Write(buf, 0, 1);
                        if (_stream.Position >= _stream.Length & bufPos >= bufLen) { return; }
                        continue;
                    }
                    return;
                }
            }
        }

        protected override ImageDecodeResult LoadGeneralTextureInfo(BinaryReader br)
        {
            return ImageDecodeResult.NotSupported;
        }
    }
}
