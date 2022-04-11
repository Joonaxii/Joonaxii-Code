
using System;
using System.Runtime.InteropServices;

namespace Joonaxii.MathJX
{
    public unsafe class BufferUtils
    {
        #region Memset
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

        public static void Memset<T>(T[] data, T val) where T : unmanaged => Memset(data, val, 0, data.Length);
        public static void Memset<T>(T[] data, T val, int start, int length) where T : unmanaged
        {
            fixed (T* buf = data) { Memset(buf + start, val, length); }
        }
        public static void Memset<T>(T* buffer, T val, int start, int length) where T : unmanaged => Memset(buffer + start, val, length);
        public static void Memset<T>(T* buffer, T val, int length) where T : unmanaged
        {
            while (length-- > 0)
            {
                *buffer++ = val;
            }
        }

        public static void Memset<T>(T* buffer, int start, T valA, int lengthA, T valB, int lengthB) where T : unmanaged => Memset(buffer + start, valA, lengthA, valB, lengthB);
        public static void Memset<T>(T* buffer, T valA, int lengthA, T valB, int lengthB) where T : unmanaged
        {
            while (lengthA-- > 0)
            {
                *buffer++ = valA;
            }

            while (lengthB-- > 0)
            {
                *buffer++ = valB;
            }
        }
        #endregion

        public static void Memcpy(IntPtr src, int srcOffset, IntPtr dst, int dstOffset, int length)=> Memcpy(src + srcOffset, dst + dstOffset, length);
        public static void Memcpy(IntPtr src, IntPtr dst, int length)
        {
            unsafe
            {
                byte* srcPtr = (byte*)src;
                byte* dstPtr = (byte*)dst;

                while (length-- > 0)
                {
                    *srcPtr++ = *dstPtr++;
                }
            }
        }

        public static void Memcpy<T>(T[] src, T[] dst, int length) where T : unmanaged => Memcpy(src, 0, dst, 0, length);
        public static void Memcpy<T>(T[] src, int srcOffset, T[] dst, int dstOffset, int length) where T : unmanaged
        {
            unsafe
            {
                fixed(T* srcPtr = src)
                fixed(T* dstPtr = dst)
                {
                    Memcpy(srcPtr + srcOffset, dstPtr + dstOffset, length);
                }
            }
        }

        public static void Memcpy<T>(T* src, int srcOffset, T* dst, int dstOffset, int length) where T : unmanaged => Memcpy(src + srcOffset, dst + dstOffset, length);
        public static void Memcpy<T>(T* src, T* dst, int length) where T : unmanaged
        {
            while(length-- > 0)
            {
                *dst++ = *src++;
            }
        }

        public static void Memshft<T>(T* src, int shift, int srcOffset, int length) where T : unmanaged => Memshft(src + srcOffset, shift, length);
        public static void Memshft<T>(T* src, int shift, int length) where T : unmanaged
        {
            //If shift is 0, don't do anything
            if(shift == 0) { return; }
            
            //If less than 0, perform a simple Memcpy
            if(shift < 0)
            {
                Memcpy(src, src + shift, length);
                return;
            }

            //Else offset the src pointer by the length and create a dst pointer
            //from said src pointer offset by the shift value
            //Then perform a Memcpy in reverse
            src += length;
            T* dst = src + shift;
            while (length-- > 0)
            {
                *dst-- = *src--;
            }
        }

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
