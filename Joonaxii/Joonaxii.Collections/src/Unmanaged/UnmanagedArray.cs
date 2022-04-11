using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Joonaxii.Collections.Unmanaged
{
    public unsafe class UnmanagedArray<T> : IUnmanagedCollection<T> where T : unmanaged
    {
        int IUnmanagedCollection<T>.Capacity => _length;

        public bool IsFreed => _items == null;

        public int Length => _length;
        public IntPtr Pointer => _safePtr;
        public T* RawPointer => _items;

        public bool IsReadOnly => false;

        int ICollection<T>.Count => _length;

        /// <summary>
        /// Unsafe indexer, doesn't check if index is in rage.
        /// </summary>
        public T this[int i]
        {
            get
            {
                if(_items == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
                return *(_items + i);
            }
            set
            {
                if (_items == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
                *(_items + i) = value;
            }
        }

        private T* _items;
        private IntPtr _safePtr;
        private int _length;
        private bool _allowReAlloc = true;

        private IEqualityComparer<T> _comparer;

        public UnmanagedArray() : this(EqualityComparer<T>.Default) { }

        public UnmanagedArray(int length) : this(EqualityComparer<T>.Default, length) { }

        public UnmanagedArray(T* ptr, int length) : this(ptr, length, EqualityComparer<T>.Default) { }
        public UnmanagedArray(T* ptr, int length, IEqualityComparer<T> comparer)
        {
            _allowReAlloc = false;
            _comparer = comparer;

            _length = length;
            _items = ptr;
            _safePtr = new IntPtr(_items);
        }

        public UnmanagedArray(IUnmanagedCollection<T> collection) : this(EqualityComparer<T>.Default, collection) { }
        public UnmanagedArray(IEqualityComparer<T> comparer, IUnmanagedCollection<T> collection)
        {
            if(collection.RawPointer == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }

            _comparer = comparer;
            ReAllocate(collection.Length);

            var ptrA = _items;
            var ptrB = collection.RawPointer;
            int i = _length;

            while(i-- > 0)
            {
                *ptrA++ = *ptrB++;
            }
        }

        public UnmanagedArray(IList<T> collection) : this(EqualityComparer<T>.Default, collection) { }
        public UnmanagedArray(IEqualityComparer<T> comparer, IList<T> collection)
        {
            _comparer = comparer;
            ReAllocate(collection.Count);

            var ptr = _items;
            int i = -1;

            while (++i < _length)
            {
                *ptr++ = collection[i];
            }
        }

        public UnmanagedArray(IEqualityComparer<T> comparer, int length)
        {
            _comparer = comparer;
            ReAllocate(length);
        }

        public UnmanagedArray(IEqualityComparer<T> comparer)
        {
            _comparer = comparer;
        }

        ~UnmanagedArray() => Free();

        public void CopyTo(T[] array, int start)
        {
            fixed(T* ptr = array) { CopyTo(ptr + start, array.Length); }
        }
        public void CopyTo(T[] array, int start, int length)
        {
            fixed (T* ptr = array) { CopyTo(ptr + start, length); }
        }

        public void CopyTo(T* array, int start, int length) => CopyTo(array + start, length);
        public void CopyTo(T* array, int length)
        {
            if(_items == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
            var ptr = _items;

            while(length-- > 0)
            {
                *array++ = *ptr++;
            }
        }

        public void CopyTo(IUnmanagedCollection<T> collection, int start, int length)
        {
            if(collection.RawPointer == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
            CopyTo(collection.RawPointer + start, length);
        }
        public void CopyTo(IUnmanagedCollection<T> collection, int length)
        {
            if (collection.RawPointer == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
            CopyTo(collection.RawPointer, length);
        }

        public int IndexOf(T value)
        {
            if (_items == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }

            int i = -1;
            var ptr = _items;
            while (++i < _length)
            {
                 if(_comparer.Equals(*ptr++, value)) { return i; }
            }
            return -1;
        }
        public bool Contains(T value) => IndexOf(value) > -1;

        public void ReAllocate(int count)
        {
            if (!_allowReAlloc) { return; }

            _length = count;
            count *= sizeof(T);
            if(_items != null)
            {
                _safePtr = Marshal.ReAllocHGlobal(new IntPtr(_items), new IntPtr(count));
                _items = (T*)_safePtr;
                return;
            }
            _safePtr = Marshal.AllocHGlobal(count);
            _items = (T*)_safePtr;
        }

        public void Free()
        {
            if(_items == null | !_allowReAlloc) { return; }
            _length = 0;
            Marshal.FreeHGlobal(_safePtr);
            _items = null;
            _safePtr = IntPtr.Zero;
        }

        public IEnumerator<T> GetEnumerator() => new UnamangedCollectionEnumerator<T>(this);
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        void IList<T>.Insert(int index, T item) => throw new NotSupportedException("Inserting is not supported for fixed collections!");
        void IList<T>.RemoveAt(int index) => throw new NotSupportedException("Removing is not supported for fixed collections!");
        void ICollection<T>.Add(T item) => throw new NotSupportedException("Adding is not supported for fixed collections!");
        void ICollection<T>.Clear() => throw new NotSupportedException("Clearing is not supported for fixed collections!");
        bool ICollection<T>.Remove(T item) => throw new NotSupportedException("Removing is not supported for fixed collections!");

        public void Sort() => Sort(Comparer<T>.Default);
        public void Sort(IComparer<T> comparer)
        {
            if (_items == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
            PointerSortHelper<T>.Default.Sort(_items, _length, 0, _length, comparer);
        }

        public void Sort(int index, int count) => Sort(index, count, Comparer<T>.Default);
        public void Sort(int index, int count, IComparer<T> comparer)
        {
            if (_items == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
            PointerSortHelper<T>.Default.Sort(_items, _length, index, count, comparer);
        }

        public void Sort(Comparison<T> comparison)
        {
            if (_items == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
            IComparer<T> comparer = new PointerSortHelper<T>.FunctorComparer(comparison);
            PointerSortHelper<T>.Default.Sort(_items, _length, 0, _length, comparer);
        }

        public void Sort(int start, int length, Comparison<T> comparison)
        {
            if (_items == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
            IComparer<T> comparer = new PointerSortHelper<T>.FunctorComparer(comparison);
            PointerSortHelper<T>.Default.Sort(_items, _length, start, length, comparer);
        }
    }
}
