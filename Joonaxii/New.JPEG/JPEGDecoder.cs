using Joonaxii.Image;
using Joonaxii.Image.Codecs;
using Joonaxii.IO;
using Joonaxii.MathJX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace New.JPEG
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

        public const int IDCT_PRECISION = 8;
        private const double SQRT_OF_TWO = 1.41421356237;
        private const double NORM_COEFF = 1.0 / SQRT_OF_TWO;
        private static readonly short[] ZIG_ZAG = new short[64]
        {
            0,  1,  8,  16,  9,  2,  3, 10,
            17, 24, 32, 25, 18, 11,  4,  5,
            12, 19, 26, 33, 40, 48, 41, 34,
            27, 20, 13,  6,  7, 14, 21, 28,
            35, 42, 49, 56, 57, 50, 43, 36,
            29, 22, 15, 23, 30, 37, 44, 51,
            58, 59, 52, 45, 38, 31, 39, 46,
            53, 60, 61, 54, 47, 55, 62, 63,
        };

        private JPEGMode _mode;
        private byte[] _luminanceTable = new byte[CHUNK_SIZE];
        private byte[] _chrominanceTable = new byte[CHUNK_SIZE];

        private HuffmanTable[] _huffmanTables;

        private List<JPEGSample> _quantMapping = new List<JPEGSample>();

        public JPEGDecoder(Stream stream) : base(stream)
        {
            _luminanceTable = DEFAULT_LUMINANCE_TABLE;
            _chrominanceTable = DEFAULT_CHROMINANCE_TABLE;

            _mode = JPEGMode.Baseline;

            _huffmanTables = new HuffmanTable[32];
        }

        public override void ValidateFormat()
        {
            switch (_colorMode)
            {
                case ColorMode.RGB24: return;
                default:
                    _colorMode = ColorMode.RGB24;
                    _bpp = 24;
                    break;
            }
        }

        public FastColor ColorConversion(double y, double cr, double cb)
        {
            float r = (float)(cr * (2 - 2 * 0.299) + y);
            float b = (float)(cb * (2 - 2 * 0.114) + y);
            float g = (float)((y - 0.114 * b - 0.299 * r) / 0.587);

            return new FastColor(
                (byte)Maths.Clamp(r + 128, 0, 255),
                (byte)Maths.Clamp(g + 128, 0, 255),
                (byte)Maths.Clamp(b + 128, 0, 255));
        }

        private int DecodeNumber(int code, int bits)
        {
            var l = (int)Math.Pow(2, code - 1);
            if (bits >= l) { return bits; }
            return bits - (2 * l - 1);
        }

        private void BuildMatrix(BitStream st, int id, byte[] quant, IDCT mat, ref int coeff)
        {
            mat.Reset();

            var code = _huffmanTables[id].GetCode(st);
            var bits = st.GetBitN(code);

            coeff += DecodeNumber(code, bits);
            mat.AddZigZag(0, coeff * quant[0]);

            var l = 1;
            while (l < 64)
            {
                code = _huffmanTables[16 + id].GetCode(st);
                if (code == 0) { break; }

                if (code > 15)
                {
                    l += (code >> 4);
                    code = code & 0xF;
                }

                bits = st.GetBitN(code);
                if (l < 64)
                {
                    var oCoeff = DecodeNumber(code, bits);
                    mat.AddZigZag(l, oCoeff * quant[l]);
                    l++;
                }
            }
        }

        public override ImageDecodeResult Decode(bool skipHeader)
        {
            _quantMapping.Clear();

            var res = ImageDecodeResult.InvalidImageFormat;
            while (_stream.Length - _stream.Position > 1)
            {
                JPEGMarker marker = (JPEGMarker)_br.ReadUInt16BigEndian();
                if (marker == JPEGMarker.EOI) { return ImageDecodeResult.Success; }
                if (marker == JPEGMarker.Padding) { continue; }

                if ((((int)marker >> 8) & 0xFF) != 0xFF) { continue; }


                System.Diagnostics.Debug.Print($"{marker}");
                ushort len = _br.ReadUInt16BigEndian();
                switch (marker)
                {
                    case JPEGMarker.SOF0:
                    case JPEGMarker.SOF1:
                    case JPEGMarker.SOF2:
                        res = ImageDecodeResult.Success;

                        byte m = _br.ReadByte();
                        _height = _br.ReadUInt16BigEndian();
                        _width = _br.ReadUInt16BigEndian();

                        byte c = _br.ReadByte();

                        for (int i = 0; i < c; i++)
                        {
                            ushort us = _br.ReadUInt16();
                            ushort bb = _br.ReadByte();
                            _quantMapping.Add(new JPEGSample((uint)(us + (bb << 16))));
                        }
                        _pixels = new FastColor[_width * _height];
                        break;

                    case JPEGMarker.SOS:
                        _br.ReadBytes(len - 2);
                        BitStream st = null;
                        byte[] buf = new byte[1];
                        using (MemoryStream ms = new MemoryStream())
                        {
                            while (_stream.Position < _stream.Length)
                            {
                                byte bA = _br.ReadByte();
                                if (bA != 0xFF)
                                {
                                    buf[0] = bA;
                                    ms.Write(buf, 0, 1);
                                    continue;
                                }
                                if (_stream.Position >= _stream.Length) { break; }
                                byte bB = _br.ReadByte();

                                if (bB == 0)
                                {
                                    buf[0] = bA;
                                    ms.Write(buf, 0, 1);
                                    continue;
                                }
                                break;
                            }

                            ms.Flush();
                            ms.Seek(0, SeekOrigin.Begin);
                            st = new BitStream(ms.ToArray());
                        }

                        int oldLumC = 0;
                        int oldCbdC = 0;
                        int oldCrdC = 0;

                        IDCT lum = new IDCT();
                        IDCT crd = new IDCT();
                        IDCT cbd = new IDCT();

                        for (int y = 0; y < _height >> 3; y++)
                        {
                            int yP = y * 8;
                            for (int x = 0; x < _width >> 3; x++)
                            {
                                BuildMatrix(st, 0, GetQuant(_quantMapping[0].index), lum, ref oldLumC);
                                BuildMatrix(st, 1, GetQuant(_quantMapping[1].index), crd, ref oldCrdC);
                                BuildMatrix(st, 1, GetQuant(_quantMapping[2].index), cbd, ref oldCbdC);

                                int xP = x * 8;
                                for (int yy = 0; yy < 8; yy++)
                                {
                                    int yyP = yy * 8;
                                    int yPP = (yP + yy) * _width;
                                    for (int xx = 0; xx < 8; xx++)
                                    {
                                        int iI = yyP + xx;
                                        _pixels[(xP + xx) + yPP] =
                                            ColorConversion(lum.baseData[iI], cbd.baseData[iI], crd.baseData[iI]);
                                    }
                                }
                            }
                        }

                        byte[] GetQuant(int i)
                        {
                            switch (i)
                            {
                                default: return _luminanceTable;
                                case 1: return _chrominanceTable;
                            }
                        }

                        break;

                    case JPEGMarker.DEF_QUANT:
                        int q = _br.ReadByte();
                        int qMode = q & 0xF;

                        if (qMode == 0)
                        {
                            _luminanceTable = _br.ReadBytes(64);
                        }
                        else { _chrominanceTable = _br.ReadBytes(64); }
                        break;

                    case JPEGMarker.DEF_HUFF:
                        byte huffHdr = _br.ReadByte();
                        byte[] lengths = _br.ReadBytes(16);

                        List<byte> elements = new List<byte>();
                        foreach (var item in lengths)
                        {
                            if (item == 0) { continue; }
                            elements.AddRange(_br.ReadBytes(item));
                        }
                        _huffmanTables[huffHdr] = new HuffmanTable(lengths, elements.ToArray());
                        break;
                }

            }
            return res;
        }

        public class IDCT
        {
            public double[] baseData = new double[64];

            public void Reset()
            {
                for (int i = 0; i < baseData.Length; i++)
                {
                    baseData[i] = 0;
                }
            }

            public double NormCoeff(int n)
            {
                return n == 0 ? 0.35355339059 : 0.5;
            }

            public void AddIDC(int n, int m, int coeff)
            {
                var an = NormCoeff(n);
                var am = NormCoeff(m);

                for (int y = 0; y < 8; y++)
                {
                    int yP = y * 8;
                    for (int x = 0; x < 8; x++)
                    {
                        var nn = an * Math.Cos(n * Math.PI * (x + 0.5) / 8.0);
                        var mm = am * Math.Cos(m * Math.PI * (x + 0.5) / 8.0);
                        baseData[yP + x] += nn * mm * coeff;
                    }
                }
            }

            public void AddZigZag(int zi, int coeff)
            {
                var i = ZIG_ZAG[zi];
                var n = i & 0x7;
                var m = i >> 3;
                AddIDC(n, m, coeff);
            }
        }

        public class BitStream
        {
            public bool CanRead { get => (_bitPos >> 3) < _data.Length; }

            private byte[] _data;
            private int _bitPos;
            public BitStream(byte[] dat)
            {
                _data = dat;
                _bitPos = 0;
            }

            public int GetBit()
            {
                var b = _data[_bitPos >> 3];
                var s = 7 - (_bitPos & 0x7);

                _bitPos++;
                return (b >> s) & 0x1;
            }

            public int GetBitN(int code)
            {
                int val = 0;
                for (int i = 0; i < code; i++)
                {
                    val = val * 2 + GetBit();
                }
                return val;
            }
        }
    }

    public enum JPEGMarker
    {
        Padding = 0xFF00,

        SOI = 0xFFD8,

        App0 = 0xFFE0,
        App1 = 0xFFE1,
        App2 = 0xFFE2,
        App3 = 0xFFE3,
        App4 = 0xFFE4,
        App5 = 0xFFE5,
        App6 = 0xFFE6,
        App7 = 0xFFE7,
        App8 = 0xFFE8,
        App9 = 0xFFE9,
        App10 = 0xFFEA,
        App11 = 0xFFEB,
        App12 = 0xFFEC,
        App13 = 0xFFED,
        App14 = 0xFFEE,
        App15 = 0xFFEF,

        SOF0 = 0xFFC0,
        SOF1 = 0xFFC1,
        SOF2 = 0xFFC2,
        SOF3 = 0xFFC3,

        SOF5 = 0xFFC5,
        SOF6 = 0xFFC6,
        SOF7 = 0xFFC7,
        SOF8 = 0xFFC8,
        SOF9 = 0xFFC9,
        SOF10 = 0xFFCA,
        SOF11 = 0xFFCB,

        SOF13 = 0xFFCD,
        SOF14 = 0xFFCE,
        SOF15 = 0xFFCF,

        DEF_HUFF = 0xFFC4,
        DEF_AR_COD = 0xFFCC,

        DEF_QUANT = 0xFFDB,
        DEF_NUM_OF_LINES = 0xFFDC,
        DEF_RESTART_INT = 0xFFDD,

        SOS = 0xFFDA,

        DEF_RESTART_0 = 0xFFD0,
        DEF_RESTART_1 = 0xFFD1,
        DEF_RESTART_2 = 0xFFD2,
        DEF_RESTART_3 = 0xFFD3,
        DEF_RESTART_4 = 0xFFD4,
        DEF_RESTART_5 = 0xFFD5,
        DEF_RESTART_6 = 0xFFD6,
        DEF_RESTART_7 = 0xFFD7,

        COMMENT = 0xFFFE,
        EOI = 0xFFD9,
    }
}
