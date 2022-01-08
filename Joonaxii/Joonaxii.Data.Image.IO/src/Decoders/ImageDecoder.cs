using Joonaxii.Data.Image.GIFDecoder;
using Joonaxii.IO;
using System;
using System.Diagnostics;
using System.IO;

namespace Joonaxii.Data.Image.IO
{
    public class ImageDecoder : ImageDecoderBase
    {
        private static string WEBP_TEMP_PATH 
        {
            get
            {
                if (!Directory.Exists(_webpTemp)) { Directory.CreateDirectory(_webpTemp); }
                return _webpTemp;
            }
        }
        private static string _webpTemp;

        public bool IsValid { get => _isReady; }

        public bool IsWebpDecoderPresent { get => _fileInfoWebpDecoder != null && _fileInfoWebpDecoder.Exists; }

        private BitReader _reader;
        private int[] _indices;

        private bool _isIndexed;
        private bool _isReady;

        private FileInfo _fileInfoWebpDecoder;

        static ImageDecoder()
        {
            _webpTemp = $"{Path.GetTempPath()}/Image Decoder";
        }

        public ImageDecoder(Stream inputStream) : this(inputStream, string.Empty) { }
        public ImageDecoder(Stream inputStream, string webpDecoderPath) : base(new BitReader(inputStream), true)
        {
            _reader = _br as BitReader;
            _indices = null;
            _fileInfoWebpDecoder = string.IsNullOrEmpty(webpDecoderPath) ? null : new FileInfo(webpDecoderPath); 
        }

        public ImageDecoder(byte[] imageData) : this(new MemoryStream(imageData), string.Empty) { }
        public ImageDecoder(byte[] imageData, string webpDecoderPath) : this(new MemoryStream(imageData), webpDecoderPath) { }

        public override ImageDecodeResult Decode(bool skipHeader)
        {
            _isReady = false;

            long posSt = _reader.BaseStream.Position;
            HeaderType type = HeaderManager.GetFileType(_reader, false);
            if (!HeaderType.IMAGE_FORMAT.HasFlag(type)) { return ImageDecodeResult.InvalidImageFormat; }

            switch (type)
            {
                case HeaderType.GIF87:
                case HeaderType.GIF89:
                    int gifMode = type == HeaderType.GIF89 ? 2 : 1;
                    using(GIFReader gr = new GIFReader(_reader, gifMode))
                    {
                        if(!gr.Decode(out string msg)) { return ImageDecodeResult.DecodeFailed; }
                        _isIndexed = gr.IsValidIndexed;

                        _width = gr.Width;
                        _height = gr.Height;
                        _bpp = (byte)(_isIndexed ? 8 : gr.HasAlpha ? 32 : 24);
                        _colorMode = ImageIOExtensions.GetColorMode(_bpp);
                    }
                    break;
                case HeaderType.WEBP:
                    if (!IsWebpDecoderPresent) { return ImageDecodeResult.WebpDecoderMissing; }
                    _reader.BaseStream.Position = posSt;

                    byte[] webpData = _reader.ReadToEnd();
                    webpData = WebpToBmp(_fileInfoWebpDecoder.FullName, webpData);

                    if(webpData == null) { return ImageDecodeResult.DecodeFailed; }

                    using(MemoryStream ms = new MemoryStream(webpData))
                    using(BmpDecoder bmpDec = new BmpDecoder(ms))
                    {
                        ImageDecodeResult res;
                        switch (res = bmpDec.Decode(false))
                        {
                            default: return res;
                            case ImageDecodeResult.Success:
                                break;
                        }

                        _bpp = bmpDec.BitsPerPixel;
                        _colorMode = bmpDec.ColorMode;
                        _width = bmpDec.Width;
                        _height = bmpDec.Height;

                        _pixels = new FastColor[_width * _height];
                        bmpDec.GetPixels(_pixels);
                    }
                    break;

                case HeaderType.BMP:
                    using (BmpDecoder bmpDec = new BmpDecoder(_reader, false))
                    {
                        ImageDecodeResult res;
                        switch (res = bmpDec.Decode(false))
                        {
                            default: return res;
                            case ImageDecodeResult.Success:
                                break;
                        }

                        _bpp = bmpDec.BitsPerPixel;
                        _colorMode = bmpDec.ColorMode;
                        _width = bmpDec.Width;
                        _height = bmpDec.Height;

                        _pixels = new FastColor[_width * _height];
                        bmpDec.GetPixels(_pixels);
                    }
                    break;

                default: return ImageDecodeResult.NotSupported;
            }

            _isReady = true;
            return ImageDecodeResult.Success;
        }

        public byte[] GetRawPixelData(byte newBPP = 0)
        {
            if (!_isReady) { return null; }
         
            using(MemoryStream stream = new MemoryStream())
            {
                WriteRawPixelData(stream, newBPP);
                stream.Flush();
                return stream.ToArray();
            }
        }

        public void WriteRawPixelData(Stream stream, byte newBPP = 0)
        {
            if (!_isReady) { return; }

            newBPP = newBPP < 1 ? _bpp : newBPP;
            bool indexed = _isIndexed & newBPP < 16;

            using (BitWriter bw = new BitWriter(stream))
            {

            }
        }

        private static byte[] WebpToBmp(string webpDecoder, byte[] webpData)
        {
            string path = WEBP_TEMP_PATH;
            string tempA = Path.Combine(path, "TEMP_WEBP.webp");
            string tempB = Path.Combine(path, "TEMP_BMP.bmp");

            File.WriteAllBytes(tempA, webpData);

            string decoderName = Path.GetFileName(webpDecoder);
            string command = $"/C {decoderName} \"{tempA}\" -bmp -o \"{tempB}\"";
            ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe")
            {
                WorkingDirectory = Path.GetDirectoryName(webpDecoder),
                Arguments = command,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process cmd = Process.Start(startInfo);
            cmd.WaitForExit();

            if (File.Exists(tempA)) { File.Delete(tempA); }

            byte[] data = null;
            if (File.Exists(tempB))
            {
                data = File.ReadAllBytes(tempB);
                File.Delete(tempB);
            }
            return data;
        }

        public override void Dispose()
        {
            base.Dispose();
            _indices = null;
        }
    }
}
