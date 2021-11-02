using System;
using System.Collections.Generic;

namespace Joonaxii.Pooling
{
    public class ObjectPool : GenericObjectPool<IPoolable>
    {
        public ObjectPool(int initCount, Func<IPoolable> createNew) : base(initCount, createNew) { }

        public override IPoolable GetNew()
        {
            var obj = base.GetNew();
            obj.OnCreate(this);
            return obj;
        }
    }
}