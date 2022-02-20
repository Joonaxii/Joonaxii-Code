using Joonaxii.MathJX;
using System;

namespace Joonaxii.Image.Codecs.JPEG
{
    public class IDCT
    {
        public const int PRECISION = 8;
        private const double SQRT_OF_TWO = 1.41421356237;
        private const double NORM_COEFF = 1.0 / SQRT_OF_TWO;
        private static readonly double[] IDCT_TABLE;
        private static readonly double[] ZIG_ZAG
            = new double[64]
        {
            0,  1,  5,  6,  14, 15, 27, 28,
            2,  4,  7,  13, 16, 26, 29, 42,
            3,  8,  12, 17, 25, 30, 41, 43,
            9,  11, 18, 24, 31, 40, 44, 53,
            10, 19, 23, 32, 39, 45, 52, 54,
            20, 22, 33, 38, 46, 51, 55, 60,
            21, 34, 37, 47, 50, 56, 59, 61,
            35, 36, 48, 49, 57, 58, 62, 63,
        };

        public double[] zigZag;
        public double[] baseValues;

        static IDCT()
        {
            IDCT_TABLE = new double[PRECISION * PRECISION];
            for (int y = 0; y < PRECISION; y++)
            {
                int yP = y * PRECISION;
                for (int x = 0; x < PRECISION; x++)
                {
                    IDCT_TABLE[yP + x] = NormCoeff(y) * Math.Cos(((2.0 * x + 1.0) * y * Math.PI) / 16.0);
                }
            }
        }

        public IDCT()
        {
            baseValues = new double[64];
            zigZag = new double[64];
        }

        public void Reset()
        {
            BufferUtils.Memset(baseValues, 0);
            Buffer.BlockCopy(ZIG_ZAG, 0, zigZag, 0, zigZag.Length * sizeof(double));
        }

        private static double NormCoeff(double val)
        {
            if (val == 0) { return NORM_COEFF; }
            return 1.0;
        }

        public void RearrangeZigZag()
        {
            for (int i = 0; i < zigZag.Length; i++)
            {
                zigZag[i] = baseValues[(int)ZIG_ZAG[i]];
            }
        }

        public void Run()
        {
            for (int y = 0; y < 8; y++)
            {
                int yP = y * 8;
                var yPP = y * PRECISION;
                for (int x = 0; x < 8; x++)
                {
                    double localSum = 0;
                    int xP = x * PRECISION;
                    for (int u = 0; u < PRECISION; u++)
                    {
                        int uP = u * PRECISION;
                        var up = IDCT_TABLE[xP + u];
                        for (int v = 0; v < PRECISION; v++)
                        {
                            localSum += zigZag[uP + v] * up * IDCT_TABLE[yPP + v];
                        }
                    }
                    baseValues[yP + x] = localSum * 0.25;
                }
            }
        }
    }
}