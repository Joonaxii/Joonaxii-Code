using System;
using System.Runtime.InteropServices;

namespace Joonaxii.MathJX
{
    [StructLayout(LayoutKind.Sequential, Size = 16, Pack = 4)]
    public struct Vector4 : IEquatable<Vector4>
    {
        public static Vector4 zero { get; } = new Vector4(0, 0, 0.0f, 0.0f);
        public static Vector4 one { get; } = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        public static Vector4 up { get; } = new Vector4(0, 1.0f, 0.0f, 0.0f);
        public static Vector4 down { get; } = new Vector4(0, -1.0f, 0.0f, 0.0f);
        public static Vector4 right { get; } = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
        public static Vector4 left { get; } = new Vector4(-1.0f, 0.0f, 0.0f, 0.0f);

        public float x;
        public float y;
        public float z;
        public float w;

        public float Magnitude => (float)Math.Sqrt(SqrMagnitude);
        public Vector4 Normalized => new Vector4(x, y, z, w).Normalize();
        public float SqrMagnitude => x * x + y * y + z * z + w * w;

        public Vector4(float _x, float _y, float _z, float _w)
        {
            x = _x;
            y = _y;
            z = _z;
            w = _w;
        }

        public void Set(float _x, float _y, float _z, float _w)
        {
            x = _x;
            y = _y;
            z = _z;
            w = _w;
        }

        public static Vector4 Lerp(Vector4 a, Vector4 b, float t) => 
            new Vector4(
                a.x + (b.x - a.x) * t, 
                a.y + (b.y - a.y) * t,
                a.z + (b.z - a.z) * t,
                a.w + (b.w - a.w) * t);
        public static float Dot(Vector4 lhs, Vector4 rhs) => lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z + lhs.w * rhs.w;

        public static float InverseLerp(Vector4 from, Vector4 to, Vector4 value)
        {
            Vector4 ab = to - from;
            Vector4 av = value - from;
            return Vector4.Dot(av, ab) / Vector4.Dot(ab, ab);
        }

        public static Vector4 SmoothDamp(Vector4 current, Vector4 target, ref Vector4 currentVelocity, float smoothTime, float deltaTime, float maxSpeed = float.MaxValue)
        {
            smoothTime = System.Math.Max(0.0001f, smoothTime);

            float stHalf = smoothTime * 0.5f;
            float stDelta = stHalf * deltaTime;

            float d = 1.0f / (1.0f + stDelta + 0.48f * stDelta * stDelta + 0.235f * stDelta * stDelta * stDelta);
            Vector4 diff = current - target;
            Vector4 tgt = target;

            float maxLength = maxSpeed * smoothTime;
            diff = ClampMagnitude(diff, maxLength);
            target = current - diff;

            Vector4 velDiff = (currentVelocity + diff * stHalf) * deltaTime;
            currentVelocity = (currentVelocity - velDiff * stHalf) * d;

            Vector4 tgtDiff = target + (diff + velDiff) * d;
            if (Dot(tgt - current, tgtDiff - tgt) > 0f)
            {
                tgtDiff = tgt;
                currentVelocity = (tgtDiff - tgt) / deltaTime;
            }
            return tgtDiff;
        }

        public bool Equals(Vector4 other) => x == other.x && y == other.y && z == other.z && w == other.w;

        public override bool Equals(object obj) => obj is Vector2 && Equals((Vector2)obj);
        public override string ToString() => $"({x}, {y}, {z}, {w})";

        public override int GetHashCode()
        {
            var hashCode = 1502939027;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            hashCode = hashCode * -1521134295 + z.GetHashCode();
            hashCode = hashCode * -1521134295 + w.GetHashCode();
            return hashCode;
        }

        public Vector4 Normalize()
        {
            float magnitude = Magnitude;
            return (this = magnitude > 1E-05f ? this / magnitude : zero);
        }

        public static Vector4 ClampMagnitude(Vector4 vector, float maxLength)
        {
            return vector.SqrMagnitude > maxLength * maxLength ? vector.Normalized * maxLength : vector;
        }

        public static Vector4 Reflect(Vector4 vector, Vector4 normal)
        {
            return vector - (2f * Dot(vector, normal) * normal);
        }

        public static explicit operator Vector2Int(Vector4 vector) => new Vector2Int(Maths.RoundToInt(vector.x), Maths.RoundToInt(vector.y));
        public static explicit operator Vector3Int(Vector4 vector) => new Vector3Int(Maths.RoundToInt(vector.x), Maths.RoundToInt(vector.y), 0);

        public static implicit operator Vector4(Vector2Int vector) => new Vector4(vector.x, vector.y, 0, 0);
        public static implicit operator Vector4(Vector3Int vector) => new Vector4(vector.x, vector.y, vector.z, 0);

        public static bool operator ==(Vector4 vector1, Vector4 vector2) => vector1.Equals(vector2);
        public static bool operator !=(Vector4 vector1, Vector4 vector2) => !(vector1 == vector2);

        public static Vector4 operator *(Vector4 vector1, float val) => new Vector4(vector1.x * val, vector1.y * val, vector1.z * val, vector1.w * val);
        public static Vector4 operator /(Vector4 vector1, float val) => new Vector4(vector1.x / val, vector1.y / val, vector1.z / val, vector1.w / val);

        public static Vector4 operator *(float val, Vector4 vector1) => new Vector4(vector1.x * val, vector1.y * val, vector1.z * val, vector1.w * val);
        public static Vector4 operator /(float val, Vector4 vector1) => new Vector4(vector1.x / val, vector1.y / val, vector1.z / val, vector1.w / val);

        public static Vector4 operator *(Vector4 vector1, Vector4 vector2) => new Vector4(vector1.x * vector2.x, vector1.y * vector2.y, vector1.z * vector2.z, vector1.w * vector2.w);
        public static Vector4 operator /(Vector4 vector1, Vector4 vector2) => new Vector4(vector1.x / vector2.x, vector1.y / vector2.y, vector1.z / vector2.z, vector1.w / vector2.w);

        public static Vector4 operator +(Vector4 vector1, Vector4 vector2) => new Vector4(vector1.x + vector2.x, vector1.y + vector2.y, vector1.z + vector2.z, vector1.w + vector2.w);
        public static Vector4 operator -(Vector4 vector1, Vector4 vector2) => new Vector4(vector1.x - vector2.x, vector1.y - vector2.y, vector1.z - vector2.z, vector1.w - vector2.w);

        public static Vector4 operator -(Vector4 vector1) => new Vector4(-vector1.x, -vector1.y, -vector1.z, -vector1.w);
    }

}
