namespace Joonaxii.MathJX
{
    public struct Rect
    {
        public float MaxX { get => _max.x; }
        public float MaxY { get => _max.y; }

        public float MinX { get => _min.x; }
        public float MinY { get => _min.y; }

        public Vector2 Min { get => _min; }
        public Vector2 Max { get => _max; }

        public Vector2 Center { get => _center; }
        public Vector2 Size { get => _size; }

        public float Width { get => _size.x; }
        public float Height { get => _size.y; }

        private Vector2 _center;
        private Vector2 _size;

        private Vector2 _min;
        private Vector2 _max;

        private Vector2 _sizeHalf;

        public Rect(float x, float y, float w, float h)
        {
            _min = new Vector2(x, y);
            _size = new Vector2(w, h);
            _sizeHalf.x = _size.x * 0.5f;
            _sizeHalf.y = _size.y * 0.5f;

            _center.x = _min.x + _sizeHalf.x;
            _center.y = _min.y + _sizeHalf.y;

            _max.x = _min.x + _size.x;
            _max.y = _min.y + _size.y;
        }

        public Rect(Vector2 min, Vector2 max)
        {
            _min = min;
            _max = max;
            _size = _max - _min;

            _sizeHalf.x = _size.x * 0.5f;
            _sizeHalf.y = _size.y * 0.5f;

            _center.x = _min.x + _sizeHalf.x;
            _center.y = _min.y + _sizeHalf.y;
        }

        public void Set(Vector2 pos)
        {
            _min.x = pos.x;
            _min.y = pos.y;

            _center.x = _min.x + _sizeHalf.x;
            _center.y = _min.y + _sizeHalf.y;

            _max.x = _min.x + _size.x;
            _max.y = _min.y + _size.y;
        }

        public void Set(Vector2 pos, Vector2 size)
        {
            _size = size;
            _sizeHalf.x = _size.x * 0.5f;
            _sizeHalf.y = _size.y * 0.5f;
            Set(pos);
        }

        public void SetMinMax(Vector2 min, Vector2 max)
        {
            _min.x = min.x;
            _min.y = min.y;

            _max.x = max.x;
            _max.y = max.y;

            _size.x = _max.x - _min.x;
            _size.y = _max.y - _min.y;

            _sizeHalf.x = _size.x * 0.5f;
            _sizeHalf.y = _size.y * 0.5f;

            _center.x = _min.x + _sizeHalf.x;
            _center.y = _min.y + _sizeHalf.y;

            _max.x = _min.x + _size.x;
            _max.y = _min.y + _size.y;
        }

        public bool Overlaps(Rect rect) => !(_min.x > rect._max.x | _min.y > rect._max.y | _max.x < rect._min.x | _max.y < rect._min.y);
        public bool Contains(Vector2 point) => !(_min.x > point.x | _min.y > point.y | _max.x < point.x | _max.y < point.y);
    }
}
