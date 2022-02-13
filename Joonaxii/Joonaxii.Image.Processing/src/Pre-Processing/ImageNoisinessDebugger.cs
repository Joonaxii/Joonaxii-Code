using Joonaxii.MathJX;
using System;
using System.Collections.Generic;

namespace Joonaxii.Image.Processing
{
    public class ImageNoisinessDebugger : ImageProcessBase<bool>
    {
        public const byte DEFAULT_BITS_PER_RMS = 8;
        public byte GetBitsPerRMS { get => _bitsPerRMS; }
        public override bool ModifiesImage => true;

        private byte _bitsPerRMS;

        public ImageNoisinessDebugger(byte bitsPerRMS = DEFAULT_BITS_PER_RMS)
        {
            _bitsPerRMS = (byte)(bitsPerRMS < 1 ? 1 : bitsPerRMS > 32 ? 32 : bitsPerRMS);
        }

        public override bool Process(IPixelProvider pixels, int width, int height, byte bpp)
        {
            var pix = pixels.GetPixels();
            byte[] gray = new byte[pix.Length];
            FastColor[] debug = new FastColor[pix.Length];

            for (int i = 0; i < pix.Length; i++)
            {
                gray[i] = pix[i].GetAverageRGB();
            }

            float[] tempRMS = new float[gray.Length];
      
            Dictionary<uint, int> rmsLut = new Dictionary<uint, int>();
            Dictionary<uint, float> rmsLutVal = new Dictionary<uint, float>();

            float maxValue = (1u << _bitsPerRMS) - 1u;

            const int BLOCK_SIZE = 3;
            byte[] viewPixels = new byte[(BLOCK_SIZE * 2 + 1) * (BLOCK_SIZE * 2 + 1)];

            float minNoise = float.MaxValue;
            float maxNoise = 0;

            int wOne = width - 1;
            int hOne = height - 1;

            for (int y = 0; y < height; y++)
            {
                int yP = y * width;
                int bMinY = y - BLOCK_SIZE;
                int bMaxY = y + BLOCK_SIZE + 1;
                for (int x = 0; x < width; x++)
                {
                    int bMinX = x - BLOCK_SIZE;
                    int bMaxX = x + BLOCK_SIZE + 1;

                    int i = yP + x;
 
                    for (int tY = bMinY; tY < bMaxY; tY++)
                    {
                        int yTP = tY * width;
                        int yVP = (tY - bMinY) * (BLOCK_SIZE * 2 + 1);

                        bool oob = tY < 0 | tY > hOne; 
                        for (int tX = bMinX; tX < bMaxX; tX++)
                        {
                            int iT = yTP + tX;
                            int iVT = yVP + (tX - bMinX);

                            oob |= tX < 0 | tX > wOne;
                            viewPixels[iVT] = oob ? (byte)0 : gray[iT];
                        }
                    }

                    float rawRMS = Maths.CalculateRMS(viewPixels, (BLOCK_SIZE * 2 + 1), (BLOCK_SIZE * 2 + 1));
                    minNoise = rawRMS < minNoise ? rawRMS : minNoise;
                    maxNoise = rawRMS > maxNoise ? rawRMS : maxNoise;
                    tempRMS[i] = rawRMS;
                }
            }

            int minCount = int.MaxValue;
            int maxCount = 0;

            uint[] rmsVals = new uint[debug.Length];
            for (int i = 0; i < debug.Length; i++)
            {
                float range = Maths.InverseLerp(minNoise, maxNoise, tempRMS[i]) * maxValue;
                var val = rmsVals[i] = (uint)Math.Round(range);
                if(rmsLut.ContainsKey(val))
                {
                    rmsLut[val]++;
                    continue;
                }
                rmsLut.Add(val, 1);
            }

            foreach (var item in rmsLut)
            {
                int c = item.Value;
                minCount = minCount > c ? c : minCount;
                maxCount = maxCount < c ? c : maxCount;
            }

            foreach (var item in rmsLut)
            {
                rmsLutVal.Add(item.Key, Maths.InverseLerp(minCount, maxCount, item.Value));
            }
            rmsLut.Clear();

            for (int i = 0; i < tempRMS.Length; i++)
            {
                var tmp = rmsVals[i];
                float v = (tmp / (float)maxValue) * (1.0f - rmsLutVal[tmp]);
                debug[i] = new FastColor((byte)(v * 255.0f));
            }

            pixels.SetPixels(debug);
            return true;
        }
    }
}
