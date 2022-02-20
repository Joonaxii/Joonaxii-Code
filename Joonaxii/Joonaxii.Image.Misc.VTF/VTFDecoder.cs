using Joonaxii.Image.Codecs;
using Joonaxii.Image.Codecs.DXT;
using Joonaxii.MathJX;
using System;
using System.Collections.Generic;
using System.IO;

namespace Joonaxii.Image.Misc.VTF
{
    public class VTFDecoder : ImageDecoderBase
    {
        public int ThumbnailWidth { get; private set; }
        public int ThumbnailHeight { get; private set; }

        public bool HasThumbnail { get; private set; }

        private FastColor[] _thumb = new FastColor[0];

        public VTFDecoder(Stream stream) : base(stream)
        {
        }

        public override ImageDecodeResult Decode(bool skipHeader)
        {
            VTFTag tagHdr = (VTFTag)_br.ReadUInt32();
            if(tagHdr != VTFTag.VTF_HEADER) { return ImageDecodeResult.InvalidImageFormat; }

            int versionA = _br.ReadInt32();
            int versionB = _br.ReadInt32();
            double version = versionA + (versionB / 10.0);

            uint headerSize = _br.ReadUInt32();

            _width = _br.ReadUInt16();
            _height = _br.ReadUInt16();

            VTFFlags flags = (VTFFlags)_br.ReadUInt32();

            ushort frames = _br.ReadUInt16();
            ushort firstFrame = _br.ReadUInt16();

            _br.ReadUInt32(); //Padding0
            Vector3 rVec = new Vector3(_br.ReadSingle(), _br.ReadSingle(), _br.ReadSingle());
            _br.ReadUInt32(); //Padding1

            float bumpScale = _br.ReadSingle();

            VTFFormat hiResFmt = (VTFFormat)_br.ReadInt32();
            byte mipCount = _br.ReadByte();

            VTFFormat lowResFmt = (VTFFormat)_br.ReadInt32();
            ThumbnailWidth  = _br.ReadByte();
            ThumbnailHeight = _br.ReadByte();

            ushort depth = 8;
            if(version >= 7.2)
            {
                depth = _br.ReadUInt16();
            }

            uint resourceCount = 0;
            if (version >= 7.3)
            {
                _br.ReadBytes(3); //Padding2
                resourceCount = _br.ReadUInt32();
            }
            _br.ReadUInt64(); //Padding3

            System.Diagnostics.Debug.Print($"VTF Version: {version}[{versionA}.{versionB}]");
            System.Diagnostics.Debug.Print($"Header Size: {headerSize} bytes");
            System.Diagnostics.Debug.Print($"Width: {_width} px");
            System.Diagnostics.Debug.Print($"Height: {_height} px");
            System.Diagnostics.Debug.Print($"Flags: {flags}");
            System.Diagnostics.Debug.Print($"Frames: {firstFrame}/{frames}");

            System.Diagnostics.Debug.Print($"\nReflectivity Vector: {rVec}");
            System.Diagnostics.Debug.Print($"Bumpmap Scale: {bumpScale}");

            System.Diagnostics.Debug.Print($"\nHigh Res Format: {hiResFmt}");
            System.Diagnostics.Debug.Print($"Mip Count: {mipCount}");

            System.Diagnostics.Debug.Print($"\nLow Res Format: {lowResFmt}");
            System.Diagnostics.Debug.Print($"Low Res Width: {ThumbnailWidth}");
            System.Diagnostics.Debug.Print($"Low Res Height: {ThumbnailHeight}");

            System.Diagnostics.Debug.Print($"\nDepth: {depth}");
            System.Diagnostics.Debug.Print($"Resources: {resourceCount}");
            System.Diagnostics.Debug.Print($"HDR: {_stream.Position-12}");


            VTFResource[] resources = new VTFResource[resourceCount];
            if (version >= 7.3)
            {
                //Resource Reading
                for (int i = 0; i < resourceCount; i++)
                {
                    long pos = _stream.Position + 8;
                    var res = resources[i] = new VTFResource(_br.ReadUInt32(), _br.ReadUInt32());
                    System.Diagnostics.Debug.Print($"   -Found Resource: {res}");

                    _stream.Seek(res.Offset, SeekOrigin.Begin);
                    switch (res.Tag)
                    {
                        case VTFTag.HI_RES_IMAGE:
                            _pixels = new FastColor[_width * _height];
                            switch (hiResFmt)
                            {
                                case VTFFormat.DXT1:
                                    for (int m = 1; m < mipCount; m++)
                                    {
                                        BC1Decoder.SeekPast(_stream, _width / (1 << m), _height / (1 << m));
                                    }
                                    BC1Decoder.Decode(_br, _pixels, _width, _height);
                                    break;
                            }
                            break;
                        case VTFTag.LOW_RES_THUMB:
                            _thumb = new FastColor[ThumbnailWidth * ThumbnailHeight];
                            HasThumbnail = true;
                            switch (lowResFmt)
                            {
                                case VTFFormat.DXT1:
                                    for (int m = 1; m < mipCount; m++)
                                    {
                                        BC1Decoder.SeekPast(_stream, ThumbnailWidth / (1 << m), ThumbnailHeight / (1 << m));
                                    }
                                    BC1Decoder.Decode(_br, _thumb, ThumbnailWidth, ThumbnailHeight);
                                    break;
                            }
                            break;
                    }
                    _stream.Seek(pos, SeekOrigin.Begin);
                }
                return ImageDecodeResult.Success;
            }

            _stream.Seek(headerSize + 16, SeekOrigin.Begin);
            _pixels = new FastColor[_width * _height];
            switch (hiResFmt)
            {
                case VTFFormat.DXT1:
                    for (int m = 1; m < mipCount; m++)
                    {
                        BC1Decoder.SeekPast(_stream, _width / (1 << m), _height / (1 << m));
                    }
                    BC1Decoder.Decode(_br, _pixels, _width, _height);
                    break;
            }
            return ImageDecodeResult.Success;
        }

        public FastColor[] GetThumbnailRef() => _thumb;
        public FastColor[] GetThumbnail()
        {
            FastColor[] pix = new FastColor[_thumb.Length];
            Array.Copy(_thumb, pix, _thumb.Length);
            return pix;
        }

        private bool CheckUnsupported(VTFFormat fmt)
        {
            switch (fmt)
            {
                default: return true;
                case VTFFormat.DXT1:
                case VTFFormat.DXT5:
                    return false;
            }
        }

        private void ConvertToColorMode(VTFFormat fmt, out ColorMode mode, out PixelByteOrder byteOrder)
        {
            mode = ColorMode.RGB24;
            byteOrder = PixelByteOrder.RGBA;
            switch (fmt) 
            {
                case VTFFormat.ABGR8888:
                    mode = ColorMode.RGBA32;
                    byteOrder = PixelByteOrder.ABGR;
                    break;
                case VTFFormat.ARGB8888:
                    mode = ColorMode.RGBA32;
                    byteOrder = PixelByteOrder.ARGB;
                    break;

                case VTFFormat.BGR565:
                    mode = ColorMode.RGB565;
                    byteOrder = PixelByteOrder.ABGR;
                    break;
                case VTFFormat.BGR888:
                    mode = ColorMode.RGB24;
                    byteOrder = PixelByteOrder.ARGB;
                    break;

                case VTFFormat.DXT1:
                    mode = ColorMode.RGB565;
                    byteOrder = PixelByteOrder.ABGR;
                    break;
            }
        }

        public override void ValidateFormat()
        {
            //throw new NotImplementedException();
        }
    }
}
