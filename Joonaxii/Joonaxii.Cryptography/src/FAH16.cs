using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Joonaxii.Cryptography
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public unsafe struct FAH16 : IEquatable<FAH16>
    {
        private const ulong MAGIC_A = 0xFE_DC_BA_98_76_54_32_10;
        private const ulong MAGIC_B = ~MAGIC_A;

        public static FAH16 Empty { get; } = new FAH16(0, 0);

        private fixed ulong _hash[2];

        public FAH16(ulong a, ulong b)
        {
            _hash[0] = a;
            _hash[1] = b;
        }

        public FAH16(ulong* hash)
        {
            _hash[0] = *hash++;
            _hash[1] = *hash;
        }

        public override bool Equals(object obj) => obj is FAH16 fAH && Equals(fAH);

        public bool Equals(FAH16 other) =>
            _hash[0] == other._hash[0] & 
            _hash[1] == other._hash[1];

        public override int GetHashCode()
        {
            ulong hash = 421305410UL;
            hash *= _hash[0];
            hash *= _hash[1];
            return hash.GetHashCode();
        }

        public static bool operator ==(FAH16 left, FAH16 right) => left.Equals(right);
        public static bool operator !=(FAH16 left, FAH16 right) => !(left == right);

        public static byte[] Compute(byte[] data, int length)
        {
            fixed (byte* ptr = data) { return Compute(ptr, length); }
        }

        public static void Compute(byte[] data, int length, ulong* output)
        {
            fixed (byte* ptr = data) { Compute(ptr, length, output); }
        }

        public static byte[] Compute(byte* data, int length)
        {
            byte[] output = new byte[16];
            fixed (byte* ptr = output)
            {
                Compute(data, length, (ulong*)ptr);
            }
            return output;
        }

        public static void Compute(byte* data, int length, FAH16* output) => Compute(data, length, (ulong*)output);

        public static void Compute(byte* data, int length, ulong* outputA)
        {
            ulong* outputB = outputA + 1;

            int mode = 0;
            ulong value = 0;
            for (int i = 0; i < length;)
            {
                //Clear and fill stack with bytes from given data
                value = 0;

                int rem = (length - i);
                if(rem < 8)
                {
                    data += i;
                    i += rem;
                    int l = 0;

                    rem <<= 3;
                    while (l < rem)
                    {
                        value |= (ulong)*data++ << l;
                        l += 8;
                    }
                }
                else
                {
                    value = *(ulong*)(data + i);
                    i += 8;
                }
                OutputCompute(i, outputA, outputB, value, ref mode);
            }
        }

        private static void OutputCompute(int i, ulong* outputA, ulong* outputB, ulong value, ref int mode)
        {
            //Do some magic
            switch (mode)
            {
                case 0:
                    *outputA |= value ^ MAGIC_A;
                    *outputB |= value ^ MAGIC_B;
                    mode = 1;
                    break;
                case 1:
                    *outputA &= ~value;
                    *outputB ^= ((ulong)i << (int)((value & 0xFF_FF) >> 8));
                    mode = 2;
                    break;
                case 2:
                    *outputA ^= value;
                    *outputB &= ~((ulong)i >> (int)((value & 0xFF_FF) >> 8));
                    mode = 0;
                    break;
            }
        }

        public static void Compute(Stream data, FAH16* output) => Compute(data, (ulong*)output);


        public static void Compute(Stream data, ulong* outputA)
        {
            ulong* outputB = outputA + 1;
            const int HEAP_SIZE = 8;
            byte[] temp = new byte[HEAP_SIZE];

            fixed (byte* tmpPtr = temp)
            {
                int mode = 0;
                ulong value = 0;
                int length = (int)data.Length;

                while(data.Position < length)
                {
                    int len = data.Read(temp, 0, HEAP_SIZE);
                    for (int i = 0; i < len;)
                    {
                        //Clear and fill stack with bytes from given data
                        value = 0;

                        int rem = len - i;
                        if (rem < 8)
                        {
                            i += rem;
                            int l = 0;

                            rem <<= 3;
                            byte* ptr = tmpPtr + i;
                            while (l < rem)
                            {
                                value |= (ulong)*ptr++ << l;
                                l += 8;
                            }
                        }
                        else
                        {
                            value = *(ulong*)(tmpPtr + i);
                            i += 8;
                        }

                        OutputCompute(i, outputA, outputB, value, ref mode);
                    }
                }
            }
        }

        public Guid ToGuid()
        {
            fixed(ulong* ptr = _hash)
            {
                uint* ptrI = (uint*)ptr;
                ushort* ptrU = (ushort*)(ptrI + 1);
                byte* ptrB = (byte*)(ptrU + 2);
                return new Guid(*ptrI, *ptrU++, *ptrU, *ptrB++, *ptrB++, *ptrB++, *ptrB++, *ptrB++, *ptrB++, *ptrB++, *ptrB);
            }
        }

        public void ToGuid(Guid* guid)
        {
            ulong* ptr = (ulong*)guid;
            ptr[0] = _hash[0];
            ptr[1] = _hash[1];
        }
    }
}
