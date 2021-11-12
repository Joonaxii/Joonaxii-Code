using System;
using System.Runtime.InteropServices;

namespace Joonaxii.Math
{
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    public struct Matrix2x4
    {
        public static Matrix2x4 zero => new Matrix2x4(0, 0, 0, 0, 0, 0, 0, 0);
        /////////////////////
        // m00 m01 m02 m03 //
        //                 //
        // m10 m11 m12 m13 //
        /////////////////////
        //Column 0 is scale//
        //Columns 1-2 are- //
        //-rotation.       //
        //Column 3 is pos  //
        /////////////////////

        private float _m00;
        private float _m01;
        private float _m02;
        private float _m03;

        private float _m10;
        private float _m11;
        private float _m12;
        private float _m13;

        public float this[int r, int c]
        {
            get => this[r * 4 + c];
            set => this[r * 4 + c] = value;
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
                    case 2: return _m02;
                    case 3: return _m03;
                    case 4: return _m10;
                    case 5: return _m11;
                    case 6: return _m12;
                    case 7: return _m13;
                }
            }

            set
            {
                switch (i)
                {
                    default: throw new IndexOutOfRangeException($"Invalid matrix index '{i}'");
                    case 0: _m00 = value; break;
                    case 1: _m01 = value; break;
                    case 2: _m02 = value; break;
                    case 3: _m03 = value; break;
                    case 4: _m10 = value; break;
                    case 5: _m11 = value; break;
                    case 6: _m12 = value; break;
                    case 7: _m13 = value; break;
                }
            }
        }

        public Matrix2x4(float m00, float m01, float m02, float m03, float m10, float m11, float m12, float m13)
        {
            _m00 = m00;
            _m01 = m01;
            _m02 = m02;
            _m03 = m03;

            _m10 = m10;
            _m11 = m11;
            _m12 = m12;
            _m13 = m13;
        }

        public Matrix2x4(Vector2 c0, Vector2 c1, Vector2 c2, Vector2 c3)
        {
            _m00 = c0.x;
            _m10 = c0.y;

            _m01 = c1.x;
            _m11 = c1.y;

            _m02 = c2.x;
            _m12 = c2.y;

            _m03 = c3.x;
            _m13 = c3.y;
        }

        public static Matrix2x4 TRS(Vector2 point, float zRotation, Vector2 scale) => new Matrix2x4().SetTRS(point, zRotation, scale);
        public Matrix2x4 SetTRS(Vector2 point, float zRotation, Vector2 scale)
        {
            _m00 = scale.x;
            _m10 = scale.y;

            float rads = zRotation * MathJX.Deg2Rad;
            _m01 = (float)System.Math.Cos(rads);
            _m11 = (float)System.Math.Sin(rads);

            _m02 = -_m11;
            _m12 = _m01;

            _m03 = point.x;
            _m13 = point.y;
            return this;
        }

        public Vector2 MultiplyPoint(Vector2 point) => new Vector2((point.x * _m01 + point.y * _m02) * _m00 + _m03, (point.y * _m12 + point.x * _m11) * _m10 + _m13);
        public Vector2 MultiplyVector(Vector2 vec) => new Vector2((vec.x * _m01 + vec.y * _m02) * _m00, (vec.y * _m12 + vec.x * _m11) * _m10);
        public Vector2 ScaleVector(Vector2 vec) => new Vector2(vec.x * _m00, vec.y * _m10);
        public float Rotate(float rotation) => rotation + MathJX.Acos(_m01) * MathJX.Rad2Deg;

        public override string ToString() => $"M00: {_m00}, M01: {_m01}, M02: {_m02}, M03: {_m03}, M10: {_m10}, M11: {_m11}, M12: {_m12}, M13: {_m13}";
    }
}
