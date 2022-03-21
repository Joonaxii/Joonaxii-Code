using System;
using System.Runtime.InteropServices;

using MathS = System.Math;

namespace Joonaxii.MathJX
{
    [StructLayout(LayoutKind.Sequential, Size = 36)]
    public unsafe struct Matrix3x3
    {
        public static Matrix3x3 identity { get; } = new Matrix3x3(1, 0, 0, 0, 1, 0, 0, 0, 1);
        public static Matrix3x3 zero { get; } = new Matrix3x3(0, 0, 0, 0, 0, 0, 0, 0, 0);
        /////////////////
        // m00 m01 m02 //
        //             //
        // m10 m11 m12 //
        //             //
        // m20 m21 m22 //
        /////////////////

        private fixed float _m[9];

        public float this[int r, int c]
        {
            get => this[r * 3 + c];
            set => this[r * 3 + c] = value;
        }

        public float this[int i]
        {
            get => i < 0 | i > 8 ? 0 : _m[i];
            set
            {
                if(i < 0 | i > 8) { return; }
                _m[i] = value;
            }
        }

        public Matrix3x3(float m00, float m01, float m02, float m10, float m11, float m12, float m20, float m21, float m22)
        {
            _m[0] = m00;
            _m[1] = m01;
            _m[2] = m02;

            _m[3] = m10;
            _m[4] = m11;
            _m[5] = m12;

            _m[6] = m20;
            _m[7] = m21;
            _m[8] = m22;
        }

        public Matrix3x3(Vector3 c0, Vector3 c1, Vector3 c2)
        {
            _m[0] = c0.x;
            _m[3] = c0.y;
            _m[6] = c0.z;

            _m[1] = c1.x;
            _m[4] = c1.y;
            _m[7] = c1.z;

            _m[2] = c2.x;
            _m[5] = c2.y;
            _m[8] = c2.z;
        }

        public static Matrix3x3 TRS(Vector2 point, float zRotation, Vector2 scale) => new Matrix3x3().SetTRS(point, zRotation, scale);
        public Matrix3x3 SetTRS(Vector2 point, float zRotation, Vector2 scale)
        {
            SetScale(scale);
            SetRotation(zRotation);
            SetPosition(point);
            return this;
        }

        public float Determinant() => (_m[0] * Det2x2(4, 8, 5, 7)) -
                                      (_m[1] * Det2x2(3, 8, 5, 6)) +
                                      (_m[2] * Det2x2(3, 5, 4, 6));

        private float Det2x2(int a, int b, int c, int d) => (this[a] * this[b]) - (this[c] * this[d]);

        public Matrix3x3 Inverse()
        {
            float det = Determinant();
            if (det == 0) { return zero; }

            Matrix3x3 tr = Transpose();
            det = 1.0f / det;

            //Inverse matrix's values are 
            //determinants of the transposed
            //matrix's minor matrices times
            //1.0 / original matrix's 
            //determinant multiplied by
            //this config ---
            ////////////////|////////////////
            //  A  B  C //  -->//  +  -  + //
            //  D  E  F //  X  //  -  +  - //
            //  G  H  I //     //  +  -  + //
            /////////////////////////////////

            float detA = tr.Det2x2(4, 5, 7, 8) * det; //Scale:  X
            float detB = -tr.Det2x2(3, 5, 6, 8) * det; //Rot A:  Cos(a)
            float detC = tr.Det2x2(3, 4, 6, 7) * det; //Rot B: -Sin(a)

            float detD = -tr.Det2x2(1, 2, 7, 8) * det; //Scale:  Y
            float detE = tr.Det2x2(0, 2, 6, 8) * det; //Rot C:  Sin(a)
            float detF = -tr.Det2x2(0, 1, 6, 7) * det; //Rot D:  Cos(a)

            float detG = tr.Det2x2(1, 2, 4, 5) * det; //Pos:    X
            float detH = -tr.Det2x2(0, 2, 3, 5) * det; //Pos:    Y
            float detI = tr.Det2x2(0, 1, 3, 4) * det; //Rot E:  Angles

            return new Matrix3x3(detA, detB, detC,
                                 detD, detE, detF,
                                 detG, detH, detI);
        }

        public Vector2 InverseScale()
        {
            float det = Determinant();
            if (det == 0) { return Vector2.zero; }
            Matrix3x3 tr = Transpose();

            det = 1.0f / det;
            float detA = tr.Det2x2(4, 5, 7, 8) * det;
            float detD = -tr.Det2x2(1, 2, 7, 8) * det;

            return new Vector2(detA, detD);
        }
        public float InverseRotation()
        {
            float det = Determinant();
            if (det == 0) { return 0; }

            Matrix3x3 tr = Transpose();

            det = 1.0f / det;
            return tr.Det2x2(0, 1, 3, 4) * det;
        }
        public Vector2 InversePosition()
        {
            float det = Determinant();
            if (det == 0) { return Vector2.zero; }
            Matrix3x3 tr = Transpose();

            det = 1.0f / det;
            float detG = tr.Det2x2(1, 2, 4, 5) * det;
            float detH = -tr.Det2x2(0, 2, 3, 5) * det;

            return new Vector2(detG, detH);
        }

        public Vector2 MultiplyPoint(Vector2 point)
        {
            float pX = point.x * _m[0];
            float pY = point.y * _m[3];
            return new Vector2((pX * _m[1] + pY * _m[2]) + _m[6], (pY * _m[5] + pX * _m[4]) + _m[7]);
        }

        public Vector2 MultiplyVector(Vector2 vec)
        {
            float pX = vec.x * _m[0];
            float pY = vec.y * _m[3];
            return new Vector2((pX * _m[1] + pY * _m[2]), (pY * _m[5] + pX * _m[4]));
        }

        public Vector2 RotatePoint(Vector2 vec) =>
            new Vector2(
            (vec.x * _m[1] + vec.y * _m[2]),
            (vec.y * _m[5] + vec.x * _m[4]));

        public Vector2 MultiplyAbsVector(Vector2 vec)
        {
            float pX = vec.x * _m[0];
            float pY = vec.y * _m[3];
            return new Vector2((pX * MathS.Abs(_m[1]) + pY * MathS.Abs(_m[2])),
                        (pY * MathS.Abs(_m[5]) + pX * MathS.Abs(_m[4])));
        }

        public Vector2 ScaleVector(Vector2 vec) => new Vector2(vec.x * _m[0], vec.y * _m[3]);
        public float Rotate(float rotation) => rotation + _m[8];

        public Matrix3x3 Transpose()
        {
            return new Matrix3x3(_m[0], _m[3], _m[6],
                                 _m[1], _m[4], _m[7],
                                 _m[2], _m[5], _m[8]);
        }

        public Vector2 GetPosition() => new Vector2(_m[6], _m[7]);
        public float GetRotation() => _m[8];
        public Vector2 GetScale() => new Vector2(_m[0], _m[3]);

        public void SetScale(Vector2 scale)
        {
            _m[0] = scale.x;
            _m[3] = scale.y;
        }
        public void SetRotation(float zRotation)
        {
            float rads = zRotation * Maths.Deg2Rad;
            _m[1] = Maths.Cos(rads);
            _m[4] = Maths.Sin(rads);

            _m[2] = -_m[4];
            _m[5] = _m[1];

            _m[8] = zRotation;
        }
        public void SetPosition(Vector2 point)
        {
            _m[6] = point.x;
            _m[7] = point.y;
        }

        public override string ToString() => $"M00: {_m[0]}\t M01: {_m[1]}\t M02: {_m[2]}\n M10: {_m[3]}\t M11: {_m[4]}\t M12: {_m[5]}\n M20: {_m[6]}\t M21: {_m[7]}\t M22: {_m[8]}";
    }
}
