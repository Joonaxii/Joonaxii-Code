using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Joonaxii.Collections
{
    public abstract class PrimitiveListBase<T> : IList<T> where T : IEquatable<T>
    {
        public int Capacity { get => _capacity; }

        public int Count => _count;
        public bool IsReadOnly => false;

        public T this[int index] { get => _items[index]; set => _items[index] = value; }

        protected int _count;
        protected int _capacity;
        protected T[] _items;

        private int _size;

        internal PrimitiveListBase() : this(128) { }
        internal PrimitiveListBase(int capacity)
        {
            _size = Marshal.SizeOf<T>();
            _capacity = capacity;
            _items = new T[_capacity];
            _count = 0;
        }

        public int IndexOf(T item)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_items[i].Equals(item)) { return i; }
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
            if (index < 0 | index >= _count) { return; }
            if(index == _count) { Add(item); return; }
            ValidateBuffer(_count);

            int len = _count - index;
            Buffer.BlockCopy(_items, index * _size, _items, (index + 1) * _size, len * _size);
            _items[index] = item;
            _count++;
        }

        public void RemoveAt(int index)
        {
            if(index < 0 | index >= _count) { return; }
            if(index == _count - 1)
            {
                _count--;
                return;
            }

            int len = _count - index - 1;
            Buffer.BlockCopy(_items, (index + 1) * _size, _items, index * _size, len * _size);
            _count--;
        }

        public void Add(T item)
        {
            ValidateBuffer(_count);
            _items[_count++] = item;
        }

        public void Clear()
        {
            _count = 0;
        }

        public void RemoveRange(int start, int length)
        {
            int startOfDataToPres = length + start;
            Buffer.BlockCopy(_items, startOfDataToPres, _items, start, length);
            _count -= length;
        }

        public void AddRange(T[] data) => AddRange(data, 0, data.Length);
        public void AddRange(T[] data, int startSrc, int length)
        {
            int start = _count;
            _count += length;
            ValidateBuffer(_count);
            Buffer.BlockCopy(data, startSrc, _items, start, length);
        }

        public bool Contains(T item) => IndexOf(item) > -1;
        public void CopyTo(T[] array, int arrayIndex)
        {
            int len = Math.Min(array.Length - arrayIndex, _count);
            for (int i = 0; i < len; i++)
            {
                array[i + arrayIndex] = _items[i];
            }
        }

        public bool Remove(T item)
        {
            int id = IndexOf(item);
            if(id < 0) { return false; }
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

        private void ValidateBuffer(int count)
        {
            if(count < _capacity) { return; }
            while(count >= _capacity) 
            {
                _capacity <<= 1;
            }
           
            Array.Resize(ref _items, _capacity);
        }
    }
}
