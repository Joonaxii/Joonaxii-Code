using System;
using System.Collections.Generic;

//Pretty much converted IArraySortHelper to work with pointers and unsafe/unmanaged Generics 
//Based on Microsoft's code from here https://github.com/microsoft/referencesource/blob/5697c29004a34d80acdaf5742d7e699022c64ecd/mscorlib/system/collections/generic/arraysorthelper.cs

namespace Joonaxii.Collections.Unmanaged
{
    internal unsafe interface IPointerSortHelper<T> where T : unmanaged
    {
        void Sort(T* values, int total, int index, int length, IComparer<T> comparer);
        int BinarySearch(T* values, int index, int length, T value, IComparer<T> comparer);
    }

    public static unsafe class UnmanagedArray
    {
        public static void Sort<T>(T* values, int total, int index, int length, IComparer<T> comparer) where T : unmanaged
        {
            if (comparer == null) { comparer = Comparer<T>.Default; }
            PointerSortHelper<T>.IntrospectiveSort(values, total, index, length, comparer);
        }
    }

    internal static class IntroSortUltis
    {
        internal const int IntrosortSizeThreshold = 16;
        internal const int QuickSortDepthThreshold = 32;

        internal static int FloorLog2(int n)
        {
            int result = 0;
            while (n >= 1)
            {
                result++;
                n >>= 1;
            }
            return result;
        }
    }

    internal unsafe class PointerSortHelper<T> : IPointerSortHelper<T> where T : unmanaged
    {      
        private static volatile PointerSortHelper<T> _defaultHelper;

        public static IPointerSortHelper<T> Default
        {
            get
            {
                IPointerSortHelper<T> help = _defaultHelper;
                if(help == null)
                {
                    help = CreatePointerSortHelper();
                }
                return help;
            }
        }

        internal sealed class FunctorComparer : IComparer<T>
        {
            private Comparison<T> _comparison;
            public FunctorComparer(Comparison<T> comparison)
            {
                _comparison = comparison;
            }

            public int Compare(T x, T y) => _comparison(x, y);
        }

        private static IPointerSortHelper<T> CreatePointerSortHelper()
        {
            //TODO: Make work with IComparable<T>
            //if (typeof(IComparable<T>).IsAssignableFrom(typeof(T)))
            //{
            //    _defaultHelper = new PointerSortHelper<T>();
            //    return _defaultHelper;
            //}

            _defaultHelper = new PointerSortHelper<T>();
            return _defaultHelper;
        }
    
        public void Sort(T* values, int total, int index, int length, IComparer<T> comparer)
        {
            if(comparer == null) { comparer = Comparer<T>.Default; }
            IntrospectiveSort(values, total, index, length, comparer);
        }

        public int BinarySearch(T* values, int index, int length, T value, IComparer<T> comparer)
        {
            if (comparer == null) { comparer = Comparer<T>.Default; }
            return InternalBinarySearch(values, index, length, value, comparer);
        }

        internal static int InternalBinarySearch(T* values, int index, int length, T value, IComparer<T> comparer)
        {
            int lo = index;
            int hi = index + length - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order = comparer.Compare(values[i], value);

                if (order == 0) { return i; }
                if (order < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }
            return ~lo;
        }

        internal static void IntrospectiveSort(T* values, int total, int left, int length, IComparer<T> comparer)
        {
            if(length < 2) { return; }
            IntroSort(values, left, length + left - 1, IntroSortUltis.FloorLog2(total) << 1, comparer);
        }

        private static void IntroSort(T* values, int lo, int hi, int depthLimit, IComparer<T> comparer)
        {
            while(hi > lo)
            {
                int partSize = hi - lo + 1;
                if(partSize <= IntroSortUltis.IntrosortSizeThreshold)
                {
                    switch (partSize)
                    {
                        case 1: return;
                        case 2:
                            SwapIfGreater(values, comparer, lo, hi);
                            return;
                        case 3:
                            SwapIfGreater(values, comparer, lo, hi - 1);
                            SwapIfGreater(values, comparer, lo, hi);
                            SwapIfGreater(values, comparer, hi - 1, hi);
                            return;
                    }
                    InsertionSort(values, lo, hi, comparer);
                    return;
                }

                if(depthLimit == 0)
                {
                    Heapsort(values, lo, hi, comparer);
                    return;
                }
                depthLimit--;

                int p = PickPivotAndPartition(values, lo, hi, comparer);
                IntroSort(values, p + 1, hi, depthLimit, comparer);
                hi = p - 1;
            }
        }

        private static int PickPivotAndPartition(T* values, int lo, int hi, IComparer<T> comparer)
        {
            int middle = lo + ((hi - lo) >> 1);

            SwapIfGreater(values, comparer, lo, middle);
            SwapIfGreater(values, comparer, lo, hi);
            SwapIfGreater(values, comparer, middle, hi);

            T pivot = values[middle];
            Swap(values, middle, hi - 1);
            int left = lo, right = hi - 1;

            while (left < right)
            {
                while (comparer.Compare(values[++left], pivot) < 0) ;
                while (comparer.Compare(pivot, values[--right]) < 0) ;

                if (left >= right) { break; }              
                Swap(values, left, right);
            }

            Swap(values, left, (hi - 1));
            return left;
        }

        private static void InsertionSort(T* keys, int lo, int hi, IComparer<T> comparer)
        {
            int i;
            int j;
            T t;
            for (i = lo; i < hi; i++)
            {
                j = i;
                t = keys[i + 1];
                while (j >= lo && comparer.Compare(t, keys[j]) < 0)
                {
                    keys[j + 1] = keys[j];
                    j--;
                }
                keys[j + 1] = t;
            }
        }

        private static void Heapsort(T* values, int lo, int hi, IComparer<T> comparer)
        {
            int n = hi - lo + 1;
            for (int i = n >> 1; i >= 1; i = i - 1)
            {
                DownHeap(values, i, n, lo, comparer);
            }
            for (int i = n; i > 1; i = i - 1)
            {
                Swap(values, lo, lo + i - 1);
                DownHeap(values, 1, i - 1, lo, comparer);
            }
        }

        private static void DownHeap(T* values, int i, int n, int lo, IComparer<T> comparer)
        {
            T d = values[lo + i - 1];
            int child;
            while (i <= n >> 1)
            {
                child = i << 1;
                if (child < n && comparer.Compare(values[lo + child - 1], values[lo + child]) < 0)
                {
                    child++;
                }

                if (!(comparer.Compare(d, values[lo + child - 1]) < 0)) { break; }
                   
                values[lo + i - 1] = values[lo + child - 1];
                i = child;
            }
            values[lo + i - 1] = d;
        }

        private static void SwapIfGreater(T* values, IComparer<T> comparer, int a, int b)
        {
            if (a != b)
            {
                T* aV = (values + a);
                T* bV = (values + b);
                if (comparer.Compare(*aV, *bV) > 0)
                {
                    T temp = *aV;
                    *aV = *bV;
                    *bV = temp;
                }
            }
        }

        private static void Swap(T* values, int a, int b)
        {
            if (a != b)
            {
                T temp = *(values + a);
                *(values + a) = *(values + b);
                *(values + b) = temp;
            }
        }
    }
}