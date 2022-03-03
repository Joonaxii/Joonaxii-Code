using Joonaxii.Image.Texturing;
using System;

namespace Joonaxii.Image.Codecs.GIF
{
    public class GIFFrame
    {
        public ushort Width { get => (ushort)_texture.Width; }
        public ushort Height { get => (ushort)_texture.Height; }

        public ushort Delay { get; private set; }
        public int Length { get => _texture.Height * _texture.Width; }

        private Texture _texture;

        public GIFFrame(ushort width, ushort height, ushort delay, FastColor[] pixels)
        {
            Delay = delay;
            _texture = new Texture(width, height, TextureFormat.RGBA32);
            _texture.SetPixels(pixels);
        }

        public GIFFrame(ushort width, ushort height, ushort delay)
        {
            Delay = delay;
            _texture = new Texture(width, height, TextureFormat.RGBA32);
        }

        public void CopyTo(GIFFrame other)
        {
            _texture.CopyTo(other._texture);
        }

        public Texture GetTexture() => _texture;

        public void Dispose()
        {
            _texture?.Dispose();
        }
    }
}