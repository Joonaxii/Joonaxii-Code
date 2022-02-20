using System;
using System.Collections.Generic;
using System.IO;
using Joonaxii.Image;

namespace Joonaxii.Image.Codecs.DXT
{
    internal static class DXTUtils
    {
        public static void Interpolate(BinaryReader br, FastColor[] colors, bool isDXT1)
        {
            FastColor colA = br.ReadColor(ColorMode.RGB565, true);
            FastColor colB = br.ReadColor(ColorMode.RGB565, true);

            colors[0] = colA;
            colors[1] = colB;

            if (isDXT1 & colA <= colB)
            {
                colors[2] = new FastColor(
                    (byte)((colA.r + colB.r) >> 1),
                    (byte)((colA.g + colB.g) >> 1),
                    (byte)((colA.b + colB.b) >> 1));
                colors[3] = FastColor.clear;
                return;
            }

             colors[2] =  FastColor.Lerp(colA, colB, 1.0f / 3.0f);
             colors[3] = FastColor.Lerp(colA, colB, 2.0f / 3.0f);
        }
    }
}