using Joonaxii.MathJX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Joonaxii.Collections.Unmanaged
{
    public unsafe class UnmanagedList<T> : IUnmanagedCollection<T> where T : unmanaged
    {
        public bool IsFreed => _items == null;
        public int Capacity => _capacity;
        private int _capacity;

        int IUnmanagedCollection<T>.Length { get => _count; }

        public IntPtr Pointer => _safePtr;
        public T* RawPointer => _items;

        public bool IsReadOnly => false;

        public int Count => _count;

        /// <summary>
        /// Unsafe indexer, doesn't check if index is in rage.
        /// </summary>
        public T this[int i]
        {
            get
            {
                if (_items == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
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
        private int _count;

        private IEqualityComparer<T> _comparer;

        public UnmanagedList() : this(EqualityComparer<T>.Default) { }

        public UnmanagedList(int length) : this(EqualityComparer<T>.Default, length) { }

        public UnmanagedList(UnmanagedList<T> collection) : this(EqualityComparer<T>.Default, collection) { }
        public UnmanagedList(IEqualityComparer<T> comparer, UnmanagedList<T> collection)
        {
            if (collection.RawPointer == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }

            _comparer = comparer;
            ReAllocate(collection._capacity);
            _count = collection._count;

            var ptrA = _items;
            var ptrB = collection.RawPointer;
            int i = _count;

            while (i-- > 0)
            {
                *ptrA++ = *ptrB++;
            }
        }

        public UnmanagedList(IUnmanagedCollection<T> collection) : this(EqualityComparer<T>.Default, collection) { }
        public UnmanagedList(IEqualityComparer<T> comparer, IUnmanagedCollection<T> collection)
        {
            if (collection.RawPointer == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }

            _comparer = comparer;
            ReAllocate(collection.Capacity);
            _count = collection.Count;

            var ptrA = _items;
            var ptrB = collection.RawPointer;
            int i = _count;

            while (i-- > 0)
            {
                *ptrA++ = *ptrB++;
            }
        }

        public UnmanagedList(IList<T> collection) : this(EqualityComparer<T>.Default, collection) { }
        public UnmanagedList(IEqualityComparer<T> comparer, IList<T> collection)
        {
            _comparer = comparer;
            ReAllocate(collection.Count);
            _count = collection.Count;

            var ptr = _items;
            int i = -1;

            while (++i < _count)
            {
                *ptr++ = collection[i];
            }
        }

        public UnmanagedList(IEqualityComparer<T> comparer, int length)
        {
            _comparer = comparer;
            ReAllocate(length);
        }

        public UnmanagedList(IEqualityComparer<T> comparer)
        {
            _comparer = comparer;
        }

        ~UnmanagedList() => Free();

        public void CopyTo(T[] array, int start)
        {
            fixed (T* ptr = array) { CopyTo(ptr + start, array.Length); }
        }
        public void CopyTo(T[] array, int start, int length)
        {
            fixed (T* ptr = array) { CopyTo(ptr + start, length); }
        }

        public void CopyTo(T* array, int start, int length) => CopyTo(array + start, length);
        public void CopyTo(T* array, int length)
        {
            if (_items == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
            var ptr = _items;

            while (length-- > 0)
            {
                *array++ = *ptr++;
            }
        }

        public void CopyTo(IUnmanagedCollection<T> collection, int start, int length)
        {
            if (collection.RawPointer == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
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
            while (++i < _count)
            {
                if (_comparer.Equals(*ptr++, value)) { return i; }
            }
            return -1;
        }
        public bool Contains(T value) => IndexOf(value) > -1;

        private void Validate(int size)
        {
            if(_capacity > size) { return; }

            int cap = _capacity;
            while(cap < size)
            {
                cap <<= 1;
            }
            ReAllocate(cap);
        }

        public void ReAllocate(int count)
        {
            _capacity = count;
            count *= sizeof(T);
            if (_items != null)
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
            if (_items == null) { return; }

            _capacity = 0;
            _count = 0;
            Marshal.FreeHGlobal(_safePtr);
            _items = null;
            _safePtr = IntPtr.Zero;
        }

        public IEnumerator<T> GetEnumerator() => new UnamangedCollectionEnumerator<T>(this);
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public void Insert(int index, T item)
        {
            if (index < 0 | index >= _capacity) { throw new IndexOutOfRangeException("Index outside of the range of the underlying pointer!"); }
            if (_items == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }

            if(index >= _count) 
            {
                *(_items + index) = item;
                _count = index + 1;
                return; 
            }

            if (index == _count - 1)
            {
                Add(item);
                *(_items + index) = item;
                _count++;
                return;
            }

            int len = _count - index;
            BufferUtils.Memshft(_items, 1, len);
            *(_items + index) = item;
        }

        public void RemoveAt(int index)
        {
            if(index < 0 | index >= _capacity) { throw new IndexOutOfRangeException("Index outside of the range of the underlying pointer!"); } 
            if (_items == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }

            var to = _items + index;
            int len = _count-- - index - 1;
            BufferUtils.Memcpy(to + 1, to, len);
        }

        public void Add(T item)
        {
            if (_items == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
            Validate(_count);
            *(_items + _count++) = item;
        }

        public void Clear()  => _count = 0;
        public void ClearFull()
        {
            var ptr = _items;       
            while(_count-- > 0)
            {
                *ptr++ = default(T);
            }
            _count = 0;
        }

        public bool Remove(T item)
        {
            if (_items == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
            int index = IndexOf(item);
            if(index < 0) { return false; }

            if(index == _count - 1)
            {
                _count--;
                return true;
            }
            RemoveAt(index);
            return true;
        }

        public void Sort() => Sort(Comparer<T>.Default);
        public void Sort(IComparer<T> comparer)
        {
            if (_items == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
            PointerSortHelper<T>.Default.Sort(_items, _count, 0, _count, comparer);
        }

        public void Sort(int index, int count) => Sort(index, count, Comparer<T>.Default);
        public void Sort(int index, int count, IComparer<T> comparer)
        {
            if (_items == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
            PointerSortHelper<T>.Default.Sort(_items, _count, index, count, comparer);
        }

        public void Sort(Comparison<T> comparison)
        {
            if (_items == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
            IComparer<T> comparer = new PointerSortHelper<T>.FunctorComparer(comparison);
            PointerSortHelper<T>.Default.Sort(_items, _count, 0, _count, comparer);
        }

        public void Sort(int start, int length, Comparison<T> comparison)
        {
            if (_items == null) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
            IComparer<T> comparer = new PointerSortHelper<T>.FunctorComparer(comparison);
            PointerSortHelper<T>.Default.Sort(_items, _count, start, length, comparer);
        }
    }
}
