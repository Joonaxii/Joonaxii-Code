using System;
using System.Collections.Generic;

namespace Joonaxii.Text.Compression
{
    public class ObjectPool<T>
    {
        private Func<T> _createNew;
        private Queue<T> _pool;

        public ObjectPool(int initCount, Func<T> createNew)
        {
            _createNew = createNew;
            _pool = new Queue<T>(initCount * 2);
            for (int i = 0; i < initCount; i++)
            {
                Return(GetNew());
            }
        }

        public T GetNew() => _createNew.Invoke();
        public T Get() => _pool.Count > 0 ? _pool.Dequeue() : GetNew();

        public void Return(T input) => _pool.Enqueue(input);
    }
}