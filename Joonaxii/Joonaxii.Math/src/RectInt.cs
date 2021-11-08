namespace Joonaxii.MathJX
{
    public struct RectInt
    {
        public int MaxX { get => _max.x; }
        public int MaxY { get => _max.y; }

        public int MinX { get => _min.x; }
        public int MinY { get => _min.y; }

        public Vector2Int Min { get => _min; }
        public Vector2Int Max { get => _max; }

        public Vector2Int Center { get => _center; }
        public Vector2Int Size { get => _size; }

        public int Width { get => _size.x; }
        public int Height { get => _size.y; }

        private Vector2Int _center;
        private Vector2Int _size;

        private Vector2Int _min;
        private Vector2Int _max;

        private Vector2Int _sizeHalf;

        public RectInt(int x, int y, int w, int h)
        {
            _min = new Vector2Int(x, y);
            _size = new Vector2Int(w, h);
            _sizeHalf = _size / 2;
            _center = _min + _sizeHalf;
            _max = _min + _size;
        }

        public RectInt(Vector2Int min, Vector2Int max)
        {
            _min = min;
            _max = max;
            _size = _max - _min;

            _sizeHalf = _size / 2;
            _center = _min + _sizeHalf;
        }

        public void Set(Vector2Int pos)
        {
            _min.x = pos.x;
            _min.y = pos.y;

            _center = pos + _sizeHalf;
            _max = pos + _size;
        }

        public void Set(Vector2Int min, Vector2Int max)
        {
            _min.x = min.x;
            _min.y = min.y;

            _max.x = max.x;
            _max.y = max.y;

            _size = _max - _min;
            _sizeHalf = _size / 2;

            _center = _min + _sizeHalf;
            _max = _min + _size;
        }
    }
}
