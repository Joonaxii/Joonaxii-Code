
using System;

namespace Joonaxii.MathJX
{
    public unsafe class BufferUtils
    {
        #region Memset
        public static void Memset(sbyte[] data, sbyte val) => Memset(data, val, 0, data.Length);
        public static void Memset(sbyte[] data, sbyte val, int start, int size)
        {
            fixed (sbyte* buf = data) { Memset(buf, val, start, size); }
        }
        public static void Memset(sbyte* buffer, sbyte val, int start, int size)
        {
            for (int i = start; i < start + size; i++)
            {
                buffer[i] = val;
            }
        }

        public static void Memset(byte[] data, byte val) => Memset(data, val, 0, data.Length);
        public static void Memset(byte[] data, byte val, int start, int size)
        {
            fixed (byte* buf = data) { Memset(buf, val, start, size); }
        }
        public static void Memset(byte* buffer, byte val, int start, int size)
        {
            for (int i = start; i < start + size; i++)
            {
                buffer[i] = val;
            }
        }

        public static void Memset(short[] data, short val) => Memset(data, val, 0, data.Length);
        public static void Memset(short[] data, short val, int start, int size)
        {
            fixed (short* buf = data) { Memset(buf, val, start, size); }
        }
        public static void Memset(short* buffer, short val, int start, int size)
        {
            for (int i = start; i < start + size; i++)
            {
                buffer[i] = val;
            }
        }

        public static void Memset(ushort[] data, ushort val) => Memset(data, val, 0, data.Length);
        public static void Memset(ushort[] data, ushort val, int start, int size)
        {
            fixed (ushort* buf = data) { Memset(buf, val, start, size); }
        }
        public static void Memset(ushort* buffer, ushort val, int start, int size)
        {
            for (int i = start; i < start + size; i++)
            {
                buffer[i] = val;
            }
        }

        public static void Memset(char[] data, char val) => Memset(data, val, 0, data.Length);
        public static void Memset(char[] data, char val, int start, int size)
        {
            fixed (char* buf = data) { Memset(buf, val, start, size); }
        }
        public static void Memset(char* buffer, char val, int start, int size)
        {
            for (int i = start; i < start + size; i++)
            {
                buffer[i] = val;
            }
        }

        public static void Memset(int[] data, int val) => Memset(data, val, 0, data.Length);
        public static void Memset(int[] data, int val, int start, int size)
        {
            fixed (int* buf = data) { Memset(buf, val, start, size); }
        }
        public static void Memset(int* buffer, int val, int start, int size)
        {
            for (int i = start; i < start + size; i++)
            {
                buffer[i] = val;
            }
        }

        public static void Memset(uint[] data, uint val) => Memset(data, val, 0, data.Length);
        public static void Memset(uint[] data, uint val, int start, int size)
        {
            fixed (uint* buf = data) { Memset(buf, val, start, size); }
        }
        public static void Memset(uint* buffer, uint val, int start, int size)
        {
            for (int i = start; i < start + size; i++)
            {
                buffer[i] = val;
            }
        }

        public static void Memset(long[] data, long val) => Memset(data, val, 0, data.Length);
        public static void Memset(long[] data, long val, int start, int size)
        {
            fixed (long* buf = data) { Memset(buf, val, start, size); }
        }
        public static void Memset(long* buffer, long val, int start, int size)
        {
            for (int i = start; i < start + size; i++)
            {
                buffer[i] = val;
            }
        }

        public static void Memset(byte[] data, long val, byte byteCount) => Memset(data, val, byteCount, 0, data.Length);
        public static void Memset(byte[] data, long val, byte byteCount, int start, int size)
        {
            fixed (byte* buf = data) { Memset(buf, val, byteCount, start, size); }
        }
        public static void Memset(byte* buffer, long val, byte byteCount, int start, int size)
        {
            buffer += start;

            byte* v;
            while (size > 0)
            {
                v = (byte*)&val;
                for (int i = 0; i < byteCount; i++)
                {
                    *buffer++ = *v++;
                }
                size -= byteCount;
            }
        }

        public static void Memset<T>(T[] data, T val, byte byteCount) where T : unmanaged => Memset(data, val, byteCount, 0, data.Length);
        public static void Memset<T>(T[] data, T val, byte byteCount, int start, int size) where T : unmanaged
        {
            fixed (T* buf = data) { Memset(buf, val, byteCount, start, size); }
        }
        public static void Memset<T>(T* buffer, T val, byte byteCount, int start, int size) where T : unmanaged
        {
            buffer += start;

            while(size-- > 0)
            {
                *buffer++ = val;
            }
        }

        public static void Memset(ulong[] data, ulong val) => Memset(data, val, 0, data.Length);
        public static void Memset(ulong[] data, ulong val, int start, int size)
        {
            fixed (ulong* buf = data) { Memset(buf, val, start, size); }
        }
        public static void Memset(ulong* buffer, ulong val, int start, int size)
        {
            for (int i = start; i < start + size; i++)
            {
                buffer[i] = val;
            }
        }


        public static void Memset(float[] data, float val) => Memset(data, val, 0, data.Length);
        public static void Memset(float[] data, float val, int start, int size)
        {
            fixed (float* buf = data) { Memset(buf, val, start, size); }
        }
        public static void Memset(float* buffer, float val, int start, int size)
        {
            for (int i = start; i < start + size; i++)
            {
                buffer[i] = val;
            }
        }

        public static void Memset(double[] data, double val) => Memset(data, val, 0, data.Length);
        public static void Memset(double[] data, double val, int start, int size)
        {
            fixed (double* buf = data) { Memset(buf, val, start, size); }
        }
        public static void Memset(double* buffer, double val, int start, int size)
        {
            for (int i = start; i < start + size; i++)
            {
                buffer[i] = val;
            }
        }
        #endregion

        public static void Memcpy(byte[] dest, byte[] src) => Memcpy(dest, 0, src, 0, src.Length);
        public static void Memcpy(byte[] dest, int startDest, byte[] src, int startSrc, int length)
        {
            fixed (byte* buf = dest)
            {
                fixed (byte* bufB = src)
                {
                    Memcpy(buf, startDest, bufB, startSrc, length);
                }
            }
        }

        public static void Memcpy(byte* dest, byte[] src)
        {
            fixed (byte* bufB = src)
            {
                Memcpy(dest, bufB, src.Length);
            }
        }
        public static void Memcpy(byte* dest, int startDest, byte[] src, int startSrc, int length)
        {
            fixed (byte* bufB = src)
            {
                Memcpy(dest, startDest, bufB, startSrc, length);
            }
        }

        public static void Memcpy(byte* dest, byte* src, int length)
        {
            while (length-- > 0)
            {
                *dest++ = (*src++);
            }
        }

        public static void Memcpy(byte* dest, int startDest, byte* src, int startSrc, int length)
        {
            dest += startDest;
            src += startSrc;

            while (length-- > 0)
            {
                *dest++ = *src++;
            }
        }
    }
}
