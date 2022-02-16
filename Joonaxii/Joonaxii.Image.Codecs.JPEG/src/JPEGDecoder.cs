using Joonaxii.Data;
using Joonaxii.IO;
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
        public JPEGDecoder(Stream stream) : base(stream)
        {
        }

        public JPEGDecoder(BinaryReader br, bool dispose) : base(br, dispose)
        {
        }

        public override ImageDecodeResult Decode(bool skipHeader)
        {
            ImageDecodeResult result = ImageDecodeResult.InvalidImageFormat;
            while (FindNextMarker(out var marker))
            {
                System.Diagnostics.Debug.Print($"Found Marker: [{marker}]");
                if (!ProcessMarker(marker, out result))
                {
                    break;
                }
            }
            return result;
        }

        private bool TryGetLength(out ushort length)
        {
            if(_stream.Position < _stream.Length - 2)
            {
                length = _br.ReadUInt16BigEndian();
                return true;
            }
            length = 0;
            return false;
        }

        private bool ProcessMarker(JPEGMarker marker, out ImageDecodeResult result)
        {
            result = ImageDecodeResult.Success;
            ushort len;
            switch (marker)
            {
                default:
                    if(!TryGetLength(out len))
                    {
                        result = ImageDecodeResult.DataCorrupted;
                        return false;
                    }
                    break;

                case JPEGMarker.SOF0:
                case JPEGMarker.SOF1:
                case JPEGMarker.SOF2:
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

                    break;

                case JPEGMarker.SOS:
                    if (!TryGetLength(out len))
                    {
                        result = ImageDecodeResult.DataCorrupted;
                        return false;
                    }
                    byte[] data = _br.ReadBytes(len - 2);


                    break;

                case JPEGMarker.DEF_RESTART_INT:

                    break;

                case JPEGMarker.DEF_QUANT:

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

                case JPEGMarker.EOI: return false;
            }

            return true;
        }

        private byte[] _privBuf = new byte[2];
        private bool FindNextMarker(out JPEGMarker marker)
        {
            marker = 0;
            while (_stream.Position < _stream.Length)
            {
                int c = _br.Read(_privBuf, 0, 2);
                if (c < 2) { return false; }

                byte b1 = _privBuf[0];
                byte b2 = _privBuf[1];

                if (b1 == (byte)JPEGMarker.Padding)
                {
                    switch ((JPEGMarker)b2)
                    {
                        case JPEGMarker.Padding:

                            continue;
                        case 0:

                            continue;
                    }

                    marker = (JPEGMarker)b2;
                    return true;
                }
            }
            return false;
        }

        public override void ValidateFormat()
        {
            switch (_colorMode)
            {
                default:
                    _colorMode = ColorMode.RGB24;
                    _bpp = 24;
                    break;
            }
        }
    }
}
