using System;

namespace Joonaxii.Image.Codecs.GIF
{
    public class GIFFrame
    {
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }

        public ushort Delay { get; private set; }
        public int Length { get => _pixels.Length; }

        private FastColor[] _pixels;

        public GIFFrame(ushort width, ushort height, ushort delay, FastColor[] pixels)
        {
            Width = width;
            Height = height;
            Delay = delay;

            _pixels = pixels;
        }

        public GIFFrame(ushort width, ushort height, ushort delay)
        {
            Width = width;
            Height = height;
            Delay = delay;

            _pixels = new FastColor[width * height];
        }

        public void CopyTo(GIFFrame other)
        {
            Array.Copy(_pixels, other._pixels, Length);
        }

        public void SetPixel(int i, FastColor pix) => _pixels[i] = pix;
        public void SetPixel(int x, int y, FastColor pix) => _pixels[y * Width + x] = pix;

        public FastColor GetPixel(int i) => _pixels[i];
        public FastColor GetPixel(int x, int y) => _pixels[y * Width + x];

        public FastColor[] GetPixels()
        {
            FastColor[] pix = new FastColor[_pixels.Length];
            CopyTo(pix);
            return pix;
        }

        public FastColor[] GetPixelsRef() => _pixels;

        public int CopyTo(FastColor[] pixels)
        {
            int min = pixels.Length < _pixels.Length ? pixels.Length : _pixels.Length;
            for (int i = 0; i < min; i++)
            {
                pixels[i] = _pixels[i];
            }
            return min;
        }

        public byte[] ToBytes(bool flipX, bool flipY)
        {
            byte[] bytes = new byte[_pixels.Length * 4];

            int bI = 0;
            for (int y = 0; y < Height; y++)
            {
                int yY = flipY ? Height - y - 1 : y;
                for (int x = 0; x < Width; x++)
                {
                    int xX = flipX ? Width - x - 1 : x;
                    var px = _pixels[yY * Width + xX];
                    bytes[bI++] = px.r;
                    bytes[bI++] = px.g;
                    bytes[bI++] = px.b;
                    bytes[bI++] = px.a;
                }
            }
            return bytes;
        } 

        public byte[] ToBytes()
        {
            byte[] bytes = new byte[_pixels.Length * 4];
            int bI = 0;
            for (int i = 0; i < _pixels.Length; i++)
            {
                var px = _pixels[i];
                bytes[bI++] = px.r;
                bytes[bI++] = px.g;
                bytes[bI++] = px.b;
                bytes[bI++] = px.a;
            }
            return bytes;
        }
    }
}