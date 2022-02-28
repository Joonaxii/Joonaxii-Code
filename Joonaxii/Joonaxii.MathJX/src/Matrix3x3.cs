using System;
using System.Runtime.InteropServices;

using MathS = System.Math;

namespace Joonaxii.MathJX
{
    [StructLayout(LayoutKind.Sequential, Size = 36)]
    public struct Matrix3x3 
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

        private float _m00;
        private float _m01;
        private float _m02;

        private float _m10;
        private float _m11;
        private float _m12;

        private float _m20;
        private float _m21;
        private float _m22;

        public float this[int r, int c]
        {
            get => this[r * 3 + c];
            set => this[r * 3 + c] = value;
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
                    case 3: return _m10;
                    case 4: return _m11;
                    case 5: return _m12;
                    case 6: return _m20;
                    case 7: return _m21;
                    case 8: return _m22;
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
                    case 3: _m10 = value; break;
                    case 4: _m11 = value; break;
                    case 5: _m12 = value; break;
                    case 6: _m20 = value; break;
                    case 7: _m21 = value; break;
                    case 8: _m22 = value; break;
                }
            }
        }

        public Matrix3x3(float m00, float m01, float m02, float m10, float m11, float m12, float m20, float m21, float m22)
        {
            _m00 = m00;
            _m01 = m01;
            _m02 = m02;

            _m10 = m10;
            _m11 = m11;
            _m12 = m12;

            _m20 = m20;
            _m21 = m21;
            _m22 = m22;
        }

        public Matrix3x3(Vector3 c0, Vector3 c1, Vector3 c2)
        {
            _m00 = c0.x;
            _m10 = c0.y;
            _m20 = c0.z;

            _m01 = c1.x;
            _m11 = c1.y;
            _m21 = c1.z;

            _m02 = c2.x;
            _m12 = c2.y;
            _m22 = c2.z;
        }

        public static Matrix3x3 TRS(Vector2 point, float zRotation, Vector2 scale) => new Matrix3x3().SetTRS(point, zRotation, scale);
        public Matrix3x3 SetTRS(Vector2 point, float zRotation, Vector2 scale)
        {
            SetScale(scale);
            SetRotation(zRotation);
            SetPosition(point);
            return this;
        }

        public float Determinant() => (_m00 * Det2x2(4, 8, 5, 7)) - 
                                      (_m01 * Det2x2(3, 8, 5, 6)) + 
                                      (_m02 * Det2x2(3, 5, 4, 6));

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

            float detA =  tr.Det2x2(4, 5, 7, 8) * det; //Scale:  X
            float detB = -tr.Det2x2(3, 5, 6, 8) * det; //Rot A:  Cos(a)
            float detC =  tr.Det2x2(3, 4, 6, 7) * det; //Rot B: -Sin(a)

            float detD = -tr.Det2x2(1, 2, 7, 8) * det; //Scale:  Y
            float detE =  tr.Det2x2(0, 2, 6, 8) * det; //Rot C:  Sin(a)
            float detF = -tr.Det2x2(0, 1, 6, 7) * det; //Rot D:  Cos(a)

            float detG =  tr.Det2x2(1, 2, 4, 5) * det; //Pos:    X
            float detH = -tr.Det2x2(0, 2, 3, 5) * det; //Pos:    Y
            float detI =  tr.Det2x2(0, 1, 3, 4) * det; //Rot E:  Angles

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
            float pX = point.x * _m00;
            float pY = point.y * _m10;
            return new Vector2((pX * _m01 + pY * _m02) + _m20, (pY * _m12 + pX * _m11) + _m21);
        }

        public Vector2 MultiplyVector(Vector2 vec)
        {
            float pX = vec.x * _m00;
            float pY = vec.y * _m10;
            return new Vector2((pX * _m01 + pY * _m02), (pY * _m12 + pX * _m11));
        }

        public Vector2 RotatePoint(Vector2 vec) => 
            new Vector2(
            (vec.x * _m01 + vec.y * _m02), 
            (vec.y * _m12 + vec.x * _m11));

        public Vector2 MultiplyAbsVector(Vector2 vec) 
        {
            float pX = vec.x * _m00;
            float pY = vec.y * _m10;
            return new Vector2((pX * MathS.Abs(_m01) + pY * MathS.Abs(_m02)),
                        (pY * MathS.Abs(_m12) + pX * MathS.Abs(_m11)));
        }

        public Vector2 ScaleVector(Vector2 vec) => new Vector2(vec.x * _m00, vec.y * _m10);
        public float Rotate(float rotation) => rotation + _m22;

        public Matrix3x3 Transpose()
        {
            return new Matrix3x3(_m00, _m10, _m20, 
                                 _m01, _m11, _m21, 
                                 _m02, _m12, _m22);
        }

        public void SetScale(Vector2 scale)
        {
            _m00 = scale.x;
            _m10 = scale.y;
        }
        public void SetRotation(float zRotation)
        {
            float rads = zRotation * Maths.Deg2Rad;
            _m01 = Maths.Cos(rads);
            _m11 = Maths.Sin(rads);

            _m02 = -_m11;
            _m12 = _m01;

            _m22 = zRotation;
        }
        public void SetPosition(Vector2 point)
        {
            _m20 = point.x;
            _m21 = point.y;
        }

        public override string ToString() => $"M00: {_m00}\t M01: {_m01}\t M02: {_m02}\n M10: {_m10}\t M11: {_m11}\t M12: {_m12}\n M20: {_m20}\t M21: {_m21}\t M22: {_m22}";
    }
}
