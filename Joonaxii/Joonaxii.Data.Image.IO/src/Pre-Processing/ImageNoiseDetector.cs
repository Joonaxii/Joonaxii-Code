using Joonaxii.MathJX;
using System;

namespace Joonaxii.Data.Image.IO.Processing
{
    public class ImageNoiseDetector : ImageProcessBase<float>
    {
        public override bool ModifiesImage => false;
        private bool _highQuality;

        public ImageNoiseDetector(bool highQuality)
        {
            _highQuality = highQuality;
        }

        public override float Process(FastColor[] pixels, int width, int height, byte bpp)
        {
            float val = 0;
            if (_highQuality) 
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var valDiff = DifferenceToSurrounding(pixels, width, height, x, y);
                        val += (valDiff.x + valDiff.y + valDiff.z) * 0.3333f;
                    }
                }
                return val / pixels.Length;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    val += DifferenceToSurroundingGray(pixels, width, height, x, y);
                }
            }
            return val / pixels.Length;
        }

        private Vector3 DifferenceToSurrounding(FastColor[] pixels, int w, int h, int x, int y)
        {
            Vector3 total = new Vector3();
            float count = 0;

            FastColor cA = pixels[y * w + x];
            for (int yy = -1; yy <= 1; yy++)
            {
                int yY = yy + y;
                if (yY < 0 | yY >= h) { continue; }

                for (int xx = -1; xx <= 1; xx++)
                {
                    int xX = xx + x;
                    if (xX < 0 | xX >= w | (xX == x & yY == y)) { continue; }
                    count++;
                    FastColor cB = pixels[yY * w + xX];
                    total += new Vector3(Difference(cA.r, cB.r), Difference(cA.g, cB.g), Difference(cA.r, cB.b));
                }
            }
            return total / count;
        }
        private float DifferenceToSurroundingGray(FastColor[] pixels, int w, int h, int x, int y)
        {
            float total = 0;
            float count = 0;

            FastColor c = pixels[y * w + x];
            for (int yy = -1; yy <= 1; yy++)
            {
                int yY = yy + y;
                if(yY < 0 | yY >= h) { continue; }

                for (int xx = -1; xx <= 1; xx++)
                {
                    int xX = xx + x;
                    if (xX < 0 | xX >= w | (xX == x & yY == y)) { continue; }
                    count++;
                    total += Difference(c, pixels[yY * w + xX]);
                }
            }
            return total / count;
        }

        private const float BYTE_TO_FLOAT = 1.0f / 255.0f;
        private float Difference(byte a, byte b) => Math.Abs(b - a) * BYTE_TO_FLOAT;

        private float Difference(FastColor a, FastColor b)
        {
            float dR = (b.r - a.r) * BYTE_TO_FLOAT;
            float dG = (b.g - a.g) * BYTE_TO_FLOAT;
            float dB = (b.b - a.b) * BYTE_TO_FLOAT;    
            return (float)Math.Sqrt((dR * dR) + (dG * dG) + (dB * dB)); 
        }
    }
}
