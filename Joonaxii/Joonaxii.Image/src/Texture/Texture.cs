using System;

namespace Joonaxii.Image
{
    public class Texture
    {
        public int Width { get => _width; }
        public int Height { get => _height; }

        public  FastColor[] Pixels { get => _pixels; }

        public ColorMode Format { get; set; }

        private FastColor[] _pixels;

        private ushort _width;
        private ushort _height;

        public FastColor GetPixel(int x, int y) => GetPixel(y * _width + x);
        public FastColor GetPixel(int i)
        {
            if(i < 0 | i >= _width * _height) { throw new IndexOutOfRangeException("Index is out of range of the pixel array!"); }
            return _pixels[i];
        }

        public FastColor[] GetPixels()
        {
            if(_pixels == null) { return null; }

            FastColor[] pix = new FastColor[_pixels.Length];
            Array.Copy(_pixels, 0, pix, 0, _pixels.Length);
            return pix;
        }

        public void SetPixel(int x, int y, FastColor color) => SetPixel(y * _width + x, color);
        public void SetPixel(int i, FastColor color)
        {
            if (i < 0 | i >= _width * _height) { throw new IndexOutOfRangeException("Index is out of range of the pixel array!"); }
            _pixels[i] = color;
        }

        public void SetPixels(FastColor[] pixels)
        {
            if(pixels == null) { throw new NullReferenceException("Given pixel array was null!"); }

            int res = _width * _height;
            if(pixels.Length != res) { throw new ArgumentException("Pixel array's length isn't the same as the resolution of the texture!"); }

            if(_pixels == null || _pixels.Length != res) { _pixels = new FastColor[res]; }
            Array.Copy(pixels, 0, _pixels, 0, res);
        }

        public static void Resize(ref FastColor[] pixels, int width, int height, ResizeMode mode)
        {

        }
    }
}
