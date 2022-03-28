using Joonaxii.MathJX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Joonaxii.Collections
{
    public class PinnableList<T> : IList<T> where T : unmanaged, IEquatable<T>
    {
        public int Capacity { get => _capacityPad; }

        public virtual int Count => _count;
        public bool IsReadOnly => false;

        public bool IsPinned { get => _handle.IsAllocated; }
        protected virtual int Padding { get => 0; }

        public T this[int index] { get => _items[index]; set => _items[index] = value; }
        public unsafe T* RawPointer { get => _pinned; }
        public IntPtr Pointer { get => _pinnedInt; }

        protected int _count;
        protected int _capacity;
        protected int _capacityPad;
        protected T[] _items;

        private GCHandle _handle;
        private unsafe T* _pinned = null;
        private IntPtr _pinnedInt = IntPtr.Zero;

        public PinnableList() : this(128) { }
        public PinnableList(int capacity)
        {
            _capacity = capacity;
            _capacityPad = capacity + Padding;
            _items = new T[_capacityPad];
            _count = 0;
        }

        ~PinnableList()
        {
            UnPin();
        }

        public T[] GetItems() => _items;

        public IntPtr Pin()
        {
            unsafe
            {
                if (IsPinned) { return _pinnedInt; }

                _handle = GCHandle.Alloc(_items, GCHandleType.Pinned);
                _pinnedInt = _handle.AddrOfPinnedObject();

                _pinned = (T*)_handle.AddrOfPinnedObject();

                _handle.Free();
                return _pinnedInt;
            }
        }

        public void UnPin()
        {
            unsafe
            {
                if (_handle.IsAllocated)
                {
                    _pinned = null;
                    _pinnedInt = IntPtr.Zero;
                    _handle.Free();
                }
            }
        }

        public int IndexOf(T item)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_items[i].Equals(item)) { return i; }
            }
            return -1;
        }

        public virtual void Insert(int index, T item)
        {
            if (index < 0 | index >= _count) { return; }
            if (index == _count) { Add(item); return; }
            ValidateBuffer(_count, true);

            int len = _count - index;
            unsafe
            {
                T* ptrI;
                if (IsPinned)
                {
                    ptrI = _pinned + index;
                    BufferUtils.Memshft(ptrI, 1, len);
                    *ptrI = item;
                    _count++;
                    return;
                }

                fixed (T* ptr = _items)
                {
                    ptrI = ptr + index;
                    BufferUtils.Memshft(ptrI, 1, len);
                    *ptrI = item;
                    _count++;
                }
            }
        }

        public virtual void RemoveAt(int index)
        {
            if (index < 0 | index >= _count) { return; }
            if (index == _count - 1)
            {
                _count--;
                return;
            }

            int len = _count - index - 1;
            unsafe
            {
                T* ptrA;
                T* ptrB;
                if (IsPinned)
                {
                    ptrA = _pinned + index;
                    ptrB = ptrA + 1;
                    BufferUtils.Memcpy(ptrA, ptrB, len);

                    _count--;
                    return;
                }

                fixed (T* ptr = _items)
                {
                    ptrA = ptr + index;
                    ptrB = ptrA + 1;
                    BufferUtils.Memcpy(ptrA, ptrB, len);
                    _count--;
                }
            }
        }

        public virtual void Add(T item)
        {
            ValidateBuffer(_count, true);
            _items[_count++] = item;
        }

        public void Trim(bool keepPinned)
        {
            if (_capacity > _count)
            {
                bool wasPinned = IsPinned;

                UnPin();
                _capacity = _count;
                _capacityPad = _capacity + Padding;
                Array.Resize(ref _items, _capacityPad);

                if (wasPinned & keepPinned)
                {
                    Pin();
                }
            }
        }

        public void Clear() => Clear(false);

        public void Clear(bool keepPinned)
        {
            _count = 0;
            if (keepPinned) { return; }
            UnPin();
        }

        public virtual void RemoveRange(int start, int length)
        {
            int shiftI = start + length;
            unsafe
            {
                if (IsPinned)
                {
                    BufferUtils.Memshft(_pinned + shiftI, -length, _count - shiftI);
                }
                else
                {
                    fixed(T* ptr = _items)
                    {
                        BufferUtils.Memshft(ptr + shiftI, -length, _count - shiftI);
                    }
                }
            }
            _count -= length;
        }

        public virtual void AddRange(T[] data) => AddRange(data, 0, data.Length);
        public virtual void AddRange(T[] data, int startSrc, int length)
        {
            int start = _count;
            _count += length;
            ValidateBuffer(_count, true);

            unsafe
            {
                T* dst;
                fixed (T* src = data)
                {
                    if (IsPinned)
                    {
                        dst = _pinned + start;
                        BufferUtils.Memcpy(dst, src + startSrc, length);
                        return;
                    }

                    fixed (T* itm = _items)
                    {
                        dst = itm + start;
                        BufferUtils.Memcpy(dst, src + startSrc, length);
                    }
                }
            }
        }

        public bool Contains(T item) => IndexOf(item) > -1;
        public void CopyTo(T[] array, int arrayIndex)
        {
            int len = Math.Min(array.Length - arrayIndex, _count);
            unsafe
            {
                fixed (T* dst = array)
                {
                    if (IsPinned)
                    {
                        BufferUtils.Memcpy(_pinned, dst + arrayIndex, len);
                        return;
                    }

                    fixed (T* src = _items)
                    {
                        BufferUtils.Memcpy(src, dst + arrayIndex, len);
                    }
                }
            }
        }

        public virtual bool Remove(T item)
        {
            int id = IndexOf(item);
            if (id < 0) { return false; }
            RemoveAt(id);
            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
            {
                yield return _items[i];
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        protected void ValidateBuffer(int count, bool keepPinned)
        {
            if (count < _capacity) { return; }
            bool wasPinned = IsPinned;
            UnPin();

            while (count >= _capacity)
            {
                _capacity <<= 1;
            }
            _capacityPad = _capacity + Padding;
            Array.Resize(ref _items, _capacityPad);

            if (wasPinned & keepPinned)
            {
                Pin();
            }
        }
    }
}
