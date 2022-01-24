using System;

namespace Joonaxii.Data.Image.Processing.Dithering
{
    public interface IDitherProcess
    {
        bool IsRunning { get; }
      
        int Width { get; }
        int Height { get; }

        void Setup(int width, int height, FastColor[] pixels, float[] mask);
        void Run(FastColor[] pixels, Action<int, int, float> progression = null);

        FastColor[] GetPixels();

        void AddOnCompleteListener(Action<IDitherProcess> act);
        void AddOnExceptionListener(Action<Exception> act);

        void RemoveOnCompleteListener(Action<IDitherProcess> act);
        void RemoveOnExceptionListener(Action<Exception> act);
    }
}
