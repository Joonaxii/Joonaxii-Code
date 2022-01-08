using Joonaxii.Collections.Queue;
using System.Collections.Generic;
using System;

namespace Joonaxii.Pooling
{
    public class FixedObjectPool : GenericObjectPool<IFixedPoolable> 
    {
        private ListQueue<IFixedPoolable> _activePool;
        public FixedObjectPool(int poolSize, Func<IFixedPoolable> createNew) : base()
        {
            _createNew = createNew;
            _pool = new Queue<IFixedPoolable>(poolSize);
            _activePool = new ListQueue<IFixedPoolable>(poolSize);
        }

        public override IFixedPoolable Get()
        {
            IFixedPoolable top;
            if (_pool.Count < 1)
            {
                top = _activePool.Dequeue();
                top.OnPoolableReused();
                _activePool.Enqueue(top);
                return top;
            }

            top = _pool.Dequeue();
            _activePool.Enqueue(top);
            return top;
        }

        public T Get<T>() where T : IFixedPoolable => (T)Get();

        public override IFixedPoolable GetNew()
        {
            var obj = base.GetNew();
            obj.OnCreate(this);
            return obj;
        }

        public override void Return(IFixedPoolable input)
        {
            if (_activePool.Remove(input))
            {
                base.Return(input);
            }
        }
    }
}
