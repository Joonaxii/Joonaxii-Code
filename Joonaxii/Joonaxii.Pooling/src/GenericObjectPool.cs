using System;
using System.Collections.Generic;

namespace Joonaxii.Pooling
{
    public class GenericObjectPool<T>
    {
        protected Func<T> _createNew;
        protected Queue<T> _pool;

        public GenericObjectPool(int initCount, Func<T> createNew)
        {
            _createNew = createNew;
            _pool = new Queue<T>(initCount * 2);
            for (int i = 0; i < initCount; i++)
            {
                Return(GetNew());
            }
        }

        public GenericObjectPool(Func<T> createNew) : this(32, createNew) { }

        protected GenericObjectPool() { }

        public virtual T GetNew() => _createNew.Invoke();
        public virtual T Get() => _pool.Count > 0 ? _pool.Dequeue() : GetNew();
        public virtual void Return(T input) => _pool.Enqueue(input);
    }
}