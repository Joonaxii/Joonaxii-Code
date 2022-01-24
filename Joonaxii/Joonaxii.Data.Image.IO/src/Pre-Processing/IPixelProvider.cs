namespace Joonaxii.Data.Image.Conversion.Processing
{
    public interface IPixelProvider
    {
        FastColor[] GetPixels();
        FastColor GetPixel(int i);

        void SetPixels(FastColor[] pixels);
    }
}