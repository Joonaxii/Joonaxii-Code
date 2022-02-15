using System;
using System.Collections.Generic;

namespace Joonaxii.Image.Processing.Dithering
{
    public class FloydSteinberg : IDitherProcess
    {
        public const int DEFAULT_PALETTE_SIZE = 256;

        private Func<FastColor, FastColor> _getClosestColor;
        private float[] _mask;
        private int _maxPaletteSize;

        private int _width;
        private int _height;

        public bool IsRunning => _running;

        public int Width => _width;
        public int Height => _height;

        private bool _running;
        private Action<IDitherProcess> _onComplete;
        private Action<Exception> _onException;
        private FastColor[] _pixels;

        public FloydSteinberg(Func<FastColor, FastColor> findClosestColor)
        {
            _maxPaletteSize = DEFAULT_PALETTE_SIZE;
            _getClosestColor = findClosestColor;
            _mask = null;
        }

        public FloydSteinberg(int maxPaletteSize)
        {
            _maxPaletteSize = maxPaletteSize < 2 ? 2 : maxPaletteSize;
            _getClosestColor = null;
            _mask = null;
        }

        public void Setup(int width, int height, FastColor[] pixels, float[] mask)
        {
            _pixels = pixels;
            _width = width;
            _height = height;

            _mask = mask;
            if (_getClosestColor == null)
            {
                Dictionary<FastColor, ColorContainer> paletteLut = new Dictionary<FastColor, ColorContainer>();
                List<ColorContainer> containers = new List<ColorContainer>();
                for (int i = 0; i < pixels.Length; i++)
                {
                    ref var px = ref pixels[i];
                    if (paletteLut.TryGetValue(px, out var container))
                    {
                        container.count++;
                        continue;
                    }
                    container = new ColorContainer(px, 1, paletteLut.Count);
                    paletteLut.Add(px, container);
                    containers.Add(container);
                }
                paletteLut.Clear();
                containers.Sort((ColorContainer a, ColorContainer b) => { return a.count.CompareTo(b.count); });

                while (containers.Count > _maxPaletteSize)
                {
                    containers.RemoveAt(0);
                }

                FastColor[] palette = new FastColor[containers.Count];
                for (int i = 0; i < palette.Length; i++)
                {
                    palette[i] = containers[i].color;
                }
                containers.Clear();

                _getClosestColor = (FastColor input) =>
                {
                    int ind = 0;
                    float scalar = float.MaxValue;
                    for (int i = 0; i < palette.Length; i++)
                    {
                        var c = palette[i];

                        byte r = (byte)Math.Abs(input.r - c.r);
                        byte g = (byte)Math.Abs(input.g - c.g);
                        byte b = (byte)Math.Abs(input.b - c.b);

                        float diff = new FastColor(r, g, b, input.a).GetScalar();
                        if (diff < scalar)
                        {
                            scalar = diff;
                            ind = i;
                        }
                    }
                    return palette[ind];
                };
            }
        }

        public void Run(FastColor[] pixels, Action<int, int, float> progression = null)
        {
            _running = true;
            int edgeCase;
            int heightOne = _height - 1;
            int totalCount = (_width * _height);
            float totalL = totalCount - 1.0f;
            int c = 0;

            try
            {

       
            progression?.Invoke(0, totalCount, 0.0f);
            for (int y = heightOne; y >= 0; y--)
            {
                int yP = y * _width;
                int yY = y - 1;
                int yPA = yY * _width;

                int yCase = (y == 0 ? 1 : 0);
                for (int x = 0; x < _width; x++)
                {
                    int xCase = x >= _width - 1 ? 1 : x == 0 ? 2 : 0;
                    edgeCase = (yCase & 0x1) | ((xCase & 0x3) << 1);

                    int xA = x - 1;
                    int xB = x + 1;
                    int i = yP + x;

                    int iA = yP + xB;
                    int iB = yPA + xA;
                    int iC = yPA + x;
                    int iD = yPA + xB;

                    var pixOG = pixels[i];
                    var pixPal = _getClosestColor.Invoke(pixOG);

                    float maskA = 1.0f;
                    float maskB = 1.0f;
                    float maskC = 1.0f;
                    float maskD = 1.0f;

                    pixels[i] = pixPal;
                    FastColor error = new FastColor(
                        (byte)Math.Abs(pixOG.r - pixPal.r),
                        (byte)Math.Abs(pixOG.g - pixPal.g),
                        (byte)Math.Abs(pixOG.b - pixPal.b), 0);

                    switch (edgeCase)
                    {
                        case 0b000: //Not on edge
                            if (_mask != null)
                            {
                                maskA = _mask[iA];
                                maskB = _mask[iB];
                                maskC = _mask[iC];
                                maskD = _mask[iD];
                            }

                            pixels[iA] = QuantError(pixels[iA], CalcualteError(error, (7.0f / 16.0f) * maskA));
                            pixels[iB] = QuantError(pixels[iB], CalcualteError(error, (3.0f / 16.0f) * maskB));
                            pixels[iC] = QuantError(pixels[iC], CalcualteError(error, (5.0f / 16.0f) * maskC));
                            pixels[iD] = QuantError(pixels[iD], CalcualteError(error, (1.0f / 16.0f) * maskD));
                            break;
                        case 0b001: //Bottom Edge
                        case 0b101: //Bot Left Corner
                            pixels[iA] = QuantError(pixels[iA], _mask != null ? CalcualteError(error, _mask[iA]) : error);
                            break;
                        case 0b010: //Right Edge
                            if (_mask != null)
                            {
                                maskB = _mask[iB];
                                maskC = _mask[iC];
                            }

                            pixels[iB] = QuantError(pixels[iB], CalcualteError(error, (6.0f / 16.0f) * maskB));
                            pixels[iC] = QuantError(pixels[iC], CalcualteError(error, (10.0f / 16.0f) * maskC));
                            break;
                        case 0b100: //Left on edge
                            if (_mask != null)
                            {
                                maskA = _mask[iA];
                                maskC = _mask[iC];
                                maskD = _mask[iD];
                            }

                            pixels[iA] = QuantError(pixels[iA], CalcualteError(error, (9.0f / 16.0f) * maskA));
                            pixels[iC] = QuantError(pixels[iC], CalcualteError(error, (5.0f / 16.0f) * maskC));
                            pixels[iD] = QuantError(pixels[iD], CalcualteError(error, (2.0f / 16.0f) * maskD));
                            break;
                        case 0b011: //Bot Right Corner [DO NOTHING]
                            break;
                    }
                    c++;
                    progression?.Invoke(c, totalCount, c / totalL);
                }
            }
            }
            catch(Exception e)
            {
                _running = false;
                _onComplete?.Invoke(this);
                _onException?.Invoke(e);
                return;
            }
            progression?.Invoke(totalCount, totalCount, 1.0f);
            _running = false;
            _onComplete?.Invoke(this);
        }

        private FastColor CalcualteError(FastColor input, float error)
        {
            float r = (float)Math.Round(input.r * error);
            float g = (float)Math.Round(input.g * error);
            float b = (float)Math.Round(input.b * error);

            return new FastColor((byte)r, (byte)g, (byte)b, input.a);
        }

        private FastColor QuantError(FastColor colorIn, FastColor error)
        {
            byte r = (byte)Math.Min(colorIn.r + error.r, 255);
            byte g = (byte)Math.Min(colorIn.g + error.g, 255);
            byte b = (byte)Math.Min(colorIn.b + error.b, 255);
            return new FastColor(r, g, b, colorIn.a);
        }

        public void AddOnCompleteListener(Action<IDitherProcess> act) => _onComplete += act;
        public void AddOnExceptionListener(Action<Exception> act) => _onException += act;

        public void RemoveOnCompleteListener(Action<IDitherProcess> act)
        {
            if(_onComplete != null)
            {
                _onComplete -= act;
            }
        }

        public void RemoveOnExceptionListener(Action<Exception> act)
        {
            if (_onException != null)
            {
                _onException -= act;
            }
        }

        public FastColor[] GetPixels() => _pixels;
    }
}
