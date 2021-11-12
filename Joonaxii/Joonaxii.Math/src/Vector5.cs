using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.Math
{
    [StructLayout(LayoutKind.Sequential, Size = 20, Pack = 4)]
    public struct Vector5
    {
        public static Vector5 zero { get; } = new Vector5(0, 0, 0, 0, 0);

        public float x;
        public float y;
        public float z;
        public float w;
        public float i;

        public Vector5(float x, float y, float z, float w, float i)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
            this.i = i;
        }
    }
}
