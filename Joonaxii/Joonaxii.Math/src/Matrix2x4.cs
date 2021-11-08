using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.MathJX.src
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct Matrix2x4
    {
        public static Matrix2x4 zero => new Matrix2x4(0, 0, 0, 0, 0, 0, 0, 0);
        /////////////////////
        // m00 m01 m02 m03 //
        //                 //
        // m10 m11 m12 m13//
        /////////////////////

        private float _m00;
        private float _m01;
        private float _m02;
        private float _m03;

        private float _m10;
        private float _m11;
        private float _m12;
        private float _m13;

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
            _m00 = point.x;
            _m10 = point.y;

            _m01 = scale.x;
            _m11 = scale.y;


            float rads = zRotation * MathJX.Deg2Rad;
            _m02 = (float)Math.Cos(rads);
            _m12 = (float)Math.Sin(rads);

            _m03 = -_m12;
            _m13 = _m02;
            return this;
        }

        public float Determinant() => _m00 * _m11 - _m01 * _m10;
        public Vector2 MultiplyPoint(Vector2 point) => new Vector2(point.x * _m01 + _m00, point.y * _m11 + _m10);
        public Vector2 MultiplyVector(Vector2 vec) => new Vector2(vec.x * _m01, vec.y * _m11);

        public override string ToString() => $"M00: {_m00}, M01: {_m01}, M10: {_m10}, M11: {_m11}";
    }
}
