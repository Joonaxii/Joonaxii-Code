using Joonaxii.Audio;
using Joonaxii.Data.Coding;
using Joonaxii.Image;
using Joonaxii.Image.Codecs;
using Joonaxii.MathJX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.Radio
{
    public class SSTVEncoder : CodecBase
    {
        public static double DEFAULT_START_SILENCE { get; } = 3.0;
        public static double DEFAULT_END_SILENCE { get; } = 3.0;

        public static VOXTone[] DefaultVOX
        { get; } = new VOXTone[]
        {
            new VOXTone(1900, 100),
            new VOXTone(1500, 100),
            new VOXTone(1900, 100),
            new VOXTone(1500, 100),

            new VOXTone(2300, 100),
            new VOXTone(1500, 100),
            new VOXTone(2300, 100),
            new VOXTone(1500, 100),
        };


        public static VOXTone[] TheRollVOX
        { get; } = new VOXTone[]
        {

            //Give you up
            new VOXTone(440.00, 130, 25.0, 25),
            new VOXTone(0, 70),

            new VOXTone(493.88, 130, 25.0, 25),
            new VOXTone(0, 70),

            new VOXTone(587.33, 130, 25.0, 25),
            new VOXTone(0, 70),

            new VOXTone(493.88, 130, 25.0, 25),
            new VOXTone(0, 70),


            new VOXTone(739.99, 130, 25.0, 25),
            new VOXTone(0, 70 * 5),

            new VOXTone(739.99, 130, 25.0, 25),
            new VOXTone(0, 70 * 5),

            new VOXTone(659.25, 130, 25.0, 25),
            new VOXTone(0, 70 * 10),

            //Let you down
            new VOXTone(440.00, 130, 25.0, 25),
            new VOXTone(0, 70),

            new VOXTone(493.88, 130, 25.0, 25),
            new VOXTone(0, 70),

            new VOXTone(587.33, 130, 25.0, 25),
            new VOXTone(0, 70),

            new VOXTone(493.88, 130, 25.0, 25),
            new VOXTone(0, 70),


            new VOXTone(659.25, 130, 25.0, 25),
            new VOXTone(0, 70 * 5),

            new VOXTone(659.25, 130, 25.0, 25),
            new VOXTone(0, 70 * 5),

            new VOXTone(587.33, 130, 25.0, 25),
            new VOXTone(0, 70 * 5),

            new VOXTone(554.37, 130, 25.0, 25),
            new VOXTone(0, 70),

            new VOXTone(493.88, 130, 25.0, 25),
            new VOXTone(0, 70),
        };


        public static VOXTone[] MegalovaniaVOX
        { get; } = new VOXTone[]
        {
            new VOXTone(290 * 2.0, 130, 25.0, 25),
            new VOXTone(0, 140 * 0.5),

            new VOXTone(290 * 2.0, 120, 25.0, 25),
            new VOXTone(0, 122 * 0.5),

            new VOXTone(587 * 2.0, 248, 25.0, 25),
            new VOXTone(0, 248 * 0.5),

            new VOXTone(440 * 2.0, 368, 25.0, 25),
            new VOXTone(0, 368 * 0.5),

            new VOXTone(415 * 2.0, 256, 25.0, 25),
            new VOXTone(0, 256 * 0.5),

            new VOXTone(392 * 2.0, 242, 25.0, 25),
            new VOXTone(0, 242 * 0.5),

            new VOXTone(349 * 2.0, 251, 25.0, 25),
            new VOXTone(0, 251 * 0.5),

            new VOXTone(293 * 2.0, 121, 25.0, 25),
            new VOXTone(0, 121 * 0.5),

            new VOXTone(349 * 2.0, 121, 25.0, 25),
            new VOXTone(0, 121),

            new VOXTone(392 * 2.0, 128, 25.0, 25),
            new VOXTone(0, 128 * 0.5),
        };

        public FastColor BackgroundColor { get => _backgroundColor; set => _backgroundColor = value; }
        private FastColor _backgroundColor = FastColor.black;

        public SSTVProtocol Protocol { get => _protocol; set => _protocol = value; }
        public double Volume { get => _volume; set => _volume = value < 0.0 ? 0.0 : _volume > 1.0 ? 1.0 : _volume; }
        private double _volume = 0.75;
        public double VOXToneDurationScale { get => _voxToneDurationMod; set => _voxToneDurationMod = value < 0.0 ? 0.0 : value; }
        private double _voxToneDurationMod = 1.0;

        private double _startSilence = DEFAULT_START_SILENCE;
        private double _endSilence = DEFAULT_END_SILENCE;

        private VOXTone[] _voxTone;
        private SSTVProtocol _protocol;
        private AudioEncoderBase _audioEncoder;
        private ToneGenerator _toneGen;

        private bool _leaveOpen;
        private SSTVFlags _flags;

        public SSTVEncoder(AudioEncoderBase encoder, SSTVProtocol protocol) : this(encoder, false, protocol, SSTVFlags.Default, DefaultVOX) { }
        public SSTVEncoder(AudioEncoderBase encoder, bool leaveOpen, SSTVProtocol protocol) : this(encoder, leaveOpen, protocol, SSTVFlags.Default, DefaultVOX) { }

        public SSTVEncoder(AudioEncoderBase encoder, bool leaveOpen, SSTVProtocol protocol, SSTVFlags flags, params VOXTone[] voxTone)
        {
            _audioEncoder = encoder;
            _leaveOpen = leaveOpen;

            _flags = flags;

            _protocol = protocol;
            _voxTone = voxTone;

            _toneGen = new ToneGenerator(encoder.BitsPerSample < 16, encoder.Samples, encoder.NumChannels > 1, encoder.SampleRate, encoder.FlushData);
        }

        public void SetFlags(SSTVFlags flags) => _flags = flags;

        public void SetVOXTone(double durationScale, params VOXTone[] vox)
        {
            _voxToneDurationMod = durationScale;
            _voxTone = vox;
        }

        public SSTVEncodeResult Encode(FastColor[] pixels, int width, int height)
        {
            if (!_protocol.GetResolution(out var w, out var h)) { return SSTVEncodeResult.UnsupportedMode; }

            int wDelta = w - width;
            int hDelta = h - height;

            if (_flags.HasFlag(SSTVFlags.RequireExactWidth) & wDelta != 0 || (_flags.HasFlag(SSTVFlags.RequireExactHeight) & hDelta != 0))
            {
                return SSTVEncodeResult.InvalidResolution;
            }

            bool centerX = _flags.HasFlag(SSTVFlags.CenterX) & wDelta != 0;
            bool centerY = _flags.HasFlag(SSTVFlags.CenterY) & hDelta != 0;

            if (centerX | centerY)
            {
                int yOffset = centerY ? hDelta / 2 : 0;
                int xOffset = centerX ? wDelta / 2 : 0;

                int wW = width;
                int hH = height;

                if (yOffset > 0)
                {
                    height += (yOffset > 0 ? yOffset : 0);
                    height = Math.Max(height, h);
                }
                else
                {
                    //height = h;
                }

                if (xOffset > 0)
                {
                    width += (xOffset > 0 ? xOffset : 0);
                    width = Math.Max(width, w);
                }
                else
                {
                    width = w;
                }

                int size = width * height;

                FastColor[] temp = new FastColor[size];
                for (int i = 0; i < size; i++) { temp[i] = _backgroundColor; }

                int wWT = Math.Min(wW, width);
                for (int y = 0; y < hH; y++)
                {
                    int yP = (y - Math.Min(yOffset, 0));
                    if (yP < 0 | yP >= hH) { continue; }

                    yP *= wW;

                    int yTP = (y) * width;
                    for (int x = 0; x < wWT; x++)
                    {
                        int xP = (x - Math.Min(xOffset, 0));
                        if (xP < 0 | xP >= wW) { continue; }

                        int xTP = x;

                        int i = yP + xP;
                        int iT = yTP + xTP;

                        temp[iT] = pixels[i];
                    }
                }
                pixels = temp;
            }
            else
            {
                bool changed = width < w | height < h;

                if (changed)
                {
                    int nWidth = Math.Max(w, width);
                    int nHeight = Math.Max(h, height);

                    FastColor[] temp = new FastColor[nWidth * nHeight];
                    for (int i = 0; i < temp.Length; i++) { temp[i] = _backgroundColor; }

                    for (int y = 0; y < height; y++)
                    {
                        int yP = y * width;
                        int yTP = y * nWidth;
                        if (y < 0 | y >= height) { continue; }

                        for (int x = 0; x < height; x++)
                        {
                            if (x < 0 | x >= width) { continue; }
                            int i = yP + x;
                            int iT = yTP + x;

                            temp[iT] = pixels[i];
                        }
                    }

                    pixels = temp;
                }
            }

            if (_startSilence > 0)
            {
                _toneGen.Generate(0, 0, _startSilence);
            }


            //VOX Tone
            if (_voxTone != null)
            {
                foreach (var tone in _voxTone)
                {
                    _toneGen.Generate(tone.frequency, _volume, tone.duration * _voxToneDurationMod - tone.fadeOut, tone.fadeIn, tone.fadeOut);
                }
            }

            //VIS Header
            _toneGen.Generate(1900, _volume, 300);
            _toneGen.Generate(1200, _volume, 10);
            _toneGen.Generate(1900, _volume, 300);
            _toneGen.Generate(1200, _volume, 30);

            //Protocol
            bool parity = false;
            for (int i = 1; i <= 64; i <<= 1)
            {
                if ((i & (int)_protocol) != 0)
                {
                    _toneGen.Generate(1100, _volume, 30);
                    parity = !parity;
                    continue;
                }
                _toneGen.Generate(1300, _volume, 30);
            }

            _toneGen.Generate(parity ? 1100 : 1300, _volume, 30);
            _toneGen.Generate(1200, _volume, 30);

            //Write Data
            switch (_protocol)
            {
                default:
                    return SSTVEncodeResult.UnsupportedMode;

                case SSTVProtocol.Martin1:
                    GenerateMartin(0.4576, pixels, w, width, height);
                    break;
                case SSTVProtocol.Martin2:
                    GenerateMartin(0.2288, pixels, w, width, height);
                    break;

                case SSTVProtocol.Scottie1:
                case SSTVProtocol.Scottie3:
                    GenerateScottie(0.432, pixels, w, width, height);
                    break;

                case SSTVProtocol.Scottie2:
                case SSTVProtocol.Scottie4:
                    GenerateScottie(0.2752, pixels, w, width, height);
                    break;

                case SSTVProtocol.ScottieDX:
                    GenerateScottie(1.080, pixels, w, width, height);
                    break;

                case SSTVProtocol.ScottieDX2:
                    GenerateScottie(0.53950, pixels, w, width, height);
                    break;

                case SSTVProtocol.Pasokon3:
                    GeneratePasokon(4800, pixels, w, width, height);
                    break;
                case SSTVProtocol.Pasokon5:
                    GeneratePasokon(3200, pixels, w, width, height);
                    break;
                case SSTVProtocol.Pasokon7:
                    GeneratePasokon(2400, pixels, w, width, height);
                    break;

                case SSTVProtocol.Robot36:
                    GenerateRobot(0.275, 0.1375, pixels, w, width, height);
                    break;
                case SSTVProtocol.Robot72:
                    GenerateRobotYU42(0.43125, 0.215625, pixels, w, width, height);
                    break;

                case SSTVProtocol.Robot12:
                    GenerateRobot(0.375, 0.1875, pixels, w, width, height);
                    break;
                case SSTVProtocol.Robot24:
                    GenerateRobotYU42(0.2815, 0.140625, pixels, w, width, height);
                    break;


                case SSTVProtocol.PD50:
                    GeneratePD(0.286, pixels, w, width, height);
                    break;

                case SSTVProtocol.PD90:
                    GeneratePD(0.532, pixels, w, width, height);
                    break;

                case SSTVProtocol.PD120:
                    GeneratePD(0.190, pixels, w, width, height);
                    break;

                case SSTVProtocol.PD160:
                    GeneratePD(0.190, pixels, w, width, height);
                    break;

                case SSTVProtocol.PD180:
                    GeneratePD(0.286, pixels, w, width, height);
                    break;

                case SSTVProtocol.PD240:
                    GeneratePD(0.3282, pixels, w, width, height);
                    break;

                case SSTVProtocol.PD290:
                    GeneratePD(0.286, pixels, w, width, height);
                    break;
            }

            //VIS Trailer
            _toneGen.Generate(1500, _volume, 400);
            _toneGen.Generate(1900, _volume, 100);
            _toneGen.Generate(1500, _volume, 100);
            _toneGen.Generate(1900, _volume, 100);
            _toneGen.Generate(1500, _volume, 100);

            if (_endSilence > 0)
            {
                _toneGen.Generate(0, 0, _endSilence);
            }

            _audioEncoder.FlushData();
            _audioEncoder.WritePostData();
            return SSTVEncodeResult.Success;
        }

        public SSTVEncodeResult Encode(ImageDecoderBase decoder)
        {
            if (!decoder.IsDecoded)
            {
                switch (decoder.Decode(false))
                {
                    default: return SSTVEncodeResult.ImageDecodeFailed;
                }
            }
            FastColor[] pixels = decoder.GetTexture().GetPixels();
            return Encode(pixels, decoder.Width, decoder.Height);
        }

        private void GeneratePasokon(double timeUnitScale, FastColor[] pixels, int scanW, int width, int height)
        {
            double timeUnit = (1.0 / timeUnitScale) * 1000.0;
            double gap = timeUnit * 5;
            double sync = timeUnit * 25;

            _toneGen.Generate(1200, _volume, sync);
            for (int y = 0; y < 16; y++)
            {
                byte gr = (byte)((y / 15.0) * 255);
                _toneGen.Generate(1500, _volume, gap);

                _toneGen.Generate(ValueToTone(gr), _volume, timeUnit * scanW);
                _toneGen.Generate(1500, _volume, gap);

                _toneGen.Generate(ValueToTone(gr), _volume, timeUnit * scanW);
                _toneGen.Generate(1500, _volume, gap);

                _toneGen.Generate(ValueToTone(gr), _volume, timeUnit * scanW);
                _toneGen.Generate(1500, _volume, gap);

                _toneGen.Generate(1200, _volume, sync);
            }

            for (int y = 0; y < height; y++)
            {
                _toneGen.Generate(1500, _volume, gap);
                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(pixels[y * width + x].r), _volume, timeUnit);
                }

                _toneGen.Generate(1500, _volume, gap);
                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(pixels[y * width + x].g), _volume, timeUnit);
                }

                _toneGen.Generate(1500, _volume, gap);
                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(pixels[y * width + x].b), _volume, timeUnit);
                }

                _toneGen.Generate(1500, _volume, gap);
                _toneGen.Generate(1200, _volume, sync);
            }
        }

        private void GenerateRobot(double pixelIntervalA, double pixelIntervalB, FastColor[] pixels, int scanW, int width, int height)
        {
            Vector4[] scanVals = new Vector4[scanW];
            if (height % 2 != 0) { height--; }
            for (int y = 0; y < height; y += 2)
            {
                for (int x = 0; x < scanW; x++)
                {
                    FastColor a = pixels[y * width + x];
                    FastColor b = pixels[(y + 1) * width + x];

                    FastColor avg = new FastColor(
                        (byte)((a.r + b.r) >> 1),
                        (byte)((a.g + b.g) >> 1),
                        (byte)((a.b + b.b) >> 1));

                    scanVals[x] = new Vector4(
                        16.0f + (0.003906f * ((65.738f * a.r) + (129.057f * a.g) + (25.064f * a.b))),
                        16.0f + (0.003906f * ((65.738f * b.r) + (129.057f * b.g) + (25.064f * b.b))),
                        128.0f + (0.003906f * ((112.439f * avg.r) + (-94.154f * avg.g) + (-18.285f * avg.b))),
                        128.0f + (0.003906f * ((-37.945f * avg.r) + (-74.494f * avg.g) + (112.439f * avg.b)))
                        );
                }

                _toneGen.Generate(1200, _volume, 9);
                _toneGen.Generate(1500, _volume, 3);

                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(scanVals[x].x), _volume, pixelIntervalA);
                }

                _toneGen.Generate(1400, _volume, 4.5);
                _toneGen.Generate(1900, _volume, 1.5);

                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(scanVals[x].z), _volume, pixelIntervalB);
                }


                _toneGen.Generate(1200, _volume, 9);
                _toneGen.Generate(1500, _volume, 3);

                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(scanVals[x].y), _volume, pixelIntervalA);
                }

                _toneGen.Generate(2300, _volume, 4.5);
                _toneGen.Generate(1900, _volume, 1.5);

                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(scanVals[x].w), _volume, pixelIntervalB);
                }
            }
        }

        private void GenerateRobotYU42(double pixelIntervalA, double pixelIntervalB, FastColor[] pixels, int scanW, int width, int height)
        {
            YCrCb[] scanVals = new YCrCb[scanW];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < scanW; x++)
                {
                    scanVals[x] = ColorConverter.RGBToYCrCb(pixels[y * width + x]);
                }

                _toneGen.Generate(1200, _volume, 9);
                _toneGen.Generate(1500, _volume, 3);

                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(scanVals[x].y), _volume, pixelIntervalA);
                }

                _toneGen.Generate(1500, _volume, 4.5);
                _toneGen.Generate(1900, _volume, 1.5);
                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(scanVals[x].cr), _volume, pixelIntervalB);
                }

                _toneGen.Generate(2300, _volume, 4.5);
                _toneGen.Generate(1900, _volume, 1.5);
                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(scanVals[x].cb), _volume, pixelIntervalB);
                }
            }
        }

        private void GeneratePD(double pixelInterval, FastColor[] pixels, int scanW, int width, int height)
        {
            //TODO: Write PD generation, https://www.sstv-handbook.com/download/sstv-handbook.pdf
            //Pages 46 -> 47

            Vector4[] scanVals = new Vector4[scanW];
            if (height % 2 != 0) { height--; }
            for (int y = 0; y < height; y += 2)
            {
                for (int x = 0; x < scanW; x++)
                {
                    FastColor a = pixels[y * width + x];
                    FastColor b = pixels[(y + 1) * width + x];

                    FastColor avg = new FastColor(
                        (byte)((a.r + b.r) >> 1),
                        (byte)((a.g + b.g) >> 1),
                        (byte)((a.b + b.b) >> 1));

                    scanVals[x] = new Vector4(
                        16.0f + (0.003906f * ((65.738f * a.r) + (129.057f * a.g) + (25.064f * a.b))),
                        16.0f + (0.003906f * ((65.738f * b.r) + (129.057f * b.g) + (25.064f * b.b))),
                        128.0f + (0.003906f * ((112.439f * avg.r) + (-94.154f * avg.g) + (-18.285f * avg.b))),
                        128.0f + (0.003906f * ((-37.945f * avg.r) + (-74.494f * avg.g) + (112.439f * avg.b)))
                        );
                }

                _toneGen.Generate(1200, _volume, 20);
                _toneGen.Generate(1500, _volume, 2.080);

                //Y1
                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(scanVals[x].x), _volume, pixelInterval);
                }

                //Cr
                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(scanVals[x].z), _volume, pixelInterval);
                }

                //Cb
                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(scanVals[x].w), _volume, pixelInterval);
                }

                //Y2
                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(scanVals[x].y), _volume, pixelInterval);
                }
            }
        }

        private void GenerateScottie(double pixelInterval, FastColor[] pixels, int scanW, int width, int height)
        {
            _toneGen.Generate(1200, _volume, 9);
            for (int y = 0; y < height; y++)
            {
                //Green
                _toneGen.Generate(1500, _volume, 1.5);
                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(pixels[y * width + x].g), _volume, pixelInterval);
                }

                //Blue
                _toneGen.Generate(1500, _volume, 1.5);
                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(pixels[y * width + x].b), _volume, pixelInterval);
                }

                //Red
                _toneGen.Generate(1200, _volume, 9.0);
                _toneGen.Generate(1500, _volume, 1.5);
                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(pixels[y * width + x].r), _volume, pixelInterval);
                }
            }
        }

        private void GenerateMartin(double pixelInterval, FastColor[] pixels, int scanW, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                _toneGen.Generate(1200, _volume, 4.862);
                _toneGen.Generate(1500, _volume, 0.572);

                //Green
                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(pixels[y * width + x].g), _volume, pixelInterval);
                }
                _toneGen.Generate(1500, _volume, 0.572);

                //Blue
                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(pixels[y * width + x].b), _volume, pixelInterval);
                }
                _toneGen.Generate(1500, _volume, 0.572);

                //Red
                for (int x = 0; x < scanW; x++)
                {
                    _toneGen.Generate(ValueToTone(pixels[y * width + x].r), _volume, pixelInterval);
                }
                _toneGen.Generate(1500, _volume, 0.572);
            }
        }

        private double ValueToTone(float val) => 1500.0 + (val * 3.1372549);

        public override void Dispose()
        {
            _voxTone = null;
            if (_leaveOpen) { return; }

            _audioEncoder.Dispose();
        }
    }
}
