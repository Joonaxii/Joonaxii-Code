using Joonaxii.MathJX;

namespace Joonaxii.Image.Processing
{
    public class PixelView : IPixelProvider
    {
        public RectInt GetRect { get => _block; }

        private FastColor[] _pixels;
        private FastColor[] _writePixels;
        private RectInt _block;

        private int _width;

        public PixelView(int width, FastColor[] pixels, RectInt block, FastColor[] writePixels = null)
        {
            _width = width;
            _block = block;
            _pixels = pixels;

            _writePixels = writePixels != null ? writePixels : pixels;
        }

        public FastColor[] GetPixels()
        {
            int h = _block.Size.x;
            int w = _block.Size.y;

            FastColor[] pixels = new FastColor[w * h];
            for (int y = 0; y < h; y++)
            {
                int yPS = y * _block.Size.x;
                int yPT = (y + _block.MinY) * _block.Size.x;
                for (int x = 0; x < w; x++)
                {
                    int iS = yPS + x;
                    int iT = yPT + (x + _block.MinX);
                    pixels[iS] = _pixels[iT];
                }
            }
            return pixels;
        }

        public void SetPixels(FastColor[] pixels)
        {
            int h = _block.Size.x;
            int w = _block.Size.y;

            for (int y = 0; y < h; y++)
            {
                int yPS = y * _block.Size.x;
                int yPT = (y + _block.MinY) * _width;
                for (int x = 0; x < w; x++)
                {
                    int iS = yPS + x;
                    int iT = yPT + (x + _block.MinX);
                    _writePixels[iT] = pixels[iS];
                }
            }
        }

        public FastColor GetPixel(int i)
        {
            int x = i % _block.Size.x + _block.MinX;
            int y = i / _block.Size.x + _block.MinY;
            return _pixels[y * _block.Size.x + x];
        }
    }
}
