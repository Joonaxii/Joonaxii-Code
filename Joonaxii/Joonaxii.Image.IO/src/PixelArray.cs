
using System;

namespace Joonaxii.Data.Image.Conversion.Processing
{
    public class PixelArray : IPixelProvider
    {
        private FastColor[] _pixels;
        public PixelArray(FastColor[] pixels) => _pixels = pixels;

        public FastColor GetPixel(int i) => _pixels[i];

        public FastColor[] GetPixels() => _pixels;

        public void SetPixels(FastColor[] pixels)
        {
            if(_pixels != null)
            {
                if(pixels.Length != _pixels.Length) { Array.Resize(ref _pixels, pixels.Length); }
            }
            else { _pixels = new FastColor[pixels.Length]; }
            Array.Copy(pixels, _pixels, pixels.Length);
        }
    }
}
