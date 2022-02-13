using System;

namespace Joonaxii.Image.Processing
{
    public class ReadWirtePixelArray : IPixelProvider
    {
        private FastColor[] _readBuffer;
        private FastColor[] _writeBuffer;
        public ReadWirtePixelArray(FastColor[] readBuffer, FastColor[] writeBuffer)
        {
            _readBuffer = readBuffer;
            _writeBuffer = writeBuffer;
        }

        public FastColor GetPixel(int i) => _readBuffer[i];

        public FastColor[] GetPixels() => _readBuffer;

        public void SetPixels(FastColor[] pixels)
        {
            if (_writeBuffer != null)
            {
                if (pixels.Length != _writeBuffer.Length) { Array.Resize(ref _writeBuffer, pixels.Length); }
            }
            else { _writeBuffer = new FastColor[pixels.Length]; }
            Array.Copy(pixels, _writeBuffer, pixels.Length);
        }
    }
}
