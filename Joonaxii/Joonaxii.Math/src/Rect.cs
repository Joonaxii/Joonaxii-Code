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
            _sizeHalf = _size * 0.5f;
            _center = _min + _sizeHalf;
            _max = _min + _size;
        }

        public Rect(Vector2 min, Vector2 max)
        {
            _min = min;
            _max = max;
            _size = _max - _min;

            _sizeHalf = _size * 0.5f;
            _center = _min + _sizeHalf;
        }

        public void Set(Vector2 pos)
        {
            _min.x = pos.x;
            _min.y = pos.y;

            _center = pos + _sizeHalf;
            _max = pos + _size;
        }

        public void Set(Vector2 min, Vector2 max)
        {
            _min.x = min.x;
            _min.y = min.y;

            _max.x = max.x;
            _max.y = max.y;

            _size = _max - _min;
            _sizeHalf = _size * 0.5f;

            _center = _min + _sizeHalf;
            _max = _min + _size;
        }
    }
}
