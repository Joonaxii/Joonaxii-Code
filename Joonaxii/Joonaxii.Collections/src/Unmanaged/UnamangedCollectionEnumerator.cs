using System;
using System.Collections;
using System.Collections.Generic;

namespace Joonaxii.Collections.Unmanaged
{
    internal unsafe class UnamangedCollectionEnumerator<T> : IEnumerator<T>  where T : unmanaged
    {
        private IUnmanagedCollection<T> _collection;
        private int _pos;

        public UnamangedCollectionEnumerator(IUnmanagedCollection<T> collection)
        {
            _collection = collection;
        }

        public T Current
        {
            get
            {
                if (_collection.IsFreed) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
                return *(_collection.RawPointer + _pos);
            }

            set
            {
                if (_collection.IsFreed) { throw new NullReferenceException("Underlying pointer has been Freed!"); }
                *(_collection.RawPointer + _pos) = value;
            }
        }

        object IEnumerator.Current => Current;

        public bool MoveNext() => ++_pos < _collection.Count;
        public void Reset() => _pos = 0;

        public void Dispose() { }
    }
}