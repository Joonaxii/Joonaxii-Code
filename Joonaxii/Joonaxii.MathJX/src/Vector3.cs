using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.MathJX
{
    [StructLayout(LayoutKind.Sequential, Size = 12, Pack = 4)]
    public struct Vector3 : IEquatable<Vector3>
    {
        public static Vector3 zero = new Vector3(0, 0);
        public static Vector3 one = new Vector3(1.0f, 1.0f);
        public static Vector3 up = new Vector3(0, 1.0f);
        public static Vector3 down = new Vector3(0, -1.0f);
        public static Vector3 right = new Vector3(1.0f, 0.0f);
        public static Vector3 left = new Vector3(-1.0f, 0.0f);

        public float x;
        public float y;
        public float z;

        public float Magnitude => (float)System.Math.Sqrt(SqrMagnitude);
        public Vector3 Normalized => new Vector3(x, y, z).Normalize();
        public float SqrMagnitude => x * x + y * y + z + z;

        public Vector3(float _x, float _y)
        {
            x = _x;
            y = _y;
            z = 0;
        }

        public Vector3(float _x, float _y, float _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public void Set(float _x, float _y)
        {
            x = _x;
            y = _y;
        }

        public void Set(float _x, float _y, float _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        public static float Dot(Vector3 lhs, Vector3 rhs) => lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;

        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime, float deltaTime, float maxSpeed = float.MaxValue)
        {
            smoothTime = System.Math.Max(0.0001f, smoothTime);

            float stHalf = smoothTime * 0.5f;
            float stDelta = stHalf * deltaTime;

            float d = 1.0f / (1.0f + stDelta + 0.48f * stDelta * stDelta + 0.235f * stDelta * stDelta * stDelta);
            Vector3 diff = current - target;
            Vector3 tgt = target;

            float maxLength = maxSpeed * smoothTime;
            diff = ClampMagnitude(diff, maxLength);
            target = current - diff;

            Vector3 velDiff = (currentVelocity + diff * stHalf) * deltaTime;
            currentVelocity = (currentVelocity - velDiff * stHalf) * d;

            Vector3 tgtDiff = target + (diff + velDiff) * d;
            if (Dot(tgt - current, tgtDiff - tgt) > 0f)
            {
                tgtDiff = tgt;
                currentVelocity = (tgtDiff - tgt) / deltaTime;
            }
            return tgtDiff;
        }

        public static Vector3 Cross(Vector3 lhs, Vector3 rhs)
        {
            return new Vector3(
                lhs.y * rhs.z - lhs.z * rhs.y,
                lhs.z * rhs.x - lhs.x * rhs.z,
                lhs.x * rhs.y - lhs.y * rhs.x);
        }

        public bool Equals(Vector3 other) => x == other.x && y == other.y && z == other.z;

        public override bool Equals(object obj) => obj is Vector3 && Equals((Vector3)obj);
        public override string ToString() => $"({x}, {y}, {z})";

        public override int GetHashCode()
        {
            var hashCode = 1502939027;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            hashCode = hashCode * -1521134295 + z.GetHashCode();
            return hashCode;
        }

        public static float InverseLerp(Vector3 from, Vector3 to, Vector3 value)
        {
            Vector3 ab = to - from;
            Vector3 av = value - from;
            return Vector3.Dot(av, ab) / Vector3.Dot(ab, ab);
        }

        public Vector3 Normalize()
        {
            float magnitude = Magnitude;
            return (this = magnitude > 1E-05f ? this / magnitude : zero);
        }

        public static Vector3 ClampMagnitude(Vector3 vector, float maxLength)
        {
            return vector.SqrMagnitude > maxLength * maxLength ? vector.Normalized * maxLength : vector;
        }

        public static Vector3 Reflect(Vector3 vector, Vector3 normal)
        {
            return vector - (2f * Dot(vector, normal) * normal);
        }

        public static explicit operator Vector2Int(Vector3 vector) => new Vector2Int(Maths.RoundToInt(vector.x), Maths.RoundToInt(vector.y));
        public static explicit operator Vector3Int(Vector3 vector) => new Vector3Int(Maths.RoundToInt(vector.x), Maths.RoundToInt(vector.y), Maths.RoundToInt(vector.z));

        public static implicit operator Vector3(Vector2Int vector) => new Vector3(vector.x, vector.y);
        public static implicit operator Vector3(Vector3Int vector) => new Vector3(vector.x, vector.y, vector.z);

        public static bool operator ==(Vector3 vector1, Vector3 vector2) => vector1.Equals(vector2);
        public static bool operator !=(Vector3 vector1, Vector3 vector2) => !(vector1 == vector2);

        public static Vector3 operator *(Vector3 vector1, float val) => new Vector3(vector1.x * val, vector1.y * val, vector1.z * val);
        public static Vector3 operator /(Vector3 vector1, float val) => new Vector3(vector1.x / val, vector1.y / val, vector1.z / val);

        public static Vector3 operator *(float val, Vector3 vector1) => new Vector3(vector1.x * val, vector1.y * val, vector1.z * val);
        public static Vector3 operator /(float val, Vector3 vector1) => new Vector3(vector1.x / val, vector1.y / val, vector1.z / val);

        public static Vector3 operator *(Vector3 vector1, Vector3 vector2) => new Vector3(vector1.x * vector2.x, vector1.y * vector2.y, vector1.z * vector2.z);
        public static Vector3 operator /(Vector3 vector1, Vector3 vector2) => new Vector3(vector1.x / vector2.x, vector1.y / vector2.y, vector1.z / vector2.z);

        public static Vector3 operator +(Vector3 vector1, Vector3 vector2) => new Vector3(vector1.x + vector2.x, vector1.y + vector2.y, vector1.z + vector2.z);
        public static Vector3 operator -(Vector3 vector1, Vector3 vector2) => new Vector3(vector1.x - vector2.x, vector1.y - vector2.y, vector1.z - vector2.z);

        public static Vector3 operator -(Vector3 vector1) => new Vector3(-vector1.x, -vector1.y, -vector1.z);
    }
}
