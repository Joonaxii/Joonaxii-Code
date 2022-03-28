using Joonaxii.Data;
using Joonaxii.Data.Coding;
using Joonaxii.Image.Texturing;
using Joonaxii.IO;
using Joonaxii.IO.BitStream;
using System;
using System.IO;
using System.Text;

namespace Joonaxii.Image.Codecs.BMP
{
    public class BMPDecoder : ImageDecoderBase
    {
        private bool _topToBottom;
        private byte _cmpMode;
        public BMPDecoder(Stream stream) : base(stream) { }
        public BMPDecoder(BinaryReader br, bool dispose) : base(br, dispose) { }

        public override ImageDecodeResult Decode(bool skipHeader)
        {
            if (!skipHeader)
            {
                var hdr = HeaderManager.GetFileType(_br, false);
                if (hdr != HeaderType.BMP) { return ImageDecodeResult.InvalidImageFormat; }
            }

            MemoryStream ms = null;
            BitReader br = _br as BitReader;
            bool isBitReader = br != null;

            if (!isBitReader)
            {
                ms = new MemoryStream();
                _br.BaseStream.CopyToWithPos(ms);
                br = new BitReader(ms);
            }

            var resHdr = LoadGeneralTextureInfo(br);
            if(resHdr != ImageDecodeResult.Success) { return resHdr; }

            TextureFormat mode;
            switch (_general.bitsPerPixel)
            {
                case 16:
                case 32:
                    int r = br.ReadInt32();
                    int g = br.ReadInt32();
                    int b = br.ReadInt32();
                    int a = br.ReadInt32();
                    mode = ImageCodecExtensions.GetColorMode(_general.bitsPerPixel, r, g, b, a);
                    break;

                default:
                    mode = ImageCodecExtensions.GetColorMode(_general.bitsPerPixel);
                    break;
            }

            GenerateTexture(_general.width, _general.height, mode, _general.bitsPerPixel);

            int padding = IOExtensions.NextDivBy(_texture.ScanSize, 4) - _texture.ScanSize;
            unsafe
            {
                for (int y = 0; y < _general.height; y++)
                {
                    int yP = _topToBottom ? y : _general.height - 1 - y;
                    int scan = yP * _texture.Width;
                    for (int x = 0; x < _general.width; x++)
                    {
                        _texture.SetPixel(scan + x, br.ReadColor(mode, true));
                    }

                    _stream.Seek(padding, SeekOrigin.Current);
                }
            }
            return ImageDecodeResult.Success;
        }

        public override int GetDataCRC(long pos)
        {
            LoadGeneralInformation(pos);

            long posSt = _stream.Position;
            _stream.Seek(pos, SeekOrigin.Begin);
            int crc = 0;

            switch (_general.bitsPerPixel)
            {
                case 16:
                case 32:
                    _stream.Seek(4, SeekOrigin.Current);
                    break;
            }

            int scanSize = (_general.bitsPerPixel >> 3) * _general.width;
            int padding = IOExtensions.NextDivBy(scanSize, 4) - scanSize;

            int bytesToRead = (scanSize + padding) * _general.height;
            crc = (int)CRC.Calculate(_stream, bytesToRead);

            _stream.Seek(posSt, SeekOrigin.Begin);
            return crc;
        }

        public override void LoadGeneralInformation(long pos)
        {
            if(_general != GeneralTextureInfo.Zero) { return; }
            long posSt = _stream.Position;
            _stream.Seek(pos, SeekOrigin.Begin);
            using(BinaryReader br = new BinaryReader(_stream, Encoding.UTF8, true))
            {
                LoadGeneralTextureInfo(br);
            }
            _stream.Seek(posSt, SeekOrigin.Begin);
        }

        protected override ImageDecodeResult LoadGeneralTextureInfo(BinaryReader br)
        {
            int size = br.ReadInt32();
            int res = br.ReadInt32();
            int dataOffset = br.ReadInt32();

            int hdrize = br.ReadInt32();

            int width = Math.Abs(br.ReadInt32());
            int height = br.ReadInt32();

            _topToBottom = height < 0;
            height = _topToBottom ? -height : height;

            ushort planes = br.ReadUInt16();
            byte bpp = (byte)br.ReadUInt16();

            byte cmpMode = (byte)br.ReadInt32();

            System.Diagnostics.Debug.Print($"BMP Info: {width} W, {height} H, Rev Y: {_topToBottom}, BPP: {bpp}, Cmp: {cmpMode}");
            switch (_cmpMode)
            {
                default: return ImageDecodeResult.NotSupported;
                case 0: break; //None
                case 1: break; //8 Bit RLE
                case 3: break; //Bit Fields
            }
            int imgSize = br.ReadInt32();

            int pxXPerM = br.ReadInt32();
            int pxYPerM = br.ReadInt32();

            int colors = br.ReadInt32();
            int impColors = br.ReadInt32();

            return ImageDecodeResult.Success;
        }
    }
}
