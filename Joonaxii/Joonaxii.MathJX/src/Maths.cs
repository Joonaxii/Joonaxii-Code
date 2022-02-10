using System;

namespace Joonaxii.MathJX
{
    public static class Maths
    {
        public const float PI = (float)Math.PI;
        public const float TWO_PI = PI * 2.0f;
        public const float Epsilon = 1E-15f;

        public const float Deg2Rad = PI / 180;
        public const float Rad2Deg = 360.0f / TWO_PI;

        public static int GetRange(this int val, int start, int length)
        {
            int v = 0;

            int ii = 0;
            for (int i = start; i < start + length; i++)
            {
                v = v.SetBit(ii++, val.IsBitSet(i));
            }
            return v;
        }

        public static byte GetRange(this byte val, int start, int length)
        {
            byte v = 0;

            int ii = 0;
            for (int i = start; i < start + length; i++)
            {
                v = v.SetBit(ii++, val.IsBitSet(i));
            }
            return v;
        }

        public static float Remap(float input, float minA, float maxA, float minB, float maxB) => Lerp(minB, maxB, InverseLerp(minA, maxA, input));

        public static float CalculateRMS(byte[] input) => (float)Math.Sqrt(CalcualteSqrRMS(input));
        public static float CalcualteSqrRMS(byte[] input)
        {
            long square = 0;
            float mean;

            for (int i = 0; i < input.Length; i++)
            {
                square += (long)Math.Pow(input[i], 2);
            }

            mean = (square / (float)input.Length) / 255.0f;
            return mean;
        }

        public static float CalculateRMS(byte[] input, int w, int h) => (float)Math.Sqrt(CalcualteSqrRMS(input, w, h));
        public static float CalcualteSqrRMS(byte[] input, int w, int h)
        {
            long square = 0;
            float mean;

            for (int y = 0; y < h; y++)
            {
                int yP = y * w;
                for (int x = 0; x < w; x++)
                {
                    square += (long)Math.Pow(input[yP + x], 2);
                }
            }

            mean = (square / (float)input.Length) / 255.0f;
            return mean;
        }

        public static byte SetRange(this byte val, int start, int length, byte value)
        {
            byte v = val;

            int ii = 0;
            for (int i = start; i < start + length; i++)
            {
                v = v.SetBit(i, value.IsBitSet(ii++));
            }
            return v;
        }

        public static long SetRange(this long val, int start, int length, long value)
        {
            long v = val;

            int ii = 0;
            for (int i = start; i < start + length; i++)
            {
                v = v.SetBit(i, value.IsBitSet(ii++));
            }
            return v;
        }


        public static int Encode7Bit(this int input)
        {
            int val = 0;
            uint v = (uint)input;

            byte i = 0;
            while (v >= 0x80)
            {
                val += ((byte)(v | 0x80) << (8 * i++));
                v >>= 7;
            }

            val += ((byte)v << i * 8);
            return val;
        }

        public static int Decode7Bit(this int input)
        {
            int count = 0;
            int shift = 0;
            byte i = 0;

            int b = input.GetRange(0, 8);
            count |= (b & 0x7F) << shift;
            shift += 7;

            i++;
            while ((b & 0x80) != 0)
            {
                if (shift >= 5 * 7) { break; }

                b = input.GetRange((byte)(8 * i++), 8);
                count |= (b & 0x7F) << shift;
                shift += 7;
            }
            return count;
        }

        public static bool IsBitSet(this long val, int bit) => (val & (1L << bit)) != 0;
        public static long SetBit(this long input, int bitIndex, bool value)
        {
            if (value) { return input |= (1L << bitIndex); }
            return input &= ~(1L << bitIndex);
        }

        public static bool IsBitSet(this ulong val, int bit) => (val & (1ul << bit)) != 0;
        public static ulong SetBit(this ulong input, int bitIndex, bool value)
        {
            if (value) { return input |= (1ul << bitIndex); }
            return input &= ~(1ul << bitIndex);
        }

        public static bool IsBitSet(this ulong val, char bit) => (val & (1ul << bit)) != 0;
        public static ulong SetBit(this ulong input, char bitIndex, bool value)
        {
            if (value) { return input |= (1ul << bitIndex); }
            return input &= ~(1ul << bitIndex);
        }

        public static bool IsBitSet(this ulong val, byte bit) => (val & (1ul << bit)) != 0;
        public static ulong SetBit(this ulong input, byte bitIndex, bool value)
        {
            if (value) { return input |= (1ul << bitIndex); }
            return input &= ~(1ul << bitIndex);
        }

        public static bool IsBitSet(this ushort val, byte bit) => (val & (1 << bit)) != 0;
        public static ushort SetBit(this ushort input, byte bitIndex, bool value)
        {
            if (value) { return input |= (ushort)(1 << bitIndex); }
            return input &= (ushort)~(1 << bitIndex);
        }

        public static bool IsBitSet(this char val, byte bit) => (val & (1 << bit)) != 0;
        public static char SetBit(this char input, byte bitIndex, bool value)
        {
            if (value) { return input |= (char)(1 << bitIndex); }
            return input &= (char)~(1 << bitIndex);
        }

        public static bool IsBitSet(this short val, byte bit) => (val & (1 << bit)) != 0;
        public static short SetBit(this short input, byte bitIndex, bool value)
        {
            if (value) { return input |= (short)(1 << bitIndex); }
            return input &= (short)~(1 << bitIndex);
        }

        public static bool IsBitSet(this int val, int bit) => (val & (1 << bit)) != 0;
        public static int SetBit(this int input, int bitIndex, bool value)
        {
            if (value) { return input |= (1 << bitIndex); }
            return input &= ~(1 << bitIndex);
        }

        public static bool IsBitSet(this uint val, int bit) => (val & (1u << bit)) != 0;
        public static uint SetBit(this uint input, int bitIndex, bool value)
        {
            if (value) { return input |= (1u << bitIndex); }
            return input &= ~(1u << bitIndex);
        }

        public static bool IsBitSet(this byte val, int bit) => (val & (1 << bit)) != 0;
        public static byte SetBit(this byte input, int bitIndex, bool value)
        {
            if (value) { return input |= (byte)(1 << bitIndex); }
            return input &= (byte)~(1 << bitIndex);
        }

        public static bool IsBitSet(this sbyte val, int bit) => (val & (1 << bit)) != 0;
        public static sbyte SetBit(this sbyte input, int bitIndex, bool value)
        {
            if (value) { return input |= (sbyte)(1 << bitIndex); }
            return input &= (sbyte)~(1 << bitIndex);
        }

        #region Math Logic

        public static bool InRange(this int input, int min, int max) => input >= min & input <= max;

        public static float Clamp(float input, float min, float max) => input < min ? min : input > max ? max : input;
        public static int Clamp(int input, int min, int max) => input < min ? min : input > max ? max : input;

        public static float Sign(float input) => input < 0 ? -1 : 1;

        public static int CeilToInt(float f) => (int)System.Math.Ceiling(f);
        public static int RoundToInt(float f) => (int)System.Math.Round(f);
        public static int FloorToInt(float f) => (int)System.Math.Floor(f);

        public static float Repeat(float t, float length) => Clamp(t - (float)System.Math.Floor(t / length) * length, 0f, length);

        #region Comparison

        public static bool CompareTo(this float input, float other, ComparisonType type)
        {
            switch (type)
            {
                default:
                    return input == other;
                case ComparisonType.NOT_EQUAL:
                    return input != other;

                case ComparisonType.EQUAL_ABS:
                    return input == System.Math.Abs(other);
                case ComparisonType.NOT_EQUAL_ABS:
                    return input != System.Math.Abs(other);


                case ComparisonType.GREATER_OR_EQUAL:
                    return input >= other;
                case ComparisonType.GREATER_OR_EQUAL_ABS:
                    return input >= System.Math.Abs(other);

                case ComparisonType.GREATER_THAN:
                    return input > other;
                case ComparisonType.GREATER_THAN_ABS:
                    return input > System.Math.Abs(other);


                case ComparisonType.LESS_OR_EQUAL:
                    return input <= other;
                case ComparisonType.LESS_OR_EQUAL_ABS:
                    return input <= System.Math.Abs(other);

                case ComparisonType.LESS_THAN:
                    return input < other;
                case ComparisonType.LESS_THAN_ABS:
                    return input < System.Math.Abs(other);
            }
        }
        public static bool CompareTo(this int input, int other, ComparisonType type)
        {
            switch (type)
            {
                default:
                    return input == other;
                case ComparisonType.NOT_EQUAL:
                    return input != other;

                case ComparisonType.EQUAL_ABS:
                    return input == System.Math.Abs(other);
                case ComparisonType.NOT_EQUAL_ABS:
                    return input != System.Math.Abs(other);


                case ComparisonType.GREATER_OR_EQUAL:
                    return input >= other;
                case ComparisonType.GREATER_OR_EQUAL_ABS:
                    return input >= System.Math.Abs(other);

                case ComparisonType.GREATER_THAN:
                    return input > other;
                case ComparisonType.GREATER_THAN_ABS:
                    return input > System.Math.Abs(other);


                case ComparisonType.LESS_OR_EQUAL:
                    return input <= other;
                case ComparisonType.LESS_OR_EQUAL_ABS:
                    return input <= System.Math.Abs(other);

                case ComparisonType.LESS_THAN:
                    return input < other;
                case ComparisonType.LESS_THAN_ABS:
                    return input < System.Math.Abs(other);
            }
        }


        #endregion

        #endregion

        #region Wave Functions
        public static float Sin(float f) => (float)System.Math.Sin(f);
        public static float Cos(float f) => (float)System.Math.Cos(f);

        public static float Asin(float f) => (float)System.Math.Asin(f);
        public static float Acos(float f) => (float)System.Math.Acos(f);

        public static float Square(float t, bool sine = true) => System.Math.Sign(sine ? Sin(TWO_PI * t) : Cos(TWO_PI * t));
        public static float Triangle(float t) => 1f - 4f * (float)System.Math.Abs(System.Math.Round(t - 0.25f) - (t - 0.25f));
        public static float Sawtooth(float t) => 2f * (t - (float)System.Math.Floor(t + 0.5f));
        #endregion

        #region Vector Math

        #region Vector Rotation
        public static float Angle(this Vector2 a, Vector2 b)
        {
            float sqr = (float)System.Math.Sqrt((a.SqrMagnitude * b.SqrMagnitude));
            return sqr < Epsilon ? 0.0f : (float)System.Math.Acos(Clamp(Vector2.Dot(a, b) / sqr, -1f, 1f)) * Rad2Deg;
        }

        public static float SignedAngle(this Vector2 a, Vector2 b) => Angle(a, b) * Sign(a.x * b.y - a.y * b.x);

        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            float rads = degrees * Deg2Rad;

            float sin = Sin(rads);
            float cos = Cos(rads);

            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
        #endregion

        public static Vector2 QuadraticBezierCurve(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;

            return uu * p0 + (2.0f * u * t) * p1 + tt * p2;
        }

        public static Vector2 CubicBezierCurve(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1.0f - t;
            float t2 = t * t;
            float u2 = u * u;
            float u3 = u2 * u;
            float t3 = t2 * t;

            return u3 * p0 +
                (3.0f * u2 * t) * p1 +
                (3.0f * u * t2) * p2 +
                t3 * p3;
        }

        #region Distance

        public static float SqrDistance(float x1, float x2, float y1, float y2)
        {
            float x = x1 - x2;
            float y = y1 - y2;

            return (x * x) + (y * y);
        }

        public static float SqrDistance(Vector2 a, Vector2 b) => SqrDistance(a.x, b.x, a.y, b.y);
        public static float Distance(Vector2 a, Vector2 b) => (float)System.Math.Sqrt(SqrDistance(a, b));


        #endregion
        #endregion

        public static float Max(float a, float b, float c)
        {
            if(a > b) { return c > a ? c : a; }
            return b > c ? b : c;
        }

        public static float Min(float a, float b, float c)
        {
            if (a < b) { return c < a ? c : a; }
            return b < c ? b : c;
        }

        public static float Max(out int index, params float[] vals)
        {
            index = -1;
            float max = float.MinValue;

            for (int i = 0; i < vals.Length; i++)
            {
                var f = vals[i];
                if (f > max) { max = f; index = i; }
            }
            return max;
        }

        public static float Min(out int index, params float[] vals)
        {
            index = -1;
            float min = float.MaxValue;

            for (int i = 0; i < vals.Length; i++)
            {
                var f = vals[i];
                if (f < min) { min = f; index = i; }
            }
            return min;
        }

        public static float Mod(float value, float length) => value - (float)Math.Floor(value / length) * length;

        #region Interpolation

        public static float InverseLerpAngle(float a, float b, float t)
        {
            if(a == b) { return 0; }

            if(t >= 180)
            {
                a = a > 360.0f ? a - 360.0f : a;
                return InverseLerp(a, b, t);
            }

            if (b > 360.0f)
            {
                b -= 360.0f;
            }
            return InverseLerp(a, b, t);
        }

        public static float LerpAngle(float a, float b, float t)
        {
            float delta = Repeat((b - a) * t, 360.0f);
            if(delta > 180.0f)
            {
                delta -= 360.0f;
            }

            float res = a + delta;
            return res < 0 ? res + 360.0f : res;
        }

        public static float SmoothStep(float from, float to, float t)
        {
            t = t > 1.0f ? 1.0f : t < 0 ? 0 : t;
            t = -2f * t * t * t + 3f * t * t;
            return to * t + from * (1f - t);
        }

        public static float InverseLerp(float a, float b, float value) => a != b ? (value - a) / (b - a) : 0f;

        public static float Lerp(float a, float b, float t) => a + (b - a) * t;
        public static int Lerp(int a, int b, float t) => (int)(a + (b - a) * t);

        public static byte Lerp(byte a, byte b, float t) => (byte)(a + (b - a) * t);
        public static byte Lerp(byte a, byte b, byte t) => (byte)(a + (b - a) * (t * (1.0f / 255.0f)));

        public static float EaseIn(float t) => t * t;
        public static float EaseOut(float t) => t * (2f - t);

        #endregion
    }
}