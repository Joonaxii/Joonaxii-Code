using System;
using System.Runtime.InteropServices;

namespace Joonaxii.Math
{
    [StructLayout(LayoutKind.Sequential, Size = 12, Pack = 4)]
    public struct Vector3Int : IEquatable<Vector3Int>
    {
        public int x;
        public int y;
        public int z;

        public Vector3Int(int _x, int _y, int _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public void Set(int _x, int _y, int _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public bool Equals(Vector3Int other) => x == other.x && y == other.y && z == other.z;
        public override bool Equals(object obj) => obj is Vector3Int && Equals((Vector3Int)obj);

        public override string ToString() => $"XY: ({x}, {y})";

        public override int GetHashCode()
        {
            var hashCode = 1502939027;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Vector3Int vector1, Vector3Int vector2) => vector1.Equals(vector2);
        public static bool operator !=(Vector3Int vector1, Vector3Int vector2) => !(vector1 == vector2);
        public static Vector3Int operator -(Vector3Int vector) => new Vector3Int(-vector.x, -vector.y, -vector.z);
    }
}