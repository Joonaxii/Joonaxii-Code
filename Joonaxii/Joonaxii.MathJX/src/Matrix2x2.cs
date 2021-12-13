using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.MathJX
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct Matrix2x2
    {
        public static Matrix2x2 zero => new Matrix2x2(0, 0, 0, 0);
        public static Matrix2x2 identity => new Matrix2x2(1, 0, 0, 1);

        /////////////
        // m00 m01 //
        //         //
        // m10 m11 //
        /////////////

        private float _m00;
        private float _m01;
        private float _m10;
        private float _m11;

        public float this[int r, int c]
        {
            get => this[r * 2 + c];
            set => this[r * 2 + c] = value;
        }

        public float this[int i]
        {
            get
            {
                switch (i)
                {
                    default: throw new IndexOutOfRangeException($"Invalid matrix index '{i}'");

                    case 0: return _m00;
                    case 1: return _m01;
                    case 2: return _m10;
                    case 3: return _m11;
                }
            }

            set
            {
                switch (i)
                {
                    default: throw new IndexOutOfRangeException($"Invalid matrix index '{i}'");
                    case 0:  _m00 = value; break;
                    case 1:  _m01 = value; break;
                    case 2:  _m10 = value; break;
                    case 3:  _m11 = value; break;
                }
            }
        }

        public Matrix2x2(float m00, float m01, float m10, float m11)
        {
            _m00 = m00;
            _m01 = m01;
            _m10 = m10;
            _m11 = m11;
        }

        public Matrix2x2(Vector2 c0, Vector2 c1)
        {
            _m00 = c0.x;
            _m10 = c0.y;

            _m01 = c1.x;
            _m11 = c1.y;
        }

        public static Matrix2x2 TS(Vector2 point, Vector2 scale) => new Matrix2x2().SetTS(point, scale);
        public Matrix2x2 SetTS(Vector2 point, Vector2 scale)
        {
            _m00 = point.x;
            _m10 = point.y;

            _m01 = scale.x;
            _m11 = scale.y;
            return this;
        }

        public static Matrix2x2 operator *(Matrix2x2 lhs, Matrix2x2 rhs)
        {
            Matrix2x2 temp = new Matrix2x2
            {
                _m00 = (lhs._m00 * rhs._m00) + (lhs._m01 * rhs._m10),
                _m01 = (lhs._m00 * rhs._m01) + (lhs._m01 * rhs._m11),

                _m10 = (lhs._m10 * rhs._m00) + (lhs._m11 * rhs._m10),
                _m11 = (lhs._m10 * rhs._m01) + (lhs._m11 * rhs._m11)
            };
            return temp;
        }

        public float Determinant() => _m00 * _m11 - _m01 * _m10;
        public Vector2 MultiplyPoint(Vector2 point) => new Vector2(point.x * _m01 + _m00, point.y * _m11 + _m10);
        public Vector2 MultiplyVector(Vector2 vec) => new Vector2(vec.x * _m01, vec.y * _m11);

        public override string ToString() => $"M00: {_m00}, M01: {_m01}, M10: {_m10}, M11: {_m11}";
    }
}
