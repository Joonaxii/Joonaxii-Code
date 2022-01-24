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
            _center = new Vector2Int(x, y);
            _size = new Vector2Int(w, h);
            _sizeHalf = _size / 2;

            _min = _center - _sizeHalf;
            _max = _center + _sizeHalf;
        }

        public RectInt(Vector2Int min, Vector2Int max)
        {
            _min = min;
            _max = max;
            _size = _max - _min;

            _sizeHalf = _size / 2;
            _center = _min + _sizeHalf;
        }

        public RectInt Clamp(int minX, int maxX, int minY, int maxY)
        {
            _min.x = _min.x < minX ? minX : _min.x > maxX ? maxX : _min.x;
            _min.y = _min.y < minY ? minY : _min.y > maxY ? maxY : _min.y;

            _max.x = _max.x < minX ? minX : _max.x > maxX ? maxX : _max.x;
            _max.y = _max.y < minY ? minY : _max.y > maxY ? maxY : _max.y;

            _size = _max - _min;
            _sizeHalf = _size / 2;

            _center = _min + _sizeHalf;
            _max = _min + _size;
            return this;
        }

        public void Set(Vector2Int pos)
        {
            _min.x = pos.x;
            _min.y = pos.y;

            _center = pos + _sizeHalf;
            _max = pos + _size;
        }

        public void Set(int x, int y, int w, int h)
        {
            _center.x = x;
            _center.y = y;

  
            _size.x = w;
            _size.y = h;

            _sizeHalf.x = w >> 1;
            _sizeHalf.y = h >> 1;

            _min.x = _center.x - _sizeHalf.x;
            _min.y = _center.y - _sizeHalf.y;

            _max.x = _center.x + _sizeHalf.x;
            _max.y = _center.y + _sizeHalf.y;
        }

        public void SetMinMax(int minX, int maxX, int minY, int maxY, bool full = true)
        {
            _min.x = minX;
            _min.y = minY;

            _max.x = maxX;
            _max.y = maxY;

            _size.x = _max.x - _min.x;
            _size.y = _max.y - _min.y;

            if (!full) { return; }

            _sizeHalf.x = _size.x >> 1;
            _sizeHalf.y = _size.y >> 1;

            _center.x = minX + _sizeHalf.x;
            _center.y = minY + _sizeHalf.y;
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
