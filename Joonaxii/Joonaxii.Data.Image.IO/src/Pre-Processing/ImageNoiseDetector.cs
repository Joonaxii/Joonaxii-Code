using Joonaxii.MathJX;
using System;

namespace Joonaxii.Data.Image.Conversion.Processing
{
    public class ImageNoiseDetector : ImageProcessBase<float>
    {
        public override bool ModifiesImage => false;

        public ImageNoiseDetector() { }

        public override float Process(IPixelProvider pixProvider, int width, int height, byte bpp)
        {
            var pixels = pixProvider.GetPixels();
            byte[] values = new byte[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                values[i] = pixels[i].GetAverageRGB();
            }
            return Maths.CalcualteSqrRMS(values);
        }

    }
}
