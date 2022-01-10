using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.Collections.ListQueue
{
    public class ListQueue<T> : IList<T>
    {
        public int Capacity => _capacity;
        public int Count => _count;
        public bool IsReadOnly => false;

        private T[] _queue = new T[0];
        private int _count;

        private int _capacity;
        private int _startPoint;
        private int _endPoint;
        private int _shiftThreshold;
        private int _last;
        private int _lastIndex;

        public T this[int index] { get => _queue[index + _startPoint]; set => _queue[index + _startPoint] = value; }

        public ListQueue() : this(64) { }
        public ListQueue(int capacity)
        {
            Resize(capacity);
            Clear();

        }

        public ListQueue(IEnumerable<T> enumerable)
        {
            Resize(256);
            Clear();

            foreach (var item in enumerable)
            {
                Enqueue(item);
            }
        }

        public T Dequeue()
        {
            var first = _queue[_startPoint];
            _queue[_startPoint] = default(T);
            _startPoint++;

            _count--;
            _last--;

            CheckForShift();
            return first;
        }

        public void Enqueue(T item)
        {
            if (_count >= _capacity)
            {
                Resize(_capacity << 1);
            }
            _queue[_endPoint++] = item;
            _count++;

            _last++;
            _lastIndex++;
        }

        public void Add(T item) => Enqueue(item);

        public void Clear()
        {
            Array.Clear(_queue, _startPoint, _count);
            _count = 0;

            _startPoint = 0;
            _endPoint = _count;
            _last = _count-1;
            _lastIndex = _last;
        }

        public bool Contains(T item) => IndexOf(item) >= 0;

        public void CopyTo(T[] array, int arrayIndex)
        {
            int len = Math.Min(array.Length - arrayIndex, _count);
            for (int i = 0; i < len; i++)
            {
                int iI = i + _startPoint;
                int iT = i + arrayIndex;
                array[iT] = _queue[iI];
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = _startPoint; i < _endPoint; i++)
            {
                yield return _queue[i];
            }
        }

        public int IndexOf(T item) => Array.IndexOf(_queue, item, _startPoint, _count);

        public void Insert(int index, T item)
        {
            if(index < 0 | index >= _count) { return; }

            if(index == 0 & _startPoint > 0)
            {
                _queue[_startPoint - 1] = item;
                _count++;
                _last++;
                _startPoint--;
                return;
            }

            if(index == _last)
            {
                Enqueue(item);
                return;
            }

            _count++;
            if(_count >= _capacity) { Resize(_capacity << 1); }

            _endPoint++;
            _lastIndex++;
            Array.Copy(_queue, index, _queue, index + 1, _count - 1);
            _queue[index] = item;
        }
        public bool Remove(T item)
        {
            var itm = IndexOf(item);
            if(itm > -1)
            {
                RemoveAt(itm);
                return true;
            }
            return false;
        }

        public void Sort()
        {
            Array.Sort(_queue, _startPoint, _count);
        }

        public void RemoveAt(int i)
        {
            if(i < 0 | i >= _count) { return; }

            if (i == 0)
            {
                Dequeue();
                return;
            }

            i += _startPoint;
            if(i >= _capacity) { return; }

            if (i == _last) 
            {
                Dequeue();
                return;
            }

            Array.Copy(_queue, i + 1, _queue, i, _last - i);
            _queue[_last] = default(T);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void Resize(int size)
        {
            _capacity = size;
            _shiftThreshold = size >> 1;

            Array.Resize(ref _queue, _capacity);
            CheckForShift();
        }

        private void CheckForShift()
        {
            if(_startPoint > _shiftThreshold)
            {
                Array.Copy(_queue, _startPoint, _queue, 0, _count);

                _startPoint = 0;
                _endPoint = _count;
                _last = _count - 1;
                _lastIndex = _last;
            }
        }
    }
}
