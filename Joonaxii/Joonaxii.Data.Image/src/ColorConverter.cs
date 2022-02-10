using Joonaxii.MathJX;
using System;

namespace Joonaxii.Data.Image
{
    public static class ColorConverter
    {
        public const float NORMALIZE_BYTE = 1.0f / 255.0f;

        public static YCrCb RGBToYCrCb(FastColor c)
        {
            float fr = c.r / 255.0f;
            float fg = c.g / 255.0f;
            float fb = c.b / 255.0f;

            return new YCrCb(
                (byte)((0.2989f * fr + 0.5866f * fg + 0.1145f * fb) * byte.MaxValue),
                (byte)((-0.1687f * fr - 0.3313f * fg + 0.5000f * fb + 0.5f) * byte.MaxValue),
                (byte)((0.5000f  * fr - 0.4184f * fg - 0.0816f * fb + 0.5f) * byte.MaxValue));
        }


        public static HSVColor RGBToHSV(FastColor c) => HSLToHSV(RGBToHSL(c));
        public static FastColor HSVToRGB(HSVColor c) => HSLToRGB(HSVToHSL(c));

        public static HSLColor RGBToHSL(FastColor c)
        {
            float lL;

            float rN = c.r * NORMALIZE_BYTE;
            float gN = c.g * NORMALIZE_BYTE;
            float bN = c.b * NORMALIZE_BYTE;

            float cMax = Maths.Max(rN, bN, gN);
            float cMin = Maths.Min(rN, bN, gN);

            float delta = cMax - cMin;

            lL = (cMax + cMin) * 0.5f;
            if (delta == 0) { return new HSLColor(0, 0, (byte)((lL < 0 ? 0 : lL > 1.0f ? 100.0f : lL * 100))); }

            float sL;
            float hL;

            sL = (lL <= 0.5f) ? (delta / (cMin + cMax)) : (delta / (2 - cMax - cMin));

            if (rN == cMax)
            {
                hL = (gN - bN) / 6.0f / delta;
            }
            else if (gN == cMax)
            {
                hL = (1.0f / 3.0f) + (bN - rN) / 6.0f / delta;
            }
            else { hL = (2.0f / 3.0f) + (rN - gN) / 6.0f / delta; }
            hL = hL < 0 ? ++hL : hL > 1 ? --hL : hL;

            return new HSLColor(
                (short)((hL < 0 ? 0 : hL > 1.0f ? 360.0f : hL * 360)),
                (byte) ((sL < 0 ? 0 : sL > 1.0f ? 100.0f : sL * 100)), 
                (byte) ((lL < 0 ? 0 : lL > 1.0f ? 100.0f : lL * 100)));
        }

        public static FastColor HSLToRGB(HSLColor c)
        {
            float hN = c.h / 360.0f;
            float sN = c.s / 100.0f;
            float lN = c.l / 100.0f;

            float q = lN < 0.5f ? lN * (1 + sN) : lN + sN - lN * sN;
            float p = 2 * lN - q;

            if (sN != 0)
            {
                return new FastColor(
                    (byte)Math.Round(GetHUE(p, q, hN + 1.0f / 3.0f) * 255),
                    (byte)Math.Round(GetHUE(p, q, hN) * 255),
                    (byte)Math.Round(GetHUE(p, q, hN - 1.0f / 3.0f) * 255));
            }
            return new FastColor(1, 1, 1);
        }


        public static HSVColor HSLToHSV(HSLColor c)
        {
            float sN = c.s / 100.0f;
            float lN = c.l / 100.0f;

            float hsV = lN + sN * Math.Min(lN, 1.0f - lN);
            float hsS = hsV == 0 ? 0 : 2 * (1 - lN / hsV);
            return new HSVColor(c.h, (byte)Math.Round(hsS * 100.0f), (byte)Math.Round(hsV * 100.0f));
        }

        public static HSLColor HSVToHSL(HSVColor c)
        {
            float sN = c.s / 100.0f;
            float vN = c.v / 100.0f;

            float hsL = vN * (1.0f - sN * 0.5f);
            float hsS = (hsL == 0 | hsL == 1) ? 0 : (vN - hsL) / Math.Min(hsL, 1 - hsL);
            return new HSLColor(c.h, (byte)Math.Round(hsS * 100.0f), (byte)Math.Round(hsL * 100.0f));
        }

        public static float GetHUE(float p, float q, float t)
        {
            t = t < 0 ? t + 1 : t > 1 ? t - 1 : t;

            if (t < 1.0f / 6.0f) { return p + (q - p) * 6 * t; }
            if (t < 1.0f / 2.0f) { return q; }
            if (t < 2.0f / 3.0f) { return p + (q - p) * (2.0f / 3.0f - t) * 6; }
            return p;
        }

    }
}