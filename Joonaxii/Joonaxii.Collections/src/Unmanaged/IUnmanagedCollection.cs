using System;
using System.Collections;
using System.Collections.Generic;

namespace Joonaxii.Collections
{
    public unsafe interface IUnmanagedCollection<T> : IList<T>, IEnumerable<T>, IEnumerable where T : unmanaged
    {
        bool IsFreed { get; }

        int Capacity { get; }
        int Length { get; }

        T* RawPointer { get; }
        IntPtr Pointer { get; }

        void CopyTo(T[] array, int start, int length);
        void CopyTo(T* array, int start, int length);
        void CopyTo(T* array, int length);

        void CopyTo(IUnmanagedCollection<T> collection, int start, int length);
        void CopyTo(IUnmanagedCollection<T> collection, int length);

        void ReAllocate(int count);
        void Free();

        void Sort();
        void Sort(Comparison<T> comparison);
        void Sort(int start, int length, Comparison<T> comparison);
        void Sort(IComparer<T> comparer);

        void Sort(int index, int count);
        void Sort(int index, int count, IComparer<T> comparer);

    }
}